using Lavender.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace spz {

	// Can launch cmd window via python, inside the Shadow_R repo-folder.
	// It will produce images that have shadows reduced.
	// This script waits for those images, imports them, and then creates a new GenData2D from them.
	public class ShadowR_PythonRunner : MonoBehaviour{
    
	    [SerializeField] ShadowR_RepoInit _repoInit;//can download repo


	    void Start(){
	        GenerateButtons_UI.OnCancelGenerationButton += OnCancelShadowR_Button;
	    }

	    void OnDestroy(){
	        GenerateButtons_UI.OnCancelGenerationButton -= OnCancelShadowR_Button;
	    }


	    public void ReduceShadows_ShadowR( GenData2D fromThis ){
	        if( _repoInit.ShouldDownload()){
	            _repoInit.ShowPanel();
	            return;
	        }
	        SD_Neural_Models.instance.UnloadModelCheckpoint();
	        StartCoroutine( ShadowR_crtn(fromThis) );
	    }


	    IEnumerator ShadowR_crtn( GenData2D originalGen ){
	        if (StableDiffusion_Hub.instance._isGeneratingWhat != Generate_RequestingWhat.nothing){
	            string msg = "Cannot use Shadow R while StableDiffusion is already generating something";
	            Viewport_StatusText.instance.ShowStatusText(msg, false, 4, true);
	            yield break; 
	        }
	        bool isSuccess = StableDiffusion_Hub.instance.SubmitCustomWorkflow(Generate_RequestingWhat.Shadow_R_delighting, sendPayload:false);
	        if (!isSuccess){ yield break; }

	        GenerateButtons_UI.OnConfirmed_StartedGenerate();

	        string exeDirectory = Directory.GetParent(Application.dataPath).FullName;
	        string shadowR_path = Path.Combine(exeDirectory, "Shadow_R");

	        string runPath   = Path.Combine(shadowR_path, "run.bat");
	        string inputDir  = Path.Combine(shadowR_path, "code","input");
	        string outputDir = Path.Combine(shadowR_path, "code","output");
	        string extraArgs = $"--chunk_size {Settings_MGR.instance.get_ShadowR_chunkSize()}";
	        string runCommand = $"\"{runPath}\" --input_dir \"{inputDir}\" --output_dir \"{outputDir}\"" + " " + extraArgs;

	        string fullCommand = $"echo Consider closing other black windows to free more VRAM."
	                          + " && echo."  // first blank line
	                          + " && echo If nothing is printed, delete the 'Shadow_R' folder and retry download."
	                          + " && echo."  // second blank line
	                          + " && echo."  // third blank line
	                          + $" && call {runCommand}";

	        // Clear directories before starting, then store input images into directory
	        SD_FileUtils.CleanDirectory( inputDir );
	        SD_FileUtils.CleanDirectory( outputDir );
	        int numFilesNeeded =  Prepare_InputTextures_into_Dir(originalGen, inputDir );

	        string message = "Shadow R started, see its window for info.  If stuck, close other black windows." +
	            "\nEnable checkbox (Main View, top left corner).  Adjust the chunk-size in Settings.";
	        Viewport_StatusText.instance.ShowStatusText(message, false, 10, false);


	        yield return StartCoroutine( RunCommand_crtn(shadowR_path, fullCommand, isCanFinish) );


	        bool isCanFinish(){
	            int fileCount = SD_FileUtils.CountFiles_withExtensions(outputDir, ".png", ".jpg", ".tga");
	            if (fileCount != numFilesNeeded){ return false; }
	            // Only if we have the right number of files, check if they're all ready
	            return SD_FileUtils.IsAllFilesReady(outputDir, ".png", ".jpg", ".tga");
	        }

	        Get_OutputTextures_from_Dir(originalGen, outputDir);

	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:false);
	        StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();
	    }


	    void OnCancelShadowR_Button(){
	        if(StableDiffusion_Hub.instance._isGeneratingWhat != Generate_RequestingWhat.Shadow_R_delighting){
	            return;//someone else's generation.
	        }
	        StopAllCoroutines();
	        GenerateButtons_UI.OnConfirmed_FinishedGenerate(canceled:true);
	        StableDiffusion_Hub.instance.MarkCustomWorkflow_Done();
	    }


	    int Prepare_InputTextures_into_Dir(GenData2D originalGen, string inputDir ){
	        bool destroyWhenDone;
	        List<Texture2D> fromTheseTexs = originalGen.GetTextures2D_expensive(out destroyWhenDone).Keys.ToList();

	        for(int i=0; i<fromTheseTexs.Count; ++i){
	            Texture2D tex  = fromTheseTexs[i];
	            string texPath = Path.Combine(inputDir, i.ToString());
	            TextureTools_SPZ.EncodeAndSaveTexture(tex, texPath);
	        }
	        int numFiles_inDir = SD_FileUtils.CountFiles_withExtensions( inputDir, ".png", ".jpg", ".tga" );

	        if (destroyWhenDone){ 
	            fromTheseTexs.ForEach( t=> DestroyImmediate(t) );
	        }
	        return numFiles_inDir;
	    }

    
	    void Get_OutputTextures_from_Dir( GenData2D originalGen, string outputDir ){

	        GenData2D dataClone = GenData2D_Maker.make_clonedGenData2D(originalGen, OnBeforeRegisterClone);
        
	        void OnBeforeRegisterClone(GenData2D clone){
	            List<Texture2D> generatedTextures = TextureTools_SPZ.LoadTextures_FromDir(outputDir).ToList();
	            clone.AssignTextures_Manual( textures_withoutOwner:generatedTextures );
	        }
	    }



	    IEnumerator RunCommand_crtn( string workingDirectory,  string fullCommand,  Func<bool> func_isCanFinish)
	    {
	        Debug.Log($"Attempting to run command in directory: {workingDirectory}");
	        Debug.Log($"Full command: {fullCommand}");
	        uint processId = StartExternalProcess.Run_Bat_or_Shortcut_or_Command(fullCommand, isJustFile: false, workingDirectory, keepWindow:true);

	        if (processId == 0){
	            Debug.LogError("Failed to start process. Process ID is 0.");
	            yield break;
	        }
	        Debug.Log($"Process started with ID: {processId}");

	        while( func_isCanFinish() == false ){
	            yield return new WaitForSeconds(0.2f);
	        }
	    }

	}
}//end namespace
