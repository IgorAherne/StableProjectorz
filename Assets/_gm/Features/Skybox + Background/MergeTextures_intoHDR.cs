using System;
using UnityEngine;

namespace spz {

	// Creates a high-dinamic-range texture which contains light intensities.
	// Avoids ghosting artifacts
	//
	// 1) Takes several images of the same surroundings, but at different exposures. (negative exposures, and 0 as highest).
	// 2) uses the highest exposure (EV0) as the base image for color/chrominance information
	// 3) For each pixel:
	//    - if not overexposed (luminance <= 0.9), keeps the original EV0 color
	//    - if it's overexposed, looks at lower exposures until it finds non-overexposed version
	//    - Preserves the color (chrominance) from EV0 but uses luminance from lower exposure
	//    - Scales the luminance back to linear HDR space using 2^(-EV)
	// The end result will be an HDR texture that captures both bright and dark details, without clipping at 1.0;
	// Consistent colors (from EV0) while luminance info comes from best exposure.
	//
	// Alpha channel stores the final luminance value (useful for effects like bloom)
	public class MergeTextures_intoHDR
	{
	    public struct ExposureInfo
	    {
	        public float EV;
	        public Texture2D texture;
	    }

	    private const float OVEREXPOSED_THRESHOLD = 0.9f;

	    // Rec. 709 luminance coefficients for linear RGB
	    private static readonly Vector3 LUMINANCE_COEFFS = new Vector3(0.2126f, 0.7152f, 0.0722f);

	    public static Texture2D MergeToHDR(ExposureInfo[] exposures){
	        // Sort exposures by EV from lowest to highest
	        System.Array.Sort(exposures, (a, b) => a.EV.CompareTo(b.EV));

	        int width = exposures[0].texture.width;
	        int height = exposures[0].texture.height;

	        // Create HDR texture (always linear)
	        Texture2D hdrTexture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
	        hdrTexture.filterMode = FilterMode.Bilinear;

	        // EV0 texture is the last one (highest EV)
	        Texture2D ev0Texture = exposures[exposures.Length - 1].texture;

	        for (int y = 0; y < height; y++)
	        {
	            for (int x = 0; x < width; x++)
	            {
	                // Get color from EV0 (base image) and ensure it's linear
	                Color pixel = ev0Texture.GetPixel(x, y);
                
	                // NOTICE: if it IS encoded as Gamma, you SHOULD convert to linear.
	                //Ensures calculations are performed in linear space regardless of input format
	                Color ev0Color = ev0Texture.isDataSRGB ? pixel.linear : pixel;
	                Vector3 finalColor = new Vector3(ev0Color.r, ev0Color.g, ev0Color.b);

	                // Calculate luminance in linear space
	                float luminance = Vector3.Dot(finalColor, LUMINANCE_COEFFS);

	                // Convert threshold to linear space for comparison
	                float linearThreshold = Mathf.GammaToLinearSpace(OVEREXPOSED_THRESHOLD);

	                if (luminance > linearThreshold)
	                {
	                    // Work through exposure pairs from lowest EV up
	                    for (int i = 0; i < exposures.Length - 1; i++)
	                    {
	                        Color lowerEvPixel = exposures[i].texture.GetPixel(x, y);
	                        // NOTICE: if it IS encoded as Gamma, you SHOULD convert to linear.
	                        Color lowerEvColor = ev0Texture.isDataSRGB ? lowerEvPixel.linear : lowerEvPixel;
	                        Vector3 lowerEvVec = new Vector3(lowerEvColor.r, lowerEvColor.g, lowerEvColor.b);
	                        float lowerEvLum = Vector3.Dot(lowerEvVec, LUMINANCE_COEFFS);

	                        // If this exposure isn't overexposed, use its luminance
	                        if (lowerEvLum <= linearThreshold)
	                        {
	                            // Apply exposure compensation in linear space
	                            float exposureScale = Mathf.Pow(2, -exposures[i].EV);
	                            float correctedLuminance = lowerEvLum * exposureScale;

	                            // Preserve chrominance from EV0 while using new luminance
	                            if (luminance > 0.0001f)
	                            {
	                                float scale = correctedLuminance / luminance;
	                                finalColor *= scale;
	                            }

	                            break;
	                        }
	                    }
	                }

	                // Calculate final luminance in linear space
	                float finalLuminance = Vector3.Dot(finalColor, LUMINANCE_COEFFS);

	                // Store final color (already in linear space) in HDR texture
	                hdrTexture.SetPixel(x, y, new Color(
	                    finalColor.x,
	                    finalColor.y,
	                    finalColor.z,
	                    finalLuminance
	                ));
	            }
	        }

	        hdrTexture.Apply();
	        return hdrTexture;
	    }

	}
}//end namespace
