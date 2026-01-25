using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	// User-cameras are those that render the viewport for the user.
	// Tells which user-cameras should keep rendering, and which can be skipped.
	// This allows to optimize the project, because we only need some of them at once.
	public static class UserCameras_Permissions{
	    public static LocksHashset_OBJ vertexColorsCam_keepRendering { get; private set; } = new LocksHashset_OBJ();
	    public static LocksHashset_OBJ contentCam_keepRendering { get; private set; } = new LocksHashset_OBJ();
	    public static LocksHashset_OBJ normalsCam_keepRendering { get; private set; } = new LocksHashset_OBJ();
	    public static LocksHashset_OBJ depthCam_keepRendering { get; private set; } = new LocksHashset_OBJ();


	    public static void LockOrUnlock_ByType( CameraTexType type,  object whoRequests,  bool isLock ){
	        switch (type){
	            case CameraTexType.Unknown:  break;
	            case CameraTexType.Nothing:  break;
	            case CameraTexType.ViewUserCamera:  break;
	            case CameraTexType.ContentUserCam:  contentCam_keepRendering.LockOrUnlock(whoRequests,isLock); break;
	            case CameraTexType.DepthUserCamera:  depthCam_keepRendering.LockOrUnlock(whoRequests,isLock);  break;
	            case CameraTexType.NormalsUserCamera:  normalsCam_keepRendering.LockOrUnlock(whoRequests,isLock);  break;
	            case CameraTexType.VertexColorsUserCamera: vertexColorsCam_keepRendering.LockOrUnlock(whoRequests,isLock); break;
	            default: Debug.Log($"incorrect type in UserCameras_Permissions {type}");  break;
	        }
	    }

	    public static CameraTexType convert( WhatImageToSend_CTRLNET what){ 
	        switch (what){
	            case WhatImageToSend_CTRLNET.None: return CameraTexType.Nothing;
	            case WhatImageToSend_CTRLNET.Depth: return CameraTexType.DepthUserCamera;
	            case WhatImageToSend_CTRLNET.Normals: return CameraTexType.NormalsUserCamera;
	            case WhatImageToSend_CTRLNET.VertexColors: return CameraTexType.VertexColorsUserCamera; //content camera used during screen masking.
	            case WhatImageToSend_CTRLNET.ContentCam: return CameraTexType.ContentUserCam; //content camera used during screen masking.
	            case WhatImageToSend_CTRLNET.CustomFile: return CameraTexType.Nothing;
	            default: Debug.Log($"incorrect 'what' in UserCameras_Permissions {what}"); return CameraTexType.Unknown;
	        }
	    }

	    public static void Force_KeepRenderingCameras(bool isForce){
	        vertexColorsCam_keepRendering.keep_pretending_isLocked(isForce);
	        contentCam_keepRendering.keep_pretending_isLocked(isForce);
	        normalsCam_keepRendering.keep_pretending_isLocked(isForce);
	        depthCam_keepRendering.keep_pretending_isLocked(isForce);
	    }
	}
}//end namespace
