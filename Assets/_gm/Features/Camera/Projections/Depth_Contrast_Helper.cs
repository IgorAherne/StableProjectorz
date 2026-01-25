using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace spz {

	//sits as a child of RenderSequence_MGR.
	//Able to adjust contrast of the depth texture after it was prepared.
	//This ensures it's details are always best.
	// Respects the dimensions of the texture, can be rectangular or square.
	// If buffers (intermediate textures) are no longer applicable, they are re-created to match the new size
	public class Depth_Contrast_Helper : MonoBehaviour{

	    [Space(10)]
	    [SerializeField] Shader _depth_shader;
	    [SerializeField] Shader _blitDepthLatestCamera_add_shader;//additive, helps to gather depths into 1 texture.
	    [Space(0)]
	    [SerializeField] ComputeShader _minMaxDepth_shader;
	    [SerializeField] ComputeShader _depthContrast_shader;

	    Material _blitDepthLatestCamera_add_mat = null;

	    ComputeBuffer _minMaxDepth_buff = null;
	    List<RenderTexture> _intermediateTextures = new List<RenderTexture>();

	    //useful when performing reduction algorithm via compute shader.
	    //each thread will look at its 4x4 region, and perform comparisons inside it.
	    //Must be the same as in BOTH shaders (find-min-max-shader and in the contrast-shader).
	    const int THREAD_ZONE_SIDE = 4;

	    float _recalcMasks_prevTime = -9999;


	    public enum ContrastMode{
	        DontApply, 
	        ApplyMild, //with some depth offsets, expanding the actual range slightly more.
	        ApplyExact,//without any extra depth offsets. Suitable for non-processed depth, if differences in depth are tiny.
	    }

	    public class DepthContrast_arg{
	        public RenderTexture improveThisRT = null;
	        public ContrastMode contrastMode;
	        public float contrastAmount01;
	        public float brightnessAmount01;

	        public float blurStepSize01 = 0; //0 disables blur (good for performance)
	        public float blurSkipSamples_R_differenceGrtr = 1;
	        public int overrideNumIters = -1;

	        public float final_blurStepSize01 = 0;//0 disables final blur (good for performance)
	        public bool finalBlur_ignoreSamples_0rgb=true;//prevents Final Blur inside texels that are completely black

	        public bool warn_ifTooFrequent = true;//gives warning if we already invoked it during this frame.
	        public DepthContrast_arg( RenderTexture improveThisRT,  ContrastMode contrastMode, float contrastAmount01, float brightnessAmount01 ){
	            this.improveThisRT = improveThisRT;
	            this.contrastMode = contrastMode;
	            this.contrastAmount01 = contrastAmount01;
	            this.brightnessAmount01 = brightnessAmount01;
	        }
	    }


	    // Renders scene with the multi-Point-of-view depth shader.
	    // Then Looks at whatever depth is available from this recent render.
	    // Then blits it into a render texture in [0,1] range.
	    public void Get_BlackWhiteDepth_POVs( Camera cam,  List<CameraPovInfo> povs,  RenderTexture result_R16){

	        cam.allowMSAA  = false;//important to disable, otherwise edges of UV chunks remain blurry and affect dillation.
	        cam.clearFlags =  CameraClearFlags.Depth;
	        cam.enabled = false;
	        cam.targetTexture = result_R16;//doesn't matter. We only care about our depth buffer (internal, not part of this RT).

	        CameraTools.Set_POVs_properties_into_mat(null, cam, povs);

	        cam.RenderWithShader(_depth_shader, "");

	        _blitDepthLatestCamera_add_mat.SetFloat("_CloseIsWhite", 1);
	        _blitDepthLatestCamera_add_mat.SetFloat("_NearClip", cam.nearClipPlane);
	        _blitDepthLatestCamera_add_mat.SetFloat("_FarClip", cam.farClipPlane);
	        TextureTools_SPZ.SetKeyword_Material(_blitDepthLatestCamera_add_mat, "ENSURE_LINEAR_01_DEPTH", false);
	        TextureTools_SPZ.Blit(null, result_R16, _blitDepthLatestCamera_add_mat);
	    }


	    // Launches compute shaders to recalculate minimum and maxmimum depth in the texture.
	    // Then, uses the two values to make the depth more promiment.
	    // Expensive, so we usually we only need to do it once per frame.
	    //
	    // blurNumInvocations_6x6:  how many times to invoke the 6x6 kernel
	    //
	    // skipSamples_R_differenceGrtr:  For example 0.05  If we are bluring depth, we might want
	    // to ignore sample if its much darker than the main texel (far along z):
	    public void Improve_DepthContrast( DepthContrast_arg a ){
	        Ensure_NotOften(a);
	        if(a.contrastMode != ContrastMode.DontApply){
	            Init_Buffers();
	            Find_MinMaxDepth(a.improveThisRT);
	            Adjust_DepthContrast(a.improveThisRT, a);
	        }
	        Blur_Depth_maybe(a);
	    }


	    void Ensure_NotOften(DepthContrast_arg a){
	        float prev = _recalcMasks_prevTime;
	        _recalcMasks_prevTime = Time.time;

	        if(Time.time > prev){ return; }
	        if(a.warn_ifTooFrequent == false){ return; }
	        Debug.LogError("You already invoked 'Improve_DepthContrast()' during this frame. This is very expensive operation."
	                        + "\nYou can disable this warning by mentioning 'warn_ifTooFrequent=false' in the argument.");
	    }

	    void Init_Buffers(){
	        if(_minMaxDepth_buff==null){
	            _minMaxDepth_buff = new ComputeBuffer(2, sizeof(float));
	        }//else, don't bother releasing it, just reuse:
	        _minMaxDepth_buff.SetData(new float[2]{0.0f, 0.0f} );//atlases will add final total values into here.
	    }


	    void Find_MinMaxDepth( RenderTexture depthTex){
        
	        int kernelHandle_float1, kernelHandle_float2, kernelHandle_float2_final;
	        SetupVars_DepthIteration( out kernelHandle_float1, out kernelHandle_float2, out kernelHandle_float2_final);
	        int ix = -1;
	        RenderTexture dataTexture = depthTex;
	        while(true){
	            ix++;
	            RenderTexture intermediateRT = GetIntermediateRT(ix, dataTexture);

	            int groupsX, groupsY, groupsZ;
	            calcNumGroups(dataTexture, out groupsX, out groupsY, out groupsZ);

	            int kernelHandle;
	            SetupKernelID( ix,  kernelHandle_float1,  kernelHandle_float2,  kernelHandle_float2_final,
	                           dataTexture, out kernelHandle );

	            SetupTextures(ix, kernelHandle, dataTexture, intermediateRT);

	            _minMaxDepth_shader.Dispatch(kernelHandle, groupsX, groupsY, groupsZ);
            
	            dataTexture = intermediateRT;//smaller and smaller with each iteration
	            if(kernelHandle == kernelHandle_float2_final){ break; }
	        }//end while
	    }


	    void SetupVars_DepthIteration( out int kernelHandle_float1_, out int kernelHandle_float2_, 
	                                   out int kernelHandle_float2_final_){
	        kernelHandle_float1_ = _minMaxDepth_shader.FindKernel("CSMain_4x4_float1");
	        kernelHandle_float2_ = _minMaxDepth_shader.FindKernel("CSMain_4x4_float2");
	        kernelHandle_float2_final_ = _minMaxDepth_shader.FindKernel("CSMain_4x4_float2_final");
	        Debug.Assert(kernelHandle_float1_>=0  &&  kernelHandle_float2_>=0  &&  kernelHandle_float2_final_>=0);

	        _minMaxDepth_shader.SetBuffer(kernelHandle_float2_final_, "_Depth_MinMax_Final", _minMaxDepth_buff);
	    }


	    // Respects the dimensions of the texture, can be rectangular or square.
	    // If buffers (intermediate textures) are no longer applicable, they are re-created to match the new size
	    RenderTexture GetIntermediateRT(int iterIx,  RenderTexture currentTexture){
	       var wh = new Vector2Int(
	            (currentTexture.width + THREAD_ZONE_SIDE-1) / THREAD_ZONE_SIDE,//ceil for integers. in cases like 5/4 we want to have 2, not 1.
	            (currentTexture.height+ THREAD_ZONE_SIDE-1) / THREAD_ZONE_SIDE //Otherwise the second 4x4 wouldn't have anywhere to store result.
	        );                                                                 //Ceil helps to prevent loss of data in cases where the texture size 
	        wh.x = Mathf.Max(1, wh.x);                                         //isn't evenly divisible by THREAD_ZONE_SIDE.
	        wh.y = Mathf.Max(1, wh.y);

	        if (iterIx <_intermediateTextures.Count){
	            RenderTexture rt = _intermediateTextures[iterIx];
	            if(rt.width==wh.x  &&  rt.height==wh.y){ return rt; }//we have texure, and it matches the expected width and height.
	            else{
	                //the texture in list is of different size, so remove it and all after it. 
	                //We will need to re-allocate them:
	                for(int i=iterIx; i<_intermediateTextures.Count; ++i){ DestroyImmediate(_intermediateTextures[i]); }
	                int numToRemove = _intermediateTextures.Count - iterIx;
	                _intermediateTextures.RemoveRange(iterIx, numToRemove);
	                //don't return yet.
	            }
	        }
	        RenderTexture intermediateRT = new RenderTexture( wh.x, wh.y,  0, //NOTICE, two channel float texture: (minDepth, maxDepth)
	                                                          UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16_SFloat, mipCount:1 );
	        intermediateRT.filterMode = FilterMode.Point;
	        intermediateRT.antiAliasing = 1;
	        intermediateRT.anisoLevel = 1;
	        intermediateRT.enableRandomWrite = true;
	        _intermediateTextures.Add(intermediateRT);
	        return intermediateRT;
	    }


	    void SetupKernelID( int iterIx,  int kernelHandle_float1,  int kernelHandle_float2,  int kernelHandle_float2_final,
	                        RenderTexture currTexture,  out int kernelHandle_){ 
	        if (iterIx==0){
	            kernelHandle_ = kernelHandle_float1;
	            return;
	        }
	        bool isFinal = true;
	        isFinal &= currTexture.width<=THREAD_ZONE_SIDE;
	        isFinal &= currTexture.height<=THREAD_ZONE_SIDE;
	        kernelHandle_ =  isFinal?  kernelHandle_float2_final : kernelHandle_float2;
	    }


	    void SetupTextures(int ix, int kernelHandle, RenderTexture currTex, RenderTexture outputTex){
	        if (ix == 0){ 
	            _minMaxDepth_shader.SetTexture(kernelHandle, "_DepthTexture_f1", currTex);
	        }else { 
	            _minMaxDepth_shader.SetTexture(kernelHandle, "_DepthTexture_f2", currTex);
	        }
	        _minMaxDepth_shader.SetInt("_DepthTexture_Width", currTex.width);
	        _minMaxDepth_shader.SetInt("_DepthTexture_Height", currTex.height);
	        _minMaxDepth_shader.SetTexture(kernelHandle, "_OutputTexture_f2", outputTex);
	    }


	    void Adjust_DepthContrast(RenderTexture normalizeMe, DepthContrast_arg a){
	        Debug.Assert(normalizeMe.enableRandomWrite);

	        string kernelName =  a.contrastMode==ContrastMode.ApplyExact?  "CSMain_4x4_float1_strict" : "CSMain_4x4_float1";
	        int kernelHandle  = _depthContrast_shader.FindKernel(kernelName);
	        Debug.Assert(kernelHandle>=0);

	        _depthContrast_shader.SetTexture(kernelHandle, "_OutputTexture_f1", normalizeMe);
	        _depthContrast_shader.SetInt("_Output_Width", normalizeMe.width);
	        _depthContrast_shader.SetInt("_Output_Height", normalizeMe.height);
	        _depthContrast_shader.SetFloat("_Contrast01", a.contrastAmount01);
	        _depthContrast_shader.SetFloat("_Brightness01", a.brightnessAmount01);

	        _depthContrast_shader.SetBuffer(kernelHandle, "_Depth_MinMax", _minMaxDepth_buff);

	        int groupsX, groupsY, groupsZ;
	        calcNumGroups(normalizeMe, out groupsX, out groupsY, out groupsZ);
	        _depthContrast_shader.Dispatch(kernelHandle, groupsX, groupsY, groupsZ);
	    }


	    void calcNumGroups(RenderTexture tex, out int groupsX, out int groupsY, out int groupsZ){
	        //thread looks at 4x4 area next to it. So, threads of group are spaced apart by that much:
	        Vector3Int xyz = ComputeShaders_MGR.computeShaders_threadsXYZ;
	        xyz.x *= THREAD_ZONE_SIDE;
	        xyz.y *= THREAD_ZONE_SIDE;
	        // Effectively rounds up the result of the division. This ensures that every block
	        // still gets processed, even partial, by dispatching an extra thread group for it:
	        groupsX = (tex.width + xyz.x -1)/xyz.x;
	        groupsY = (tex.height+ xyz.y -1)/xyz.y;
	        groupsX = groupsX==0?1:groupsX;
	        groupsY = groupsY==0?1:groupsY;
	        groupsZ = 1;
	    }


	    void Blur_Depth_maybe(DepthContrast_arg a){

	        RenderTexture helper = RenderTexture.GetTemporary(a.improveThisRT.descriptor);
        
	        var bArg = new BlurTextures_MGR.BlurTextureArg( a.improveThisRT,  helper, 
	                                                        blurBoxHalfSize_1_to_12: 7,//half size 7, 14x14 kernel.
	                                                        stepLength: a.blurStepSize01 );

	        bArg.farSteps_amplification01 =  BlurTextures_MGR.calc_StepAmplification(a.improveThisRT);
	        bArg.ignoreSamples_thatHave_0rgb  = true;
	        bArg.skipSample_if_differenceGrtr = a.blurSkipSamples_R_differenceGrtr;
	        bArg.blurByChannel = BlurByChannel.R;//because our depth texture has only R.

	        //because we'll use 'farSteps_amplification01', we don't need many iterations (tested, Sept 2024).
	        int numInvoke = 1;  
	        int texRes = Mathf.Max(a.improveThisRT.width, a.improveThisRT.height);
	        if(texRes >= 512 ){ numInvoke = 2; }
	        if(texRes >= 4096){ numInvoke = 3; }
	        numInvoke = a.overrideNumIters>0? a.overrideNumIters : numInvoke;
    
	        if(a.blurStepSize01 > 0){ 
	            for(int i=0; i<numInvoke; ++i){
	                BlurTextures_MGR.instance.Blur_texture( bArg ); 
	            }
	        }
        
	        if(a.final_blurStepSize01 > 0){
	            //farSteps_amplification01: keep same.
	            bArg.stepLength =  a.final_blurStepSize01;
	            bArg.blurBoxHalfSize_1_to_12 = 3;  //half size 3, for a 6x6 kernel  - final pass is a smaller (faster) kernel.
	            bArg.ignoreSamples_thatHave_0rgb = a.finalBlur_ignoreSamples_0rgb;
	            bArg.skipSample_if_differenceGrtr = 1;//don't skip anything for final blurs. Helps to smooth sharp edges.

	            for(int i=0; i<numInvoke; ++i){
	                BlurTextures_MGR.instance.Blur_texture( bArg );
	            }
	        }

	        RenderTexture.ReleaseTemporary(helper);
	    }

	    void Awake(){
	        _blitDepthLatestCamera_add_mat = new Material(_blitDepthLatestCamera_add_shader);
	    }

	    void OnDestroy(){
	        DestroyImmediate(_blitDepthLatestCamera_add_mat);
	        _blitDepthLatestCamera_add_mat = null;

	        if (_minMaxDepth_buff!=null){
	            _minMaxDepth_buff.Release();
	            _minMaxDepth_buff = null;
	        }
	        _intermediateTextures.ForEach(tex => DestroyImmediate(tex) );
	        _intermediateTextures.Clear();
	    }
	}
}//end namespace
