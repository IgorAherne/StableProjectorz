#ifndef SP_BRUSH_EFFECTS
#define SP_BRUSH_EFFECTS

struct SegmentResult {
    float2 nearestPoint;
    float2 interpolatedSize;
    float interpolatedStrength;
};


//  Returns point on segment and also interpolated size and strength
// (from two values at the ends of the segment).
SegmentResult nearestPointOnSegment(float2 fromPoint, float2 segmentPointA, float2 segmentPointB, 
                                    float2 sizeA, float2 sizeB, float strengthA, float strengthB, float aspect){
    // Normalize points based on aspect ratio
    float2 normSegmentPointA = float2(segmentPointA.x * aspect, segmentPointA.y);
    float2 normSegmentPointB = float2(segmentPointB.x * aspect, segmentPointB.y);
    float2 normP = float2(fromPoint.x*aspect, fromPoint.y);

    float2 ab = normSegmentPointB - normSegmentPointA;
    float2 ap = normP - normSegmentPointA;

    // Compute the projection scalar, and then clamp it between 0 and 1
    float t = dot(ap, ab) / dot(ab, ab);
    t = saturate(t);

    // Calculate the nearest point and revert to original aspect
    SegmentResult result;
    float2 normNearestPoint = normSegmentPointA + t * ab;
    result.nearestPoint = float2(normNearestPoint.x / aspect, normNearestPoint.y);
    result.interpolatedSize = lerp(sizeA, sizeB, t);
    result.interpolatedStrength = lerp(strengthA, strengthB, t);

    return result;
}



struct PaintInBrushStroke_Input{
    float screenAspectRatio;
    //screen space position [0,0] to [1,1] of the current fragment.
    float2 fragScreenSpaceUV;
    //xy: previous brush coord in the viewport (during previous frame).  
    //zy: this frame brush coord.
    float4 PrevNewBrushScreenCoord; 
    //x: prev brush size (maybe pressure was different). 
    //y: this frame brush size.  z: 0,  w: is it first frame of painting the stroke.
    float4 BrushSizes_andFirstFrameFlag;
    //black and white image of brush (circular gradient, etc):
    sampler2D BrushStamp;
    
    // 0 when erasing a painted color mask
    // 0.5 when Erasing a projector mask.
    // This helps to make them similar, because colors are erasing very strongly, and brush looks thicker
    float brushStampStronger;
    
    float2 BrushStrength01; // Can be between 0 to 1.  x: previous, y: new
    
    //alpha where brush already painted during the current stroke. 
    //Allows to prevent ""building up" of color. Usually looks like a "worm" on the gray texture.
    // Can be beteween 0 to 1
    float currentBrushPath01; 
    
    // we'll diminish brushing for surfaces that face away from the camera.
    // Helpful to prevent accidentally painting sides of the mesh, leaving ugly stretches.
    // To use this correctly, your mesh needs to have 180-degree auto-smoothed normals.
    // Set to 1 if not used.
    float normalDotView;
};




float invLerp(float a, float b, float x){
    return (x-a)/(b-a);
}


//Helps to draw a continuous line between previous position and new position.
//Otherwise we would be placing dots if brush moves too quickly.
float PaintInBrushStroke(PaintInBrushStroke_Input i){
	// Calculate the brush texture coordinate
    float2 brushPrevPos =  i.PrevNewBrushScreenCoord.xy;
    float2 brushNewPos =   i.PrevNewBrushScreenCoord.zw;
    float2 brushPrevSize = i.BrushSizes_andFirstFrameFlag.xx; 
    float2 brushNewSize =  i.BrushSizes_andFirstFrameFlag.yy;
    float brushPrevStrength = i.BrushStrength01.x;
    float brushNewStrength =  i.BrushStrength01.y;

    SegmentResult segResult = nearestPointOnSegment( i.fragScreenSpaceUV, brushPrevPos, brushNewPos, 
                                                     brushPrevSize, brushNewSize, brushPrevStrength, brushNewStrength, i.screenAspectRatio);
    float2 brushNearPos = segResult.nearestPoint;
    float2 brushNearSize = segResult.interpolatedSize;
    float brushNearStrength = segResult.interpolatedStrength;

    brushNearSize.x /= i.screenAspectRatio;
    fixed2 brushUV_curr =  (i.fragScreenSpaceUV - brushNearPos);
           brushUV_curr =  (brushUV_curr + 0.5f*brushNearSize) / brushNearSize;
           brushUV_curr =  clamp(brushUV_curr, 0.0f, 1.0f);
                 
    float brushStamp = tex2D(i.BrushStamp, brushUV_curr).r; // Sample the brush texture
    brushStamp = 1-pow(1-brushStamp, 1+i.brushStampStronger);
    brushStamp = saturate(brushStamp);
    
    //fade out the brush stroke the more the surface is turned away from the camera, based on dot product:
    float normDotView = saturate(invLerp(0.2, 0.5, i.normalDotView));

    float wanted  = brushStamp * brushNearStrength * normDotView;
    float final   = max(wanted, i.currentBrushPath01);
    return final;//range is [0,1]
}


//0 everywhere on screen except inside the brush stamp
float Mask_by_CurrBrushCursor(PaintInBrushStroke_Input i){
    // Calculate the brush texture coordinate
    float2 brushPrevPos =  i.PrevNewBrushScreenCoord.xy;
    float2 brushNewPos =   i.PrevNewBrushScreenCoord.zw;
    float2 brushPrevSize = i.BrushSizes_andFirstFrameFlag.xx; 
    float2 brushNewSize =  i.BrushSizes_andFirstFrameFlag.yy;

     brushNewSize.x /= i.screenAspectRatio;
    fixed2 brushUV_curr =  (i.fragScreenSpaceUV - brushNewPos);
           brushUV_curr =  (brushUV_curr + 0.5f*brushNewSize) / brushNewSize;
           brushUV_curr =  clamp(brushUV_curr, 0.0f, 1.0f);
                 
    return tex2D(i.BrushStamp, brushUV_curr).r; // Sample the brush texture
}

#endif