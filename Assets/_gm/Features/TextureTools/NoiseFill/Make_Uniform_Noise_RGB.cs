using UnityEngine;
using System.IO;

namespace spz {

	public class Make_Uniform_Noise_RGB : MonoBehaviour
	{
	    [SerializeField] private int textureSize = 512;
	    [SerializeField] private string saveFileName = "UniformNoiseRGB.png";

	    void Start()
	    {
	        GenerateNoiseTexture();
	    }

	    void GenerateNoiseTexture()
	    {
	        Texture2D noiseTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
	        noiseTexture.filterMode = FilterMode.Point;
	        noiseTexture.wrapMode = TextureWrapMode.Repeat;

	        Color[] colorArray = new Color[textureSize * textureSize];
	        System.Random random = new System.Random();

	        for (int i = 0; i < colorArray.Length; i++)
	        {
	            colorArray[i] = new Color(
	                (float)random.NextDouble(),
	                (float)random.NextDouble(),
	                (float)random.NextDouble()
	            );
	        }

	        noiseTexture.SetPixels(colorArray);
	        noiseTexture.Apply();

	        SaveTextureAsPNG(noiseTexture, saveFileName);
	    }

	    void SaveTextureAsPNG(Texture2D tex, string fileName)
	    {
	        byte[] bytes = tex.EncodeToPNG();
	        string path = Path.Combine(Application.dataPath, fileName);
	        File.WriteAllBytes(path, bytes);
	        Debug.Log("Saved texture to: " + path);
	    }
	}
}//end namespace
