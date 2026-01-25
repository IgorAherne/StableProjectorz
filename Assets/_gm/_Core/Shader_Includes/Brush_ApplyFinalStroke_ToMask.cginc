#ifndef SP_BRUSH_APPLY_FINAL_STROKE_TO_MASK
#define SP_BRUSH_APPLY_FINAL_STROKE_TO_MASK


float accelerateBrushStrength(float x, float maxStrength) {
    float PI = 3.14159265359;
    // This function accelerates the effect based on how close we are to the target
    // and the maximum possible brush strength
    float t = x / maxStrength;
    return saturate(sin(t * PI * 0.5) * maxStrength);
}


float4 apply_brush_stroke_rgba(float4 currMask, float4 targetColor, float diff_m1_p1, float maxPossibleBrushStrength01 ){
    if (diff_m1_p1 > 0) {
        // Keep the simple approach for positive painting
        return lerp(currMask, targetColor, diff_m1_p1);
    } else {
        // For erasing, we want to accelerate more when we're closer to full opacity
        float decreaseStrength = -diff_m1_p1;
        float accelDecrease = accelerateBrushStrength(decreaseStrength, maxPossibleBrushStrength01);
        // Enhance erasing strength when closer to full opacity
        accelDecrease *= 1.0 + (1.0 - currMask.a) * 2.0;
        float4 blendedColor = lerp(currMask, float4(0,0,0,0), accelDecrease);
        // Snap to zero for very low alpha
        blendedColor.a *= step(0.02, blendedColor.a);
        return blendedColor;
    }
}


#endif