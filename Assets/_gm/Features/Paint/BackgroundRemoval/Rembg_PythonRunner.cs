using Lavender.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


namespace spz {

	// Similar to ShadowR_PythonRunner, but for "rembg-stable-projectorz" repository.
	// It can remove backgrounds from images, reading them from code/input, saving output to code/output.
	public class Rembg_PythonRunner : MonoBehaviour{
	    public static Rembg_PythonRunner instance { get; private set; } = null;


	    [SerializeField] Rembg_RepoInit _repoInit; // Can download the "inspyrenet-stable-projectorz" repo


	    public class Rembg_arg{
	        public int backgroundThresh_0_255 = 20;
	        public int foregroundThresh_0_255 = 210;
	        public List<Texture2D> input;
	        public bool destroyInputTextures_whenDone;
	        public Action<List<Texture2D>> onReady;
	    }

	    // Public entry method to remove backgrounds from a GenData2D object.
	    // deleteInputTextures_whenDone: do you want us to dispose of these input textures.
	    // Useful when they don't belong to anyone.
	    //
	    // NOTICE: remember to destroy the Texture2D returned via onReady, when you no longer need them.
	    public void RemoveBackground_Rembg( Rembg_arg arg ){
	        if (_repoInit.ShouldDownload()){
	            _repoInit.ShowPanel();
	            return;
	        }
	        SD_Neural_Models.instance.UnloadModelCheckpoint();
	        StartCoroutine( Rembg_crtn(arg) );
	    }

    
	    // runs the background removal workflow.
	    IEnumerator Rembg_crtn( Rembg_arg arg ){
	        // Ensure nothing else is generating:
	        if (StableDiffusion_Hub.instance._isGeneratingWhat != Generate_RequestingWhat.nothing){
	            string msg = "Cannot remove backgrounds while another generation is already in progress.";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 4, true);
	            yield break;
	        }
	        bool isSuccess = StableDiffusion_Hub.instance.SubmitCustomWorkflow( Generate_RequestingWhat.rembg_backgroundRemoval, 
	                                                                            sendPayload: false);
	        if (!isSuccess){ yield break; }

	        GenerateButtons_UI.OnConfirmed_StartedGenerate();

	        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	        string rembgPath = Path.Combine(exeDirectory, "rembg-stable-projectorz");

	        string runPath   = Path.Combine(rembgPath, "run.bat");
	        string inputDir  = Path.Combine(rembgPath, "code", "input");
	        string outputDir = Path.Combine(rembgPath, "code", "output");

	        string extraArgs = $"--alpha_matting --foreground_thresh {arg.foregroundThresh_0_255} --background_thresh {arg.backgroundThresh_0_255}";
	        string runCommand = $"\"{runPath}\" {extraArgs}";

	        string fullCommand = 
	              "echo Launching Rembg process... "
	            + "&& echo. && echo If stuck, close other windows."
	            + $"&& call {runCommand}";

	        // Clean input/output directories, then save the input images
	        SD_FileUtils.CleanDirectory(inputDir);
	        SD_FileUtils.CleanDirectory(outputDir);
	        int numFilesNeeded = Prepare_InputTextures_into_Dir(arg.input, inputDir);

	        if (arg.destroyInputTextures_whenDone){
	            arg.input.ForEach( t=>DestroyImmediate(t) );
	            arg.input.Clear();
	        }

	        string message = "Background Removal started. A black window may appear for background removal.";
	        Viewport_StatusText.instance.ShowStatusText(message, false, 10, false);

	        // Run the external process, and wait until output is ready
	        yield return StartCoroutine(RunCommand_crtn(rembgPath, fullCommand, isCanFinish));

	        bool isCanFinish(){
	            int fileCount = SD_FileUtils.CountFiles_withExtensions(outputDir, ".png", ".jpg", ".tga");
	            if (fileCount != numFilesNeeded){ return false; }
	            // Additionally confirm the files are not still being written
	            return SD_FileUtils.IsAllFilesReady(outputDir, ".png", ".jpg", ".tga");
	        }
	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled: false);
	        StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();

	        List<Texture2D> generatedTextures = TextureTools_SPZ.LoadTextures_FromDir(outputDir).ToList();
	        arg.onReady?.Invoke( generatedTextures );
	    }


	    void OnCancelRembg_Button(){
	        if (StableDiffusion_Hub.instance._isGeneratingWhat != Generate_RequestingWhat.rembg_backgroundRemoval){
	            return;
	        }
	        StopAllCoroutines();
	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled: true);
	        StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();
	    }


	    // Copies the textures from a GenData2D into the input folder so Rembg can process them.
	    // "inputDir" Path to the 'code/input/' folder in the rembg.
	    int Prepare_InputTextures_into_Dir( List<Texture2D> inputTextures,  string inputDir){

	        for (int i=0; i<inputTextures.Count; i++){
	            Texture2D tex  = inputTextures[i];
	            string texPath = Path.Combine(inputDir, i.ToString());
	            TextureTools_SPZ.EncodeAndSaveTexture(tex, texPath);
	        }
	        int numFiles_inDir = SD_FileUtils.CountFiles_withExtensions(inputDir, ".png", ".jpg", ".tga");
	        return numFiles_inDir;
	    }


	    // Launches a CMD that calls our run.bat script, then yields until `func_isCanFinish` is true.
	    IEnumerator RunCommand_crtn(string workingDirectory, string fullCommand, Func<bool> func_isCanFinish){
	        Debug.Log($"Attempting to run command in directory: {workingDirectory}");
	        Debug.Log($"Full command: {fullCommand}");

	        uint processId = StartExternalProcess.Run_Bat_or_Shortcut_or_Command(fullCommand, isJustFile: false, workingDirectory, keepWindow:true);

	        if (processId == 0){
	            Debug.LogError("Failed to start process. Process ID is 0.");
	            yield break;
	        }
	        Debug.Log($"Process started with ID: {processId}");

	        while (!func_isCanFinish()){
	            yield return new WaitForSeconds(0.2f);
	        }
	    }

	    void Awake(){
	        if(instance != null){ DestroyImmediate(this); return; }
	        instance = this;
	    }

	    void Start(){
	        GenerateButtons_UI.OnCancelGenerationButton += OnCancelRembg_Button;
	    }

	    void OnDestroy(){
	        GenerateButtons_UI.OnCancelGenerationButton -= OnCancelRembg_Button;
	    }
	}
}//end namespace
