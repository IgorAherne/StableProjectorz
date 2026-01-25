#ifndef SP_ATLAS_SORT_INCLUDE
#define SP_ATLAS_SORT_INCLUDE


//first 8 bits represent 'order' in which mask is to be layed.
//So, we'll re-arrange the array, by the order.
// The contents of array are guaranteed to have unique order values. From 0 onwards, without gaps.
void sortBy_order_in_first8Bits( inout uint arr[MASKS_OF_ALL_ATLASES] ){
    // We will disregard empty masks in the loop soon. 
    // So before we proceed, make sure array is pre-filled with empty-masks values.
    //
    // BE VERY CAREFUL OPTIMIZING THIS CODE.  Remember out-of-bounds errors when you index with wanted ix or origIx.
    // Verify results via the Atlas_DebugVisualizer.cs script.
    uint rslt[MASKS_OF_ALL_ATLASES];
    for(int r=0; r<MASKS_OF_ALL_ATLASES; ++r){  rslt[r] = 255;  }

    for(int i=0; i<MASKS_OF_ALL_ATLASES; ++i){
        uint packedValue = arr[i];

        uint order = packedValue & 0xFF; //must correspond to what's in take_next_16_masks().
        uint wantedIx = order;
        bool empty = (wantedIx==255);//empty masks are always marked by order 255.

        wantedIx  = empty?  0 : wantedIx;// To avoid out of bounds access).
                                         // but, now we need ternary, to ensure 0th entry isn't overwritten:
        rslt[wantedIx] =  empty?  rslt[wantedIx] : packedValue;//'as is' if empty,  or 'packedValue' if order is ok.
    }
    // Copy sorted result back into the original array:
    for (int m=0; m<MASKS_OF_ALL_ATLASES; ++m){  arr[m] = rslt[m];  }
}


void unsortBy_originalIx( inout uint arr[MASKS_OF_ALL_ATLASES] ){
    uint rslt[MASKS_OF_ALL_ATLASES];
    for(int r=0; r<MASKS_OF_ALL_ATLASES; ++r){  rslt[r]=255;  }

    //Put back all values to their original locations.
    for(int i=0; i<MASKS_OF_ALL_ATLASES; ++i){
        uint packedValue = arr[i];
        uint order =  packedValue & 0xFF; //must correspond to what's in take_next_16_masks().
        bool empty =  order==255;//empty masks are always marked by order 255.

        uint origIx  = (packedValue >> 16) & 0xFF; //Original ix (when arr was still not-sorted).  
             origIx  = empty?  0 : origIx;         //Must correspond to what's in take_next_16_masks().
        rslt[origIx] = empty?  rslt[origIx] : packedValue;
    }
    // Copy sorted result back into the original array:
    for(int m=0; m<MASKS_OF_ALL_ATLASES; ++m){  arr[m] = rslt[m];  }
}


#endif