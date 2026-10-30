using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// Modddif-style "ring" texturing:
	// - the user selects the model (without the cloned copies),
	// - we orbit one real view-camera around it and save N poses,
	// - every pose is sent to Stable Diffusion separately at full resolution,
	// - every result is projected back from the same pose.
	//
	// The old "clones + one image" workflow is left completely untouched. This
	// component is opt-in and only takes over the normal Generate button while
	// ring mode is enabled.
	public class RingProjection_MGR : MonoBehaviour {
	    public static RingProjection_MGR instance { get; private set; } = null;

	    public const int MIN_VIEWS = 2;
	    public const int MAX_VIEWS = 8;
	    public const int DEFAULT_VIEWS = 4;

	    const string PREF_ENABLED = "SPZ_RingProjection_Enabled";
	    const string PREF_VIEWS   = "SPZ_RingProjection_Views";

	    public event Action OnStateChanged;

	    public bool isEnabled { get; private set; } = false;
	    public int numViews { get; private set; } = DEFAULT_VIEWS;
	    public bool isRunning { get; private set; } = false;
	    public int currentViewIx { get; private set; } = -1;
	    public int totalViews { get; private set; } = 0;
	    public string statusText { get; private set; } = "Idle";

	    // While ring mode is generating, every payload must be one image and all
	    // views should share the same seed. The payload-maker reads these.
	    public static int? seedOverride { get; private set; } = null;
	    public static bool forceSingleImage => instance != null && instance.isRunning;

	    public string progressPrefix => isRunning && totalViews > 0
	        ? $"Ring view {currentViewIx + 1}/{totalViews}: "
	        : "";

	    bool _awaitingResult = false;
	    bool _lastResultSuccess = false;
	    bool _cancelRequested = false;

	    View_UserCamera _cam = null;
	    Vector3 _savedCamPos;
	    Quaternion _savedCamRot;
	    Vector2 _savedProjCenter;
	    float _savedFov;
	    bool _savedCamValid = false;
	    bool[] _savedActiveCams = null;
	    bool _savedMultiViewUI_enabled = false;
	    bool _multiViewUI_wasNull = true;

	    struct Pose {
	        public Vector3 pos;
	        public Quaternion rot;
	    }


	    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	    static void Bootstrap() {
	        if (instance != null) { return; }
	        GameObject go = new GameObject("RingProjection");
	        DontDestroyOnLoad(go);
	        go.AddComponent<RingProjection_MGR>();
	        go.AddComponent<RingProjection_UI>();
	    }


	    void Awake() {
	        if (instance != null) { Destroy(gameObject); return; }
	        instance = this;
	        DontDestroyOnLoad(gameObject);

	        isEnabled = PlayerPrefs.GetInt(PREF_ENABLED, 0) != 0;
	        numViews  = Mathf.Clamp(PlayerPrefs.GetInt(PREF_VIEWS, DEFAULT_VIEWS), MIN_VIEWS, MAX_VIEWS);
	        SD_GenRequests_Helper._Act_OnGenerationFinished += OnGenerationFinished;
	    }


	    void OnDestroy() {
	        SD_GenRequests_Helper._Act_OnGenerationFinished -= OnGenerationFinished;
	        if (instance == this) { instance = null; }
	    }


	    public void ToggleEnabled() => SetEnabled(!isEnabled);


	    public void SetEnabled(bool value) {
	        if (isRunning) { return; }
	        isEnabled = value;
	        PlayerPrefs.SetInt(PREF_ENABLED, isEnabled ? 1 : 0);
	        PlayerPrefs.Save();
	        SetStatus(isEnabled
	            ? $"Ring mode ON. {numViews} full-resolution views. Select only the model to texture."
	            : "Ring mode OFF. Normal Generate is unchanged.");
	    }


	    public void SetNumViews(int value) {
	        if (isRunning) { return; }
	        numViews = Mathf.Clamp(value, MIN_VIEWS, MAX_VIEWS);
	        PlayerPrefs.SetInt(PREF_VIEWS, numViews);
	        PlayerPrefs.Save();
	        SetStatus($"Ring mode: {numViews} views around the model.");
	    }


	    // Called by StableDiffusion_Hub. Returns true when this click was consumed
	    // by ring mode (even if we only showed an error), false to let the normal
	    // single-image workflow proceed.
	    public bool TryStartGeneration(bool isMakingBackgrounds) {
	        if (isMakingBackgrounds || !isEnabled || isRunning) { return false; }
	        if (StableDiffusion_Hub.instance == null) { return false; }

	        if (UserCameras_MGR.instance == null || ModelsHandler_3D.instance == null) {
	            SetStatus("Ring: scenes are still loading.");
	            return true;
	        }
	        if (UserCameras_MGR.instance._curr_viewCamera == null) {
	            SetStatus("Ring: the viewport camera is still loading.");
	            return true;
	        }

	        if (ModelsHandler_3D.instance.selectedMeshes == null || ModelsHandler_3D.instance.selectedMeshes.Count == 0) {
	            SetStatus("Ring: select the model you want to texture first.");
	            Viewport_StatusText.instance?.ShowStatusText("Ring mode: select the model you want to texture first.", false, 4, false);
	            return true;
	        }

	        if (StableDiffusion_Hub.instance._generating || StableDiffusion_Hub.instance._finalPreparations_beforeGen) {
	            SetStatus("Ring: Stable Diffusion is still busy.");
	            return true;
	        }
	        if (Time.unscaledTime < StableDiffusion_Hub.instance._generationCooldownUntil) {
	            SetStatus("Ring: wait a moment after the last cancellation.");
	            return true;
	        }
	        if (StableDiffusion_Hub.instance.DenyWithMessage_ifCantGenerate(allow_without_controlnets: false)) {
	            SetStatus("Ring: cannot generate yet. See the viewport message.");
	            return true;
	        }

	        StartCoroutine(RunRing(isMakingBackgrounds: false));
	        return true;
	    }


	    public void Cancel() {
	        if (!isRunning) { return; }
	        _cancelRequested = true;
	        _awaitingResult = false;
	        _lastResultSuccess = false;
	        SetStatus("Ring: cancelling...");

	        var hub = StableDiffusion_Hub.instance;
	        if (hub != null && (hub._generating || hub._finalPreparations_beforeGen)) {
	            hub.OnStopGenerate_Button();
	        }
	    }


	    IEnumerator RunRing(bool isMakingBackgrounds) {
	        isRunning = true;
	        _cancelRequested = false;
	        _awaitingResult = false;
	        _lastResultSuccess = false;
	        currentViewIx = -1;
	        totalViews = numViews;

	        string finalStatus = "Ring: stopped.";
	        int completedViews = 0;

	        // try/finally makes sure a broken request or a scene change can never
	        // leave the user's cameras disabled or the mode half-running.
	        try {
	            SaveState();
	            SetStatus($"Ring: preparing {totalViews} views...");

	            var cams = UserCameras_MGR.instance;
	            var multiViewUI = MultiView_Ribbon_UI.instance;
	            if (multiViewUI != null) { multiViewUI.enabled = false; }
	            cams.DisableAll_but_CurrViewCam(doCallbacks: true);

	            // Pick a seed once, so all angles describe the same character/material.
	            int uiSeed = SD_InputPanel_UI.instance != null ? SD_InputPanel_UI.instance.seed_intField.recentVal : -1;
	            seedOverride = uiSeed > 0 ? uiSeed : UnityEngine.Random.Range(1, int.MaxValue);

	            List<Pose> poses = BuildPoses();
	            if (poses == null || poses.Count == 0) {
	                finalStatus = "Ring: could not calculate camera poses.";
	                yield break;
	            }

	            for (int i = 0; i < poses.Count; ++i) {
	                if (_cancelRequested) { break; }

	                currentViewIx = i;
	                ApplyPose(poses[i]);
	                SetStatus($"Ring: view {i + 1}/{totalViews} - capturing and generating...");

	                // Let the view/content cameras copy the new pose before the request.
	                yield return null;
	                if (_cancelRequested) { break; }

	                _awaitingResult = true;
	                _lastResultSuccess = false;
	                bool started = StableDiffusion_Hub.instance.GenerateOnce(isMakingBackgrounds);
	                if (!started) {
	                    _awaitingResult = false;
	                    break;
	                }

	                while (_awaitingResult && !_cancelRequested) {
	                    yield return null;
	                }

	                if (_cancelRequested) { break; }
	                if (!_lastResultSuccess) { break; }

	                completedViews++;
	                SetStatus($"Ring: view {i + 1}/{totalViews} done. {completedViews}/{totalViews} projected.");

	                // Give the new projector one frame to land before moving the camera.
	                yield return null;
	            }

	            if (_cancelRequested) {
	                finalStatus = $"Ring: cancelled after {completedViews}/{totalViews} views.";
	            } else if (completedViews >= totalViews) {
	                finalStatus = $"Ring: done. {completedViews} full-resolution views projected.";
	            } else {
	                finalStatus = $"Ring: stopped after {completedViews}/{totalViews} views.";
	            }
	        } finally {
	            if (isRunning) { FinishRing(finalStatus); }
	        }
	    }


	    void FinishRing(string finalStatus) {
	        try {
	            RestoreState();
	        } catch (Exception e) {
	            Debug.LogException(e);
	            finalStatus += " (camera restore failed)";
	        } finally {
	            // Even if restoring a camera fails, never leave the mode stuck on.
	            seedOverride = null;
	            isRunning = false;
	            currentViewIx = -1;
	            totalViews = 0;
	            _awaitingResult = false;
	        }

	        SetStatus(finalStatus);
	        Objects_Renderer_MGR.instance?.ReRenderAll_soon();
	    }


	    void OnGenerationFinished(Generate_RequestingWhat what, GenData2D genData, bool success) {
	        if (!_awaitingResult || !isRunning) { return; }
	        if (what != Generate_RequestingWhat.txt2img && what != Generate_RequestingWhat.img2img) { return; }

	        _awaitingResult = false;
	        _lastResultSuccess = success;

	        if (!success) {
	            if (genData != null) {
	                GenData2D_Archive.instance.OnTerminatedGeneration(genData);
	            }
	            SetStatus("Ring: generation failed. See the status text for details.");
	        }
	    }


	    List<Pose> BuildPoses() {
	        Bounds bounds = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();
	        Vector3 center = bounds.center;
	        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);

	        _cam = UserCameras_MGR.instance._curr_viewCamera;
	        if (_cam == null) { return null; }

	        _cam.cameraFocus.CancelCurrentFlight_andKeepPlacement();

	        float fov = _cam.fovMgr._trueCameraFov > 0 ? _cam.fovMgr._trueCameraFov : _cam.myCamera.fieldOfView;
	        fov = Mathf.Clamp(fov, 1f, 179f);

	        float aspect = 1f;
	        if (SD_InputPanel_UI.instance != null && SD_InputPanel_UI.instance.height > 0) {
	            aspect = Mathf.Max(0.05f, (float)SD_InputPanel_UI.instance.width / (float)SD_InputPanel_UI.instance.height);
	        }

	        float vFov = fov * Mathf.Deg2Rad;
	        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * aspect);
	        float fitFov = Mathf.Min(vFov, hFov);
	        float distance = radius / Mathf.Max(0.001f, Mathf.Sin(fitFov * 0.5f)) * 1.08f;

	        Vector3 baseDir = _cam.transform.position - center;
	        if (baseDir.sqrMagnitude < 0.0001f) { baseDir = Vector3.back; }
	        baseDir.Normalize();

	        // A purely vertical view makes the ring ambiguous. Keep a little
	        // elevation, but never look straight down/up at the model.
	        if (Mathf.Abs(Vector3.Dot(baseDir, Vector3.up)) > 0.97f) {
	            baseDir = (baseDir + Vector3.back * 0.35f).normalized;
	        }

	        List<Pose> poses = new List<Pose>(numViews);
	        float step = 360f / numViews;
	        for (int i = 0; i < numViews; ++i) {
	            Vector3 dir = Quaternion.AngleAxis(step * i, Vector3.up) * baseDir;
	            Vector3 pos = center + dir * distance;
	            Quaternion rot = Quaternion.LookRotation((center - pos).normalized, Vector3.up);
	            poses.Add(new Pose { pos = pos, rot = rot });
	        }
	        return poses;
	    }


	    void ApplyPose(Pose pose) {
	        if (_cam == null) { return; }
	        _cam.cameraFocus.CancelCurrentFlight_andKeepPlacement();
	        _cam.transform.SetPositionAndRotation(pose.pos, pose.rot);
	        _cam.Set_ProjMat_center(new Vector2(0.5f, 0.5f));
	    }


	    void SaveState() {
	        _cam = UserCameras_MGR.instance._curr_viewCamera;
	        _savedCamValid = _cam != null;
	        if (_savedCamValid) {
	            _savedCamPos = _cam.transform.position;
	            _savedCamRot = _cam.transform.rotation;
	            _savedProjCenter = _cam._projectionMat_center;
	            _savedFov = _cam.fovMgr._trueCameraFov > 0 ? _cam.fovMgr._trueCameraFov : _cam.myCamera.fieldOfView;
	        }

	        _savedActiveCams = new bool[UserCameras_MGR.instance.numViewCameras];
	        for (int i = 0; i < _savedActiveCams.Length; ++i) {
	            _savedActiveCams[i] = UserCameras_MGR.instance.isViewCameraActive(i);
	        }

	        var multiViewUI = MultiView_Ribbon_UI.instance;
	        _multiViewUI_wasNull = multiViewUI == null;
	        if (multiViewUI != null) { _savedMultiViewUI_enabled = multiViewUI.enabled; }
	    }


	    void RestoreState() {
	        if (_cam == null) { _cam = UserCameras_MGR.instance != null ? UserCameras_MGR.instance._curr_viewCamera : null; }

	        if (_cam != null && _savedCamValid) {
	            _cam.cameraFocus.CancelCurrentFlight_andKeepPlacement();
	            _cam.transform.SetPositionAndRotation(_savedCamPos, _savedCamRot);
	            _cam.fovMgr.SetFieldOfView(Mathf.Clamp(_savedFov, 1f, 179f), compensateByDistanceOffset: false);
	            _cam.Set_ProjMat_center(_savedProjCenter);
	        }

	        var cams = UserCameras_MGR.instance;
	        if (cams != null && _savedActiveCams != null) {
	            int count = Mathf.Min(_savedActiveCams.Length, cams.numViewCameras);
	            for (int i = 0; i < count; ++i) {
	                bool want = _savedActiveCams[i];
	                if (cams.isViewCameraActive(i) != want) {
	                    cams.ToggleViewCamera(i, want, doCallback: true);
	                }
	            }
	        }

	        var multiViewUI = MultiView_Ribbon_UI.instance;
	        if (multiViewUI != null) {
	            int activeCount = cams != null ? cams.numActiveViewCameras() : 1;
	            multiViewUI.SyncWantedNumCameras(activeCount);
	            // If the UI did not exist when we started, we never disabled it;
	            // make sure a late-created one is left enabled.
	            multiViewUI.enabled = _multiViewUI_wasNull || _savedMultiViewUI_enabled;
	        }
	    }


	    void SetStatus(string text) {
	        statusText = text;
	        OnStateChanged?.Invoke();
	    }
	}
}//end namespace
