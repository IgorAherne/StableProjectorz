using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace spz {

	// Given a square 2D image that is cropped around the shiny chrome ball, it extracts panorama 2D texture.
	// Then, it converts the panorama into cubemap and assigns that to skybox.
	public class SkyboxReconstruction : MonoBehaviour
	{
	    [Tooltip("Assign your 1024x1024 reflection texture (the shiny ball image).")]
	    public Texture2D reflectionTexture;

	    [Tooltip("Assign the compute shader 'SkyboxReconstruction.compute'.")]
	    public ComputeShader computeShader;

	    [Tooltip("Width of the output panoramic texture.")]
	    public int outputWidth = 2048;

	    [Tooltip("Height of the output panoramic texture.")]
	    public int outputHeight = 1024;

	    private RenderTexture outputTexture;

	    void Start(){
	        // Create the output RenderTexture
	        outputTexture = new RenderTexture(outputWidth, outputHeight, 0, RenderTextureFormat.ARGBFloat);
	        outputTexture.enableRandomWrite = true;
	        outputTexture.wrapMode = TextureWrapMode.Repeat;  // This is important for panoramic
	        outputTexture.autoGenerateMips = true;  // Add this - Unity needs mips for reflections
	        outputTexture.useMipMap = true;         // Add this - Unity needs mips for reflections
	        outputTexture.Create();

	        // Find the kernel in the compute shader
	        int kernelHandle = computeShader.FindKernel("CSMain");

	        // Set textures for the compute shader
	        computeShader.SetTexture(kernelHandle, "ReflectionTexture", reflectionTexture);
	        computeShader.SetTexture(kernelHandle, "Result", outputTexture);

	        // Dispatch the compute shader
	        uint threadGroupSizeX, threadGroupSizeY, threadGroupSizeZ;
	        computeShader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

	        int dispatchSizeX = Mathf.CeilToInt(outputWidth / (float)threadGroupSizeX);
	        int dispatchSizeY = Mathf.CeilToInt(outputHeight / (float)threadGroupSizeY);

	        computeShader.Dispatch(kernelHandle, dispatchSizeX, dispatchSizeY, 1);

	        // Create a new panoramic skybox material and assign the output texture
	        var panoSkyboxMat = new Material(Shader.Find("Skybox/Panoramic"));
	        panoSkyboxMat.SetTexture("_MainTex", outputTexture);
	        RenderSettings.skybox = panoSkyboxMat;

	        Cubemap reflectionsCube = PanoramaToCubemap(512, panoSkyboxMat);

	        Material cubemapSkyboxMat = new Material(Shader.Find("Skybox/Cubemap"));
	        cubemapSkyboxMat.SetTexture("_Tex", reflectionsCube); // "_Tex" is the cubemap property name
	        RenderSettings.skybox = cubemapSkyboxMat;
	        RenderSettings.customReflectionTexture = reflectionsCube;
	        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

	        //RenderSettings.customReflectionTexture = reflectionsCube;
	        DynamicGI.UpdateEnvironment();
	    }


	    Cubemap PanoramaToCubemap(int cubemapSize, Material skyboxMaterial){
	        // Assign the skybox material to the RenderSettings temporarily
	        Material originalSkybox = RenderSettings.skybox;
	        RenderSettings.skybox = skyboxMaterial;
	        DynamicGI.UpdateEnvironment();

	        // Create a new camera
	        GameObject cameraGO = new GameObject("CubemapCamera", typeof(Camera));
	        Camera cubemapCamera = cameraGO.GetComponent<Camera>();
	        cubemapCamera.backgroundColor = Color.black;
	        cubemapCamera.clearFlags = CameraClearFlags.Skybox;
	        cubemapCamera.cullingMask = 0; // No need to render any objects
	        cubemapCamera.transform.position = Vector3.zero;

	        // Create the Cubemap
	        var cubemap = new Cubemap(cubemapSize, TextureFormat.RGBAFloat, false);

	        // Render the panoramic skybox into the Cubemap
	        cubemapCamera.RenderToCubemap(cubemap);

	        DestroyImmediate(cameraGO);
	        RenderSettings.skybox = originalSkybox; // Restore the original skybox
	        return cubemap;
	    }

	}
}//end namespace
