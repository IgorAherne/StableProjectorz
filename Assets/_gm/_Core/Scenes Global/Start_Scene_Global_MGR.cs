using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The 'using' directives for editor scripts must be within the #if UNITY_EDITOR block
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace spz {


	// Loads all necessary additive scenes at startup. The scene list is hard-coded
	// in this script for easier version control merging.
	public class Start_Scene_Global_MGR : MonoBehaviour
	{
	    static bool _hasLoaded = false;

	    readonly List<string> scenePathsToLoadFirst = new List<string>
	    {
	        "Assets/_gm/_Core/Scenes Global/Separators/0 ----.unity", //Delimiter
	        "Assets/_gm/_Core/System/SP Version/Version_CheckUpdates.unity",
	        "Assets/_gm/Layouts/Skeleton/UI_Global_Skeleton.unity",
	        "Assets/_gm/Features/Intro Panels/UI_IntroScreen_Starter.unity",
	        "Assets/_gm/_Core/UI (reusable)/Widgets and Gadgets/UI_ConfirmPopup_YesNo/UI_ConfirmPopup_YesNo.unity",
	        "Assets/_gm/Features/Settings/Tool_Settings.unity",
	        "Assets/_gm/_Core/Scenes Global/Managers_Global.unity", // Renamed from GlobalManagers
	    };

	    // EDIT THIS LIST
	    // Comment out or remove any scenes you do NOT want to load on startup.
	    // The path must start from "Assets/" and point to the .unity file.
	    //  Also includes an editor utility, to load all scenes for easy editing.
	    readonly List<string> scenePathsToLoadAfter = new List<string>{
        
	        // Introduction
	        "Assets/_gm/_Core/Scenes Global/Separators/1 ----.unity", //Delimiter
	        "Assets/_gm/Features/Intro Panels/UI_MainWelcomeScreenCMD.unity",
	        "Assets/_gm/Features/Intro Panels/UI_MainWelcomeScreenNovices.unity",
	        "Assets/_gm/Layouts/Viewport (MainView)/UI_Global_MainView.unity",
	        "Assets/_gm/Layouts/LeftPanel/UI_Global_Left_Panel.unity",
	        "Assets/_gm/Layouts/RightPanel/UI_Global_Right_Panel.unity",
	        "Assets/_gm/Features/StableDiffusion/Input Panel/UI_2D_StableDiffusion Input_Panel (Left).unity",
	        "Assets/_gm/Features/3D Generate/UI_3D_Generation Input_Panel (Left).unity",

	        // Common UI Elements & Popups
	        "Assets/_gm/Features/Paint/UI_ColorPicker.unity",
	        "Assets/_gm/Features/Tooltips/UI_Tooltips.unity",
	        "Assets/_gm/_Core/UI/Draggable UI + Grid/UI_CurrentlyDraggedIcons.unity",
	        "Assets/_gm/Features/Repos/ShadowR/UI_ShadowR_Init.unity",

	        // Core Global Systems
	        "Assets/_gm/_Core/Scenes Global/Separators/2 ----.unity", //Delimiter
	        "Assets/_gm/Features/StableDiffusion/GenData/Global_GenData2D_Archive.unity",

	        // World & Cameras
	        "Assets/_gm/_Core/Scenes Global/Separators/3 ----.unity", //Delimiter
	        "Assets/_gm/Features/3D Models/World_ModelsHandler_3D.unity",
	        "Assets/_gm/Features/Skybox + Background/World_Skybox.unity",
	        "Assets/_gm/Features/Camera/World_UserCameras.unity",
	        "Assets/_gm/Features/Camera/Projections/World_ProjectorCameras.unity",

	        // Tools
	        "Assets/_gm/_Core/Scenes Global/Separators/4 ----.unity", //Delimiter
	        "Assets/_gm/Features/Save Load Import Export/Tool_ProjectSave.unity",
	        "Assets/_gm/Features/Save Load Import Export/Tool_FilesDragAndDrop.unity",
	        "Assets/_gm/Features/3D Clicking/Tool_ClickSelect_Meshes.unity",
	        "Assets/_gm/Features/Paint/Inpaint/Tool_InpaintMask_TextMaker.unity",
	        "Assets/_gm/Features/Camera/Projections/Tool_MultiprojMask_TextMaker.unity",
	        "Assets/_gm/Features/StableDiffusion/Input Panel/Tool_PromptColorHighlighter.unity",
	        "Assets/_gm/Features/3D Generate/Tool_Gen3D_MGR+Gen3D_API.unity",
	        "Assets/_gm/Features/Save Load Import Export/Tool_ImagesImportHelper.unity",
        
	        // WebUI Data Fetchers
	        "Assets/_gm/_Core/Scenes Global/Separators/5 ----.unity", //Delimiter
	        "Assets/_gm/Features/StableDiffusion/Webui/Webui_Connection.unity",
	        "Assets/_gm/Features/StableDiffusion/Webui/Webui_NeuralModels.unity",
	        "Assets/_gm/Features/StableDiffusion/Webui/Webui_StableDiffusion_Hub.unity",
	        "Assets/_gm/Features/StableDiffusion/Webui/Webui_Upscaler.unity",
	        "Assets/_gm/Features/StableDiffusion/Webui/Webui_OptionsFetcher.unity",
	        "Assets/_gm/Features/StableDiffusion/Webui/Webui_System_Info.unity",
        
	        // Graphics & Rendering Managers
	        "Assets/_gm/_Core/Scenes Global/Separators/6 ----.unity", //Delimiter
	        "Assets/_gm/Features/TextureTools/AO/GFX_AmbientOcclusionBaker.unity",
	        "Assets/_gm/Features/TextureTools/Blur/GFX_BlurTextures.unity",
	        "Assets/_gm/_Core/Logic/ShadersLogic/GFX_CommonComputeShaders.unity",
	        "Assets/_gm/_Core/Logic/ShadersLogic/GFX_CommonShaders.unity",
	        "Assets/_gm/Features/TextureTools/Delight/GFX_Delight_MGR.unity",
	        "Assets/_gm/Features/Skybox + Background/GFX_HDR_PanoSkybox.unity",
	        "Assets/_gm/Features/Render/GFX_ObjectsRenderer.unity",
	        "Assets/_gm/Features/TextureTools/Dilation/GFX_TextureDilation.unity",
	        "Assets/_gm/Features/TextureTools/Screenshot/GFX_Screenshot_MGR.unity",
	    };

	    void Start(){
	        if (_hasLoaded){ DestroyImmediate(gameObject); return; }

	        if (SceneManager.sceneCount > 1){
	            Debug.Log($"<color=yellow>[{nameof(Start_Scene_Global_MGR)}]</color> Other scenes detected. Skipping  auto-load sequence to allow isolated testing.");
	            _hasLoaded = true;
	            return;
	        }
	        StartCoroutine(LoadScenes());
	    }


	    IEnumerator LoadScenes(){
	        yield return StartCoroutine(LoadScenes_crtn(scenePathsToLoadFirst));
	        yield return StartCoroutine(LoadScenes_crtn(scenePathsToLoadAfter));
	    }

	    IEnumerator LoadScenes_crtn(List<string> scenePaths)
	    {
	        Debug.Log($"Starting parallel additive load of {scenePaths.Count} scenes...");
	        var asyncOperations = new List<AsyncOperation>();

	        foreach (string scenePath in scenePaths){
	            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0){
	                Debug.LogError($"Skipping scene '{scenePath}' because it is not in the Build Settings.");
	                continue;
	            }
	            asyncOperations.Add(SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive));
	        }

	        foreach (var operation in asyncOperations){
	            yield return operation;
	            Debug.Log("completed scene ");
	            operation.allowSceneActivation = true;
	        }
	    }


	#if UNITY_EDITOR
	    /// This method is called by the custom editor button. It is not included in builds.
	    public void LoadAllScenes_EDITOR_ONLY(){
	        // First, get the path of the scene this manager is in, and open it exclusively.
	        Debug.Log("Editor: Loading all scenes from the list...");
	        string startScenePath = this.gameObject.scene.path;
	        EditorSceneManager.OpenScene(startScenePath, OpenSceneMode.Single);

	        var allScenes = new List<string>();
	        allScenes.AddRange(scenePathsToLoadFirst);
	        allScenes.AddRange(scenePathsToLoadAfter);
	        // Now, load all other scenes additively.
	        foreach (string scenePath in allScenes){
	            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null){
	                Debug.LogWarning($"Editor: Could not find scene at path '{scenePath}'. Skipping.");
	                continue;
	            }
	            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
	        }
	        Debug.Log("Editor: Scene loading complete.");
	    }
	#endif

	}
}//end namespace
