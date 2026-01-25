using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace spz {

	//contains the information about the whole project.
	//Use it when saving the project. 
	[Serializable]
	public class StableProjectorz_SL{ //SL ('Save/Load'). 
	    public readonly string version;
	    public string filepath_with_exten;//filepath of the project-save file.
	    public string filepath_dataDir; //data directory containing resources (textures, etc) for the project-save-file.
	    public ProjectorCameras_SL projectorCameras;
	    public SD_GenSettingsInput_UI sd_genSettingsInput;
	    public SD_WorkflowRibbon_SL sd_workflowRibbon;
	    public Gen3D_WorkflowOptionsRibbon_SL gen3D_WorkflowOptionsRibbon;
	    public Generations2D_MGR_SL generations_MGR;
	    public Generate3D_Inputs_SL generate3D_inputs;
	    public ModelsHandler_3D_SL modelsHandler3D;
	    public ModelsHandler_3D_UI_SL modelsHandler3D_UI;
	    public SkyboxColorButtons_UI_SL skyboxColorButtons;//for storing gradients of Background;
	    public Art2D_IconsList_SL art2D_iconList;//for icons of projections, etc.
	    public ArtBG_IconsList_SL artBG_iconList;//for icons of backgrounds
	    public SceneResolution_SL sceneResolution;
	    public MainViewWindow_ToolsRibbon_SL mainViewWindow_ToolsRibbon;
	    public BrushRibbon_UI_SL brush_MGR;
	    public UserCameras_MGR_SL camerasMGR;
	    public ControlNetUnits_Panel_SL controlNetUnits_panel;
	    public ConnectionPanel_SL connectionPanel;
	    public StableProjectorz_SL(){ version = CheckForUpdates_MGR.CURRENT_VERSION_HERE; }

	    public static StableProjectorz_SL CreateFromJSON(string jsonString, out string resultMessage_){
	        try{
	            // Use class-type information, to support inheritance of objects:
	            var settings = new JsonSerializerSettings{ TypeNameHandling = TypeNameHandling.Auto, };
	            resultMessage_ = "Loaded OK";
	            return JsonConvert.DeserializeObject<StableProjectorz_SL>( jsonString, settings);
	        }catch (Exception ex){ // Catching a more general exception
	            resultMessage_ = "Couldn't load - " + ex.Message;
	            return null;
	        }
	    }

	    public void update_dataDir_toCurrent(string filepath_of_spzFile){
	        // Get the directory path of the .spz file, and the file name of the .spz file:
	        string directoryPath = Path.GetDirectoryName(filepath_of_spzFile);
	        string fileName = Path.GetFileNameWithoutExtension(filepath_of_spzFile);

	        string dataFolderName = fileName + "_Data";
	        filepath_dataDir = Path.Combine(directoryPath, dataFolderName);
	    }
	}



	[Serializable]
	public class ProjectorCameras_SL{
	    public List<ProjectorCamera_SL> projCameras;
	}


	[Serializable]
	public class ProjectorCamera_SL{
	    public string genGUID;
	    public List<ushort> myMeshes_uniqueIds;//each camera is only working for some meshes.
	}



	[Serializable]
	public class SD_GenSettingsInput_UI{ //for StableDiffusion_UI
	    public string positivePrompt;
	    public string negativePrompt;
	    public List<string> positivePromptPresets;
	    public List<string> negativePromptPresets;
	    public int positivePromptPreset_ix;
	    public int negativePromptPreset_ix;
	    public SD_InputNeuralModels_SL neural_models;
	    public SD_NeuralVAE_SL neural_vae;
	    public SD_NeuralRefiner_SL neural_refiner;
	    public SD_NeuralUpscaler_SL neural_upscaler;
	    public SD_InputSamplers_SL samplers;
	    public SD_InputSchedulers_SL scheduler;
	    public int sampleSteps;
	    public float cfg_scale;
	    public int seed;
	    public int width;
	    public int height;
	    public int batch_count;
	    public int batch_size;
	}



	[Serializable]
	public class Generate3D_Inputs_SL{//for Trellis 3D generaiton, etc
	    public List<string> positivePromptPresets;
	    public int positivePromptPreset_ix;
	    public string prompt_positive = "";

	    public Generate3D_Inputs_singleImage_SL singleImage;
	    public Generate3D_Inputs_multiImage_SL multiImage;
	}

	[Serializable]
	public class Generate3D_Inputs_singleImage_SL{
	    public string image_filename;//for example image0.png
	    public int seed = 1;
	    public float ss_guidance_strength = 7.5f;
	    public int ss_sampling_steps = 12;
	    public float slat_guidance_strength = 3f;
	    public int slat_sampling_steps = 12;
	    public float mesh_simplify_ratio = 0.95f;
	    public int texture_size = 1024;
	    public bool remove_background = true;
	}

	[Serializable]
	public class Generate3D_Inputs_multiImage_SL{
	    public List<string> image_filenames;//for example {image0.png, image1.png}
	    public int seed = 1;
	    public float ss_guidance_strength = 7.5f;
	    public int ss_sampling_steps = 12;
	    public float slat_guidance_strength = 3f;
	    public int slat_sampling_steps = 12;
	    public float mesh_simplify_ratio = 0.95f;
	    public int texture_size = 1024;
	    public bool remove_background = true;
	}


	[Serializable]
	public class SD_InputNeuralModels_SL{
	    public string selectedModel_name;
	}

	[Serializable]
	public class SD_NeuralVAE_SL{
	    public string selectedVAE_name;
	}

	[Serializable]
	public class SD_NeuralRefiner_SL{
	    public string selectedRefiner_name;
	    public float switch_at;
	}

	[Serializable]
	public class SD_NeuralUpscaler_SL{
	    public string selectedUpscaler_name;
	}

	[Serializable]
	public class SD_InputSamplers_SL{
	    public string selectedSampler_name;
	}

	[Serializable]
	public class SD_InputSchedulers_SL{
	    public string selectedScheduler_name;
	}


	[Serializable]
	public class SD_WorkflowRibbon_SL{
	    public float denoisingStrength;

	    public float maskBlur_stepLength;

	    public string workflowMode;
	    public bool ignoreDepthOrNormals;
	    public bool isUseSoftInpaint;//differential inpainting
	    public bool isTileable;//for seamless 2D textures
	}


	[Serializable]
	public class Generations2D_MGR_SL{
	    public Dictionary<string, GenData2D_SL> guid_to_genData;
	    public List<string> GUIDs_ordered;
	    public string latestGeneration_GUID;
	}


	[Serializable]
	public class GenData2D_SL{
	    public string internal_GUID;
	    public string kind;
	    public Vector3Serializable selected3dModel_pos;
	    public List<CameraPovInfo> camerasPOVs;

	    public int _projCamera_ix;//can be null (if we are for AO or 2Dbackground). Index of camera in ProjectionCameras

	    public SD_GenRequest2D_args_byproducts_SL byproductsOfRequest = null;
	    public SD_txt2img_payload txt2img_req = null; //what was used to make the inital request.
	    public SD_img2img_payload img2img_req = null; //what was used to make the inital request.
	    public SD_img2extra_payload ext_req = null; //what was used to make the initial request (upscale, etc)

	    public GeneratedTextures_SL generatedTextures = null;//will be receiving the generated textures.

	    public GenData_Masks_SL masking_utils = null;

	    public int n_iter;
	    public int batch_size;
	}


	[Serializable]
	public class SD_GenRequest2D_args_byproducts_SL{
	    public string usualView;//filepath with extensions
	    public string depth;//filepath with extensions
	    public string viewNormals;
	    public string vertexColors;
	    public string screenSpaceMask_WE;//filepath with extensions (with anti-Edge)
	    public string screenSpaceMask_NE;//filepath with extensions (No anti-Edge)
	}


	[Serializable]
	public class GeneratedTextures_SL{
	    public string texturePreference;
	    public bool use_many_icons; //it false, all the textures are to be represented by single ui icon.
	    public List<string> textureGuidsOrdered;
	    public List<string> textureFilepaths; //filepaths with extensions.
	    public List<UDIM_Sector> udims;
	}


	[Serializable]
	public class GenData_Masks_SL{
	    public List<RenderUdims_SL> objectUV_brushMasks;//filepaths with extensions.
	    public List<RenderUdims_SL> objectUV_visibilities;//filepaths with extensions.
	}


	[Serializable]
	public class RenderUdims_SL{
	    public List<string> textures;//filepaths with extensions.
	    public List<UDIM_Sector> udim_coords;//which squares of uv space the udims operate on.
	}



	[Serializable]
	public class ModelsHandler_3D_SL{
	    public float currModelRoot_scaleAfterImport;//<--for example, 0.001
	    public string currModelRootGO=null;//filepath with extension.
	    public List<SD_3D_Mesh_SL> meshes = new List<SD_3D_Mesh_SL>();
	    public List<ushort> selectedMeshes = new List<ushort>();//collection of 'SD_3D_Mesh.unique_id' numbers.
	}

	[Serializable]
	public class ModelsHandler_3D_UI_SL{
	    public bool is_show_VertexColors;
	}


	[Serializable]
	public class SD_3D_Mesh_SL{
	    public ushort unique_id;
	    public List<UDIM_Sector> udimSectors;
	}


	[Serializable]
	public class ControlNetUnits_Panel_SL{
	    public List<ControlNetUnit_SL> ctrl_units;
	}


	[Serializable]
	public class ControlNetUnit_SL{
	    public bool isEnabled;
	    public bool isLowVram;
	    public float preprocessorRes_factor;
	    public bool hasAtLeastSomeModel;
	    public string neural_model;
	    public string preProcessor;
	    public string controlMode; //promptMoreImportant, balanced, ctrlNetMoreIMportant
	    public float controlWeight;
	    public float startingControl_step;
	    public float endingControl_step;
	    public string whatImageToSend;
	    public string customImg_howResize;
	    public string myCustomImg;//filepath.
	}


	//gradient of the colors, shown as a background
	[Serializable]
	public class SkyboxColorButtons_UI_SL{
	    public SerializableColor color_bot;
	    public SerializableColor color_top;
	}


	[Serializable]
	public class IconsList_SL{
	    // Nov 2024: list instead of dictionary, otherwise users were
	    // getting bugs with icon order, when saving/loading:
	    public List<GenGUID_and_ArtIconsGroup_SL> generationGUID_to_iconGrps;
	    public string _mainSelectedIcon_groupGuid;
	    public IconsList_Header_SL header;
	}

	[Serializable]
	public class GenGUID_and_ArtIconsGroup_SL{
	    public string guid;
	    public ArtIconsGroup_SL groupSL;
	}


	[Serializable]
	public class Art2D_IconsList_SL : IconsList_SL{}


	[Serializable]
	public class ArtBG_IconsList_SL : IconsList_SL{
	    public bool isPretendNoBackground;
	}


	[SerializeField]
	public class IconsList_Header_SL{
	    public int numIcons_inGrid;
	    public bool bakeAO_withBlur;
	    public bool bakeAO_shineAbove;
	}

	[Serializable]
	public class Art2D_IconsList_Header_SL : IconsList_Header_SL{}

	[Serializable]
	public class ArtBG_IconsList_Header_SL : IconsList_Header_SL{}


	[Serializable]
	public class ArtIconsGroup_SL{
	    public int chosenIconIx = -1;
	    public List<IconUI_SL> icons; //might contain null entries, if some icons were deleted.
	    public bool showMyIcons_as_solo;
	    public bool hideMyIcons_please;
	}


	[Serializable]
	public class IconUI_SL{
	    public List<string> texture_guids;
	    public bool did_deinit;
	    public IconUI_Art2DContextMenu_SL art2DcontextMenu = null;
	    public IconUI_AOContextMenu_SL aoContextMenu = null;
	}


	[Serializable]
	public class IconUI_AOContextMenu_SL{
	    public AmbientOcclusionInfo aoInfo;
	}


	[Serializable]
	public class IconUI_Art2DContextMenu_SL{
	    public HueSatValueContrast hsvc;
	    public ProjBlendingParams projBlends;
	    public BackgroundBlendParams bgBlends;
	}



	[Serializable]
	public class SceneResolution_SL{
	    public int scene_texResolution;
	    public string scene_texFilterMode = "Bilinear";
	}


	[Serializable]
	public class MainViewWindow_ToolsRibbon_SL{
	    public bool isShowWireframe;
	    public float depthContrast;
	    public float depthBrightness;
	    public float depthBlur_stepSize;
	    public float depth_sharpBlur;
	    public float depthBlurFinal_stepSize;
	    public bool depth_finalBlur_inside;

	}

	[Serializable]
	public class BrushRibbon_UI_SL {
	    public int maskBrush_hardnessIx;
	    public ColorSerializable maskBrush_color;
	    public float maskBrush_opacity01;
	    public bool maskBrush_autoReset;
	    public float maskBrush_size01;
	    public bool maskBrush_showText;
	    public bool isColorlessMask;
	}

	[Serializable]
	public class Gen3D_WorkflowOptionsRibbon_SL{
	    public float rembg_foregroundThresh;
	    public float rembg_backgroundThresh;
	}


	[Serializable]
	public class UserCameras_MGR_SL { 

	}


	[Serializable]
	public class ConnectionPanel_SL {
	    public string ip;
	    public int port;
	}


	//Same as Unity's Vector3 but can be saved and loaded from disk.
	//Contains implicit conversions to automatically work when assigned to a Vector3.
	[Serializable]
	public class Vector3Serializable{
	    public float x;
	    public float y;
	    public float z;
	    public Vector3Serializable(float rX, float rY, float rZ){ x=rX; y=rY; z=rZ; }
	    public Vector3 toVec3() => new Vector3(x,y,z);
	    public static implicit operator Vector3(Vector3Serializable rValue) => new Vector3(rValue.x, rValue.y, rValue.z);
	    public static implicit operator Vector3Serializable(Vector3 rValue) => new Vector3Serializable(rValue.x, rValue.y, rValue.z);
	}


	//Same as Unity's Vector2 but can be saved and loaded from disk.
	//Contains implicit conversions to automatically work when assigned to a Vector2.
	[Serializable]
	public class Vector2Serializable{
	    public float x, y;
	    public Vector2Serializable(float x, float y){  this.x=x;  this.y=y; }
	    public Vector2 toVec2()=> new Vector2(x,y);
	    public static implicit operator Vector2(Vector2Serializable v) => new Vector2(v.x, v.y);
	    public static implicit operator Vector2Serializable(Vector2 v) => new Vector2Serializable(v.x, v.y);
	}

	[Serializable]
	public class QuaternionSerializable{
	    public float x, y, z, w;
	    public Quaternion toQuatern()=> new Quaternion(x,y,z,w);
	    public QuaternionSerializable(float x, float y, float z, float w){ this.x=x; this.y=y; this.z=z; this.w=w; }
	    public static implicit operator Quaternion(QuaternionSerializable q) => new Quaternion(q.x, q.y, q.z, q.w);
	    public static implicit operator QuaternionSerializable(Quaternion q) => new QuaternionSerializable(q.x, q.y, q.z, q.w);
	}

	[Serializable]
	public class ColorSerializable{
	    public float r, g, b, a;
	    public Color toColor() => new Color(r, g, b, a);
	    public ColorSerializable(float r, float g, float b, float a=1.0f){ this.r=r; this.g=g; this.b=b; this.a=a; }

	    public static implicit operator Color(ColorSerializable c) => new Color(c.r, c.g, c.b, c.a);
	    public static implicit operator ColorSerializable(Color c) => new ColorSerializable(c.r, c.g, c.b, c.a);
	}


	[Serializable]
	public struct SerializableColor{
	    public float r, g, b, a;
	    public SerializableColor(float r, float g, float b, float a = 1f)
	        => (this.r, this.g, this.b, this.a) = (r, g, b, a);

	    public static implicit operator Color(SerializableColor c)
	        => new(c.r, c.g, c.b, c.a);

	    public static implicit operator SerializableColor(Color c)
	        => new(c.r, c.g, c.b, c.a);

	    public static SerializableColor FromHex(string hex)
	        => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : default;

	    public string ToHex(bool alpha = false)
	        => alpha ? ColorUtility.ToHtmlStringRGBA((Color)this)
	                : ColorUtility.ToHtmlStringRGB((Color)this);
	}


	[Serializable] 
	public struct SerializableVector2{
	    public float x, y;
	    public SerializableVector2(float x, float y) => (this.x, this.y) = (x, y);
	    public static implicit operator Vector2(SerializableVector2 v) => new(v.x, v.y);
	    public static implicit operator SerializableVector2(Vector2 v) => new(v.x, v.y);
	}

	[Serializable] 
	public struct SerializableVector3{
	    public float x, y, z;
	    public SerializableVector3(float x, float y, float z) => (this.x, this.y, this.z) = (x, y, z);
	    public static implicit operator Vector3(SerializableVector3 v) => new(v.x, v.y, v.z);
	    public static implicit operator SerializableVector3(Vector3 v) => new(v.x, v.y, v.z);
	}

	[Serializable] 
	public struct SerializableVector4{
	    public float x, y, z, w;
	    public SerializableVector4(float x, float y, float z, float w) => (this.x, this.y, this.z, this.w) = (x, y, z, w);
	    public static implicit operator Vector4(SerializableVector4 v) => new(v.x, v.y, v.z, v.w);
	    public static implicit operator SerializableVector4(Vector4 v) => new(v.x, v.y, v.z, v.w);
	}
}//end namespace
