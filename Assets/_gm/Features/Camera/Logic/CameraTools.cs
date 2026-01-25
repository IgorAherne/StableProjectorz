using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace spz {

	public static class CameraTools{
   
	    public static Matrix4x4 Get_ViewProj(Camera cam){
	        // Combine them to get the view to Clip Space Matrix. Notice, order is important (mats are mulitplied right to left)
	        // NOTICE: converting the projection matrix, becaus there are differences in DirectX and OpenGL:
	        Matrix4x4 projMat = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
	        Matrix4x4 viewProjMatrix = projMat * cam.worldToCameraMatrix;
	        return viewProjMatrix;
	    }
    

	    //projectionMat_center: (0,0) would be bottom left corner of viewport, (1,1) top right.
	    public static void ShiftViewportCenter_ofProjMat(Camera cam, Vector2 projectionMat_center){
	        Vector2 viewportPoint =  Vector2.one - projectionMat_center;
	        // Calculate the frustum height at the near plane based on FOV
	        float frustumHeight = 2.0f * cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
	        // Calculate the frustum width based on the aspect ratio.
	        float frustumWidth = frustumHeight*cam.aspect;// USUALLY you did _camera.aspect=renderTex.width/(float)renderTex.height;

	        // Map viewport coordinates (0 to 1) to frustum dimensions
	        float left = (viewportPoint.x - 0.5f) * frustumWidth;
	        float right = left + frustumWidth;
	        float bottom = (viewportPoint.y - 0.5f) * frustumHeight;
	        float top = bottom + frustumHeight;

	        // Adjust left and right to maintain the center position at the mouse cursor
	        left -= frustumWidth * 0.5f;
	        right -= frustumWidth * 0.5f;
	        // Adjust top and bottom to maintain the center position at the mouse cursor
	        bottom -= frustumHeight * 0.5f;
	        top -= frustumHeight * 0.5f;

	        Matrix4x4 m = PerspectiveOffCenter(left, right, bottom, top, cam.nearClipPlane, cam.farClipPlane);
	        cam.projectionMatrix = m;
	    }


	    //taken from https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
	    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far){
	        float x = 2.0F * near / (right - left);
	        float y = 2.0F * near / (top - bottom);
	        float a = (right + left) / (right - left);
	        float b = (top + bottom) / (top - bottom);
	        float c = -(far + near) / (far - near);
	        float d = -(2.0F * far * near) / (far - near);
	        float e = -1.0F;
	        Matrix4x4 m = new Matrix4x4();
	        m[0, 0] = x;
	        m[0, 1] = 0;
	        m[0, 2] = a;
	        m[0, 3] = 0;
	        m[1, 0] = 0;
	        m[1, 1] = y;
	        m[1, 2] = b;
	        m[1, 3] = 0;
	        m[2, 0] = 0;
	        m[2, 1] = 0;
	        m[2, 2] = c;
	        m[2, 3] = d;
	        m[3, 0] = 0;
	        m[3, 1] = 0;
	        m[3, 2] = e;
	        m[3, 3] = 0;
	        return m;
	    }


	    //Ensures your multi-pov shader uses correct #defines:
	    //You can provide null material, if you want to set into shader (as global variables).
	    public static void Toggle_numPOVs_Keywords(Material here, int numPovs){
	        Debug.Assert(here!=null);
	        for(int i=2; i<=6; ++i){  here.DisableKeyword("NUM_POV_"+i);  }
	        if(numPovs<2){ return; } //first two keywords don't need ot be enabled.
	        here.EnableKeyword("NUM_POV_" + numPovs);//no need to do forloop. See comment below.
	    }


	    public static void Toggle_numPOVs_Keywords(ComputeShader sh, int numPovs){
	        for(int i=2; i<=6; ++i){  sh.DisableKeyword("NUM_POV_"+i);  }
	        if(numPovs<2){ return; } //first two keywords don't need ot be enabled.
	        //NOTICE: no need to do for-loop. The shaders will automatically include all necessary defines.
	        // Otherwise we'd need to script shaders to have:
	        //  #pragma multi_compile NUM_POV_2
	        //  #pragma multi_compile NUM_POV_3
	        //  etc (in a stacked manner).
	        // Instead, we scripted them as:
	        //  #pragma multi_compile NUM_POV_2  NUM_POV_3  NUM_POV_4   which makes them mutually exclsuive.
	        // Therefore, just setting one keyword and expecting it to #define the rest, in the cginc:
	        sh.EnableKeyword("NUM_POV_" + numPovs);
	    }


	    //enables global shader properties "NUM_POV_2" etc, invokes callback, then disables them.
	    //Doesn't need a material.
	    //NOTICE: even if your material uses only few pov, these ones will still be "enabled".
	    public static void TempEnable_POVs_Keywords_GLOBAL(int numPovs, Action doStuff){
	        for(int i=2; i<=6; ++i){  Shader.DisableKeyword("NUM_POV_"+i);  }
	         //first keyword doesn't need to be enabled. And no need to do forloop. See comment below.
	        if(numPovs>=2){ Shader.EnableKeyword("NUM_POV_" + numPovs); }
	        doStuff();
	        for(int i=2; i<=6; ++i){  Shader.DisableKeyword("NUM_POV_"+i);  }
	    }


	    //you can provide null material, if you want to set into shader (as global variables).
	    public static void Set_POVs_properties_into_mat( Material here,  Camera cam, IReadOnlyList<CameraPovInfo> fromThisPOVs,  
	                                                     bool alterTheCamera=false ){
	        int ixInShader = 0;
	        for(int i=0; i<fromThisPOVs.Count; ++i){
	            CameraPovInfo pov = fromThisPOVs[i];
	            if(pov.wasEnabled==false){ continue; }
	            Set_POV_properties_into_mat(here, cam, pov, ixInShader, alterTheCamera);
	            ixInShader++;
	        }//end for povs
	    }

	    //you can provide null material, if you want to set into shader (as global variables).
	    public static void Set_POV_properties_into_mat( Material setIntoHere,  Camera cam,  CameraPovInfo fromThisPOV,  
	                                                    int ixInShader=0,  bool alterTheCamera=false){
	        // Remember camera, to ensure our shifting of viewport center
	        // won't affect camera's state once we are done setting these veraibles:
	        ParamsBeforeRender camParams = new ParamsBeforeRender(cam);
	          cam.fieldOfView = fromThisPOV.camera_fov;
	          cam.transform.position = fromThisPOV.camera_pos;
	          cam.transform.rotation = fromThisPOV.camera_rot;
	          ShiftViewportCenter_ofProjMat( cam, fromThisPOV.perspectiveCenter01);
	          // Combine them to get the view to Clip Space Matrix. Notice, order is important (mats are mulitplied right to left)
	          // NOTICE: converting the projection matrix, becaus there are differences in DirectX and OpenGL:
	          Matrix4x4 projMat = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
	          Matrix4x4 viewProjMatrix =  projMat * cam.worldToCameraMatrix;
	          if (setIntoHere != null){ 
	              setIntoHere.SetMatrix("_ViewProj_matrix"+ixInShader, viewProjMatrix);
	              setIntoHere.SetVector("_CameraWorldPos"+ixInShader, fromThisPOV.camera_pos.toVec3());
	          }else { 
	              Shader.SetGlobalMatrix("_ViewProj_matrix"+ixInShader, viewProjMatrix);
	              Shader.SetGlobalVector("_CameraWorldPos"+ixInShader, fromThisPOV.camera_pos.toVec3());
	          }
	        if(!alterTheCamera){ camParams.RestoreCam(cam); }
	    }


	    public static float Calc_PosOffset_forFOVchange(float fov_initial, float fov_final, float distance_initial){
	        float radFovInitial = fov_initial * Mathf.Deg2Rad;
	        float radFovFinal   = fov_final * Mathf.Deg2Rad;
	        float oppositeSideLen = distance_initial * Mathf.Tan(radFovInitial/2);
	        float newDistance     = oppositeSideLen / Mathf.Tan(radFovFinal/2);
	        return newDistance;
	    }


	    public static void TightPlanes_around_meshes( Camera cam, float smallestAllowedNearPlane, 
	                                                  IReadOnlyList<SD_3D_Mesh> aroundThese,
	                                                  out float nearClipPlane_, out float farClipPlane_){ 
	        if(aroundThese.Count == 0){
	            nearClipPlane_ = cam.nearClipPlane;
	            farClipPlane_ = cam.farClipPlane;
	            return; 
	        }
	        float smallestDist = float.MaxValue;
	        float greatestDist = float.MinValue;

	        for(int i=0; i<aroundThese.Count; ++i){
	            aroundThese[i].NearestFurthest_BoundBoxCoords( cam.transform.position,  cam.transform.position+cam.transform.forward*50000,
	                                                           ref smallestDist,  ref greatestDist);
	        }
	        //ensure the camera's plane doesn't become negative.
	        //That can happen if we fly into the bounding box of the 3d model.
	        farClipPlane_ = greatestDist;
	        nearClipPlane_ = Mathf.Max(smallestDist, smallestAllowedNearPlane);
	        // Ensure the near plane is always less than the far plane:
	        nearClipPlane_ = Mathf.Min(nearClipPlane_, farClipPlane_-0.01f);
	    }


	    // Helps to linearilize the Z buffer (depth) values. Via functions like Linear01Depth or LinearEyeDepth.
	    // You need to invoke this if you are doing Graphics.Blit(). This will ensure latest camera is correct one.
	    public static void SetZBufferParams_intoMat(Camera cam, Material mat){
	        // https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html 
	        float near = cam.nearClipPlane;
	        float far = cam.farClipPlane;
	        float x = 1 - far/near;
	        float y = far/near;
	        float z = x/far;
	        float w = y/far;
	        mat.SetVector("_ZBufferParams", new Vector4(x,y,z,w));
	        Debug.Assert(false, "this function isn't usable - unity prevents/ignores setting builting parameter (gives console warnings)");
	    }


	    public static void RenderIntoTextureArray( ref CommandBuffer cmd, Camera camera, IReadOnlyList<Renderer> renderers,  
	                                               Material material, RenderTexture targetTextureArray,  int sliceIndex,  Color clearColor){
	        if(cmd == null){
	            cmd = new CommandBuffer();
	            cmd.name = "Render to TextureArray";
	        }
	        cmd.Clear();

	        cmd.SetupCameraProperties(camera);
	        // Set render target to specific slice of texture array
	        cmd.SetRenderTarget(new RenderTargetIdentifier(targetTextureArray, 0, CubemapFace.Unknown, sliceIndex));
	        cmd.ClearRenderTarget(true, true, clearColor);
	        // Draw all renderers
	        foreach(var r in renderers){
	            cmd.DrawRenderer(r, material);
	        }
	        Graphics.ExecuteCommandBuffer(cmd);//send the gpu!
	    }
	}


	// Allows to remember state of the camera and revert it afterwards.
	// Just make sure you disable anything not mentioned here, if you tweak it.
	public class ParamsBeforeRender{
	    public bool isOn;
	    public float prevFov;
	    public Vector3 prevPos;
	    public Quaternion prevRot;
	    public CameraClearFlags prevFlags;
	    public bool prevAllowMSAA;
	    public Matrix4x4 prevCullingMatrix;
	    public Matrix4x4 prevProjMatrix;
	    public RenderTexture prevTexture;
	    public ParamsBeforeRender(Camera cam){
	        this.isOn = cam.enabled;
	        this.prevFov = cam.fieldOfView;
	        this.prevPos = cam.transform.position;
	        this.prevRot = cam.transform.rotation;
	        this.prevFlags = cam.clearFlags;
	        this.prevAllowMSAA = cam.allowMSAA;
	        this.prevCullingMatrix = cam.cullingMatrix;
	        this.prevProjMatrix = cam.projectionMatrix;
	        this.prevTexture = cam.targetTexture;
	    }
	    public void RestoreCam(Camera restoreThisCam){
	        restoreThisCam.enabled = isOn;
	        restoreThisCam.fieldOfView = prevFov;
	        restoreThisCam.transform.position = prevPos;
	        restoreThisCam.transform.rotation = prevRot;
	        restoreThisCam.clearFlags = prevFlags;
	        restoreThisCam.allowMSAA  = prevAllowMSAA;
	        restoreThisCam.cullingMatrix = prevCullingMatrix;
	        restoreThisCam.projectionMatrix = prevProjMatrix;
	        restoreThisCam.targetTexture = prevTexture;
	    }
	}



	// creates command buffer, and disposes it when this object is garbage-collected.
	// This way we don't have to recreate the buffer and can reuse it.
	// And it won't be forgotten to be released.
	public class CommandBufferScope : IDisposable{
	    public CommandBuffer Cmd { get; private set; }

	    public CommandBufferScope(string name){  
	        Cmd = new CommandBuffer();
	        Cmd.name = name;
	    }

	    //Keep sliceIndex as -1 if destination slice will be decided in shader (via SV_RenderTargetArrayIndex)
	    // https://docs.unity3d.com/Manual/class-Texture2DArray.html
	    public void RenderIntoTextureArray( Camera camera,  IReadOnlyList<Renderer> renderers,  Material material, 
	                                        bool useClearingColor, bool clearTheDepth,  Color clearColor, 
	                                        RenderTexture targetTextureArray,  int sliceIndex=-1, 
	                                        Action onPreCull=null, Action onPreRender=null, Action onPostRender=null){
	        Cmd.Clear();  
	        onPreCull?.Invoke();
	        onPreRender?.Invoke();
	        Cmd.SetupCameraProperties(camera);
	        Cmd.SetRenderTarget( new RenderTargetIdentifier(targetTextureArray, 0, CubemapFace.Unknown, sliceIndex) );
	        if(useClearingColor || clearTheDepth){ 
	            Cmd.ClearRenderTarget(clearDepth:clearTheDepth, clearColor:useClearingColor, clearColor);
	        }
	        foreach(var r in renderers){
	            Cmd.DrawRenderer(r, material);
	        }
	        Graphics.ExecuteCommandBuffer(Cmd);
	        onPostRender?.Invoke();
	    }

	    //Keep sliceIndex as -1 if destination slice will be decided in shader (via SV_RenderTargetArrayIndex)
	    // https://docs.unity3d.com/Manual/class-Texture2DArray.html
	    public void RenderIntoTextureArrays( Camera camera,  IReadOnlyList<Renderer> renderers,  Material material, 
	                                        bool useClearingColor,  bool clearTheDepth, Color clearColor, 
	                                        IReadOnlyList<RenderTexture> targetTextureArrays,  int sliceIndex=-1, 
	                                        Action onPreCull=null, Action onPreRender=null, Action onPostRender=null){
	        Cmd.Clear();
	        onPreCull?.Invoke();
	        onPreRender?.Invoke();
	        Cmd.SetupCameraProperties(camera);

	        int numArrays = targetTextureArrays.Count;
        
	        var depthIdentifier= new RenderTargetIdentifier(targetTextureArrays[0], 0, CubemapFace.Unknown, sliceIndex);
	        var colIdentifiers = new RenderTargetIdentifier[numArrays];
        
	        for(int i=0; i<numArrays; ++i){
	            colIdentifiers[i] = new RenderTargetIdentifier(targetTextureArrays[i], 0, CubemapFace.Unknown, sliceIndex);
	        }
	        Cmd.SetRenderTarget(colors:colIdentifiers, depth:depthIdentifier, 0, CubemapFace.Unknown, sliceIndex);

	        if(useClearingColor || clearTheDepth){ 
	            Cmd.ClearRenderTarget(clearDepth:clearTheDepth, clearColor:useClearingColor, clearColor);
	        }
	        foreach(var r in renderers){ 
	            Cmd.DrawRenderer(r,material);
	        }
	        Graphics.ExecuteCommandBuffer(Cmd);
	        onPostRender?.Invoke();
	    }

	    public void Dispose(){
	        Cmd?.Release();
	        Cmd = null;
	    }
	}
}//end namespace
