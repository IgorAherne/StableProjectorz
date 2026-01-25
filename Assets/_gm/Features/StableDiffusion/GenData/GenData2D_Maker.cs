using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace spz {

	// Creates GenData2D objects, and stores them into the  dictionary<guid,genData>
	// This is cleaner than doing it manually in various locations of the project.
	public static class GenData2D_Maker{

	    // onBeforeNotifyAll: optional, you can use it to fine-tune the GenData2D.
	    // Afterwards, everyone will notified about it, including the 'GenData2D_Archive'.
	    public static GenData2D make_clonedGenData2D( GenData2D cloneThis, Action<GenData2D> onBeforeRegister=null ){
	        ProjectorCamera projCam;
        
	        GenData2D clone = new GenData2D(cloneThis, out projCam);//<--will spawn projectorCamera
	        onBeforeRegister?.Invoke(clone);

	        GenData2D_Archive.instance.WillGenerate(clone);
	        clone.ForceEvent_OnGenerationCompleted();//no more texture-render-updates expected, so force finish.

	        projCam?.Init(clone);
	        return clone;
	    }


	    public static GenData2D make_txt2img( SD_txt2img_payload txt2imgReq,  SD_GenRequestArgs_byproducts intermediates,
	                                          GenerationData_Kind kind){

	        ProjectorCamera projCam  = (kind==GenerationData_Kind.SD_ProjTextures) ? ProjectorCameras_MGR.instance.Spawn_ProjCamera()
	                                                                               : null;
	        Bounds bounds  = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();
	        List<CameraPovInfo> camerasInfos = UserCameras_MGR.instance.get_viewCams_PovInfos();

	        GenData2D genData = new GenData2D( kind, use_many_icons:true,  bounds.center,  camerasInfos, projCam,
	                                           txt2imgReq, null, null, intermediates );

	        GenData2D_Archive.instance.WillGenerate(genData);
	        projCam?.Init(genData);//after all the GenData2D + icons were created, init the projector camera.
	        return genData;
	    }



	    public static GenData2D make_img2img( SD_img2img_payload img2imgReq,  SD_GenRequestArgs_byproducts intermediates,
	                                          GenerationData_Kind kind ){

	        ProjectorCamera projCam  = (kind==GenerationData_Kind.SD_ProjTextures)? ProjectorCameras_MGR.instance.Spawn_ProjCamera()
	                                                                              : null;
	        Bounds bounds = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();
	        List<CameraPovInfo> camerasInfos = UserCameras_MGR.instance.get_viewCams_PovInfos();

	        GenData2D genData = new GenData2D( kind, use_many_icons:true,  bounds.center,  camerasInfos,
	                                           projCam, null, img2imgReq, null, intermediates);

	        GenData2D_Archive.instance.WillGenerate(genData);
	        projCam?.Init(genData);//after all the GenData2D + icons were created, init the projector camera.
	        return genData;
	    }



	    public static GenData2D make_img2extra( SD_img2extra_payload img2extraReq, 
	                                            GenData2D predecessorGen=null, 
	                                            SD_GenRequestArgs_byproducts intermediates=null ){

	        bool correctArgs = predecessorGen==null && intermediates!=null;
	             correctArgs|= predecessorGen!=null && intermediates==null;
	        Debug.Assert(correctArgs, "predecessorGen and intermediates can't both be null, and exactly one must be provided.");

	        GenData2D genData = predecessorGen!=null? make_img2extra(img2extraReq, predecessorGen)
	                                                : make_img2extra(img2extraReq, intermediates);
	        return genData;
	    }


	    static GenData2D make_img2extra( SD_img2extra_payload img2extraReq,  GenData2D predecessorGen){
	        //we are upscaling already existing image. Copy its GenData and clone its Projection camera.
	        GenData2D clone = make_clonedGenData2D(predecessorGen, OnBeforeRegisterClone);

	        void OnBeforeRegisterClone(GenData2D clone){
	            if (clone.txt2img_req != null){
	                clone.txt2img_req.width  = img2extraReq.rslt_imageWidths;
	                clone.txt2img_req.height = img2extraReq.rslt_imageHeights;
	            }
	            if(clone.img2img_req != null){
	                clone.img2img_req.width  = img2extraReq.rslt_imageWidths;
	                clone.img2img_req.height = img2extraReq.rslt_imageHeights;
	            }
	            if(clone.ext_req != null){
	                clone.ext_req.rslt_imageWidths  = img2extraReq.rslt_imageWidths;
	                clone.ext_req.rslt_imageHeights = img2extraReq.rslt_imageHeights;
	            }
	            // Make sure the new icon shows gray color, rather than copy of predecessor.
	            // This avoids confusion for the user.
	            Color clearingCol = Color.gray;
	            clone.ClearAllTextures_ToColor(clearingCol);
	        }
	        return clone;
	    }

	    static GenData2D make_img2extra( SD_img2extra_payload img2extraReq,  SD_GenRequestArgs_byproducts intermediates ){
	        //we are upscaling the View. Create a new projection camera from the current location:
	        GenerationData_Kind kind = GenerationData_Kind.SD_ProjTextures;
	        ProjectorCamera projCam  = ProjectorCameras_MGR.instance.Spawn_ProjCamera();

	        Bounds bounds = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();
	        List<CameraPovInfo> camerasInfos = UserCameras_MGR.instance.get_viewCams_PovInfos();

	        GenData2D genData = new GenData2D( kind, use_many_icons:true,  bounds.center,  camerasInfos, projCam, 
	                                           null, null, img2extraReq, intermediates );
	        GenData2D_Archive.instance.WillGenerate(genData);
	        projCam?.Init(genData);//after all the GenData2D + icons were created, init the projector camera.
	        return genData;
	    }


	    public static GenData2D make_AmbientOcclusion( RenderTexture textureArray_takeOwnership, 
	                                                   IReadOnlyList<UDIM_Sector> udims=null ){

	        Debug.Assert( textureArray_takeOwnership.dimension==TextureDimension.Tex2DArray, 
	                      $"{nameof(GenData2D_Maker)}.{nameof(make_AmbientOcclusion)} expected a textureArray." );

	        List<CameraPovInfo> camInfos =  UserCameras_MGR.instance.get_viewCams_PovInfos();
	        Bounds bounds =  ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();

	        var kind = GenerationData_Kind.AmbientOcclusion;

	        GenData2D genData =  new GenData2D( kind, use_many_icons:false, bounds.center, camInfos, projCamera:null);
	        genData.AssignTextures_Manual(textureArray_takeOwnership, udims);

	        GenData2D_Archive.instance.WillGenerate(genData);
	        genData.ForceEvent_OnGenerationCompleted();//no more texture-render-updates expected, so force finish.
	        return genData;
	    }


	    public static GenData2D make_ImportedCustomImages( GenerationData_Kind kind,
	                                                       List<Texture2D> textures_withoutOwner,
	                                                       List<UDIM_Sector> udims=null ){

	        bool isProjection =  kind == GenerationData_Kind.SD_ProjTextures;
	        udims =  CanUseUdims(kind)? udims : null;//drop udims if not expected for this kind (projection,bg,etc)

	        Bounds bounds = ModelsHandler_3D.instance.GetTotalBounds_ofSelectedMeshes();

	        bool onlyAllowOnePOV =  isProjection?false:true;
	        List<CameraPovInfo> camInfos = UserCameras_MGR.instance.get_viewCams_PovInfos(maxOneActive:onlyAllowOnePOV);

	        ProjectorCamera projCam =  isProjection ? ProjectorCameras_MGR.instance.Spawn_ProjCamera() : null;
	        var byproducts =  new SD_GenRequestArgs_byproducts();

	        bool use_many_icons =  ImportedFiles_use_several_icons(kind);
	        int masks_width  = kind==GenerationData_Kind.SD_Backgrounds? textures_withoutOwner[0].width : -1;
	        int masks_height = kind==GenerationData_Kind.SD_Backgrounds? textures_withoutOwner[0].height : -1;
	        var genData =  new GenData2D( kind, use_many_icons, bounds.center, 
	                                      camInfos, projCam, byproducts, masks_width, masks_height);
	        genData.AssignTextures_Manual(textures_withoutOwner, udims);

	        GenData2D_Archive.instance.WillGenerate(genData);
	        projCam?.Init(genData);//after all the GenData2D + icons were created, init the projector camera.
	        genData.ForceEvent_OnGenerationCompleted();//no more texture-render-updates expected, so force finish.
	        return genData;
	    }

	    //returns:
	    // does the Kind expect image to have its own icon (true),
	    // or does it allow to stack images into the same icon (false).
	    public static bool ImportedFiles_use_several_icons( GenerationData_Kind kind ){
	        switch (kind){
	            case GenerationData_Kind.Unknown: return false;
	            case GenerationData_Kind.TemporaryDummyNoPics: return false;
	            case GenerationData_Kind.SD_ProjTextures: return true;
	            case GenerationData_Kind.SD_Backgrounds: return true;
	            case GenerationData_Kind.UvTextures_FromFile: return false;
	            case GenerationData_Kind.UvPaintedBrush: return false;
	            case GenerationData_Kind.UvNormals_FromFile: return false;
	            case GenerationData_Kind.BgNormals_FromFile: return true;
	            case GenerationData_Kind.AmbientOcclusion: return false;
	            default: 
	                Debug.LogError("unknown type of imported image, don't know if show in 1 icon or several");
	                break;
	        }
	        return true;
	    }

	    // Does it make sense to use udims for a given Kind.
	    public static bool CanUseUdims( GenerationData_Kind kind ){
	        switch (kind){
	            case GenerationData_Kind.Unknown: return false;
	            case GenerationData_Kind.TemporaryDummyNoPics: return false;
	            case GenerationData_Kind.SD_ProjTextures: return false;
	            case GenerationData_Kind.SD_Backgrounds: return false;
	            case GenerationData_Kind.UvTextures_FromFile: return true;
	            case GenerationData_Kind.UvPaintedBrush: return true;
	            case GenerationData_Kind.UvNormals_FromFile: return true;
	            case GenerationData_Kind.BgNormals_FromFile: return false;
	            case GenerationData_Kind.AmbientOcclusion: return true;
	            default: 
	                Debug.LogError("unknown type of imported image, don't know if show in 1 icon or several");
	                break;
	        }
	        return true;
	    }
	}
}//end namespace
