#ifndef SP_SHADER_EFFECTS
#define SP_SHADER_EFFECTS

// Midpoint of 0.5 for neutral midtones
float4 AdjustContrast(float4 color, float contrast, float midpoint=0.5f){
	contrast = max(contrast, 0); // Ensure contrast is non-negative
	color.rgb = (color.rgb - midpoint) * contrast + midpoint;// Adjust contrast
	color.rgb = saturate(color.rgb);
	return color;
}


float3 hue2rgb(float hue){//from https://www.ronja-tutorials.com/post/041-hsv-colorspace/
	hue = frac(hue); //only use fractional part
	float r = abs(hue * 6 - 3) - 1; //red
	float g = 2 - abs(hue * 6 - 2); //green
	float b = 2 - abs(hue * 6 - 4); //blue
	float3 rgb = float3(r,g,b); //combine components
	rgb = saturate(rgb); //clamp between 0 and 1
	return rgb;
}

float3 hsv2rgb(float3 hsv){//from https://www.ronja-tutorials.com/post/041-hsv-colorspace/
	float3 rgb = hue2rgb(hsv.x); //apply hue
	rgb = lerp(1, rgb, hsv.y); //apply saturation
	rgb = rgb * hsv.z; //apply value
	return rgb;
}

float3 rgb2hsv(float3 rgb){//from https://www.ronja-tutorials.com/post/041-hsv-colorspace/
	float maxComponent = max(rgb.r, max(rgb.g, rgb.b));
	float minComponent = min(rgb.r, min(rgb.g, rgb.b));
	float diff = maxComponent - minComponent;
	float invDiff = 1.0f/diff;
	float hue = 0;
	if(maxComponent == rgb.r) {
		hue = 0+(rgb.g-rgb.b)*invDiff;
	} else if(maxComponent == rgb.g) {
		hue = 2+(rgb.b-rgb.r)*invDiff;
	} else if(maxComponent == rgb.b) {
		hue = 4+(rgb.r-rgb.g)*invDiff;
	}
	hue = frac(hue * 0.16666667f);
	float saturation = diff / maxComponent;
	float value = maxComponent;
	return float3(hue, saturation, value); 
}


float4 EffectsPostProcess(float4 sampledColor, float4 HSV_and_Contrast){
	float3 sampledColor_HSV = rgb2hsv(sampledColor.rgb);
	float hueShift = HSV_and_Contrast.x;
	float sat      = HSV_and_Contrast.y;
	float val      = HSV_and_Contrast.z;
	float contrast = HSV_and_Contrast.w;
	sampledColor_HSV.x  = frac(sampledColor_HSV.x + hueShift);
	sampledColor_HSV.y *= sat + 2*max(0, sat-1);  // 0--> 1 --> 3
	sampledColor_HSV.z *= val;
	sampledColor.rgb = hsv2rgb(sampledColor_HSV);
	sampledColor = AdjustContrast(sampledColor, contrast);
	return sampledColor;
}



bool isOutsideScreen_or_behind( float4 fragScreenSpacePos_dividedByW ){
    float3 xyz = fragScreenSpacePos_dividedByW.xyz;
    // check if the coordinate is outside the screen (if user zoomed up on a detail of meshes).
    // This ensures nothing will be tiled beyond the screen.
    float2 posFromCenter_abs = abs(xyz.xy - 0.5f);
    // We also need to check if z is less than 0 (behind camera). 
    // But we can avoid two if statements, and combine it into a single check.
    // We will subtract 0.5 and then negate the z during the max check. [-1,1] --> [-1.5, 0.5]
    // So even if z is -0.1, it will become -0.6 and when negated will be greater than 0.5.
    float z =  xyz.z-0.5; //Z is NOT ABSOLUTE

    float xyz_max =  max(max(posFromCenter_abs.x, posFromCenter_abs.y), -z);
    return xyz_max > 0.5f;
}

#endif