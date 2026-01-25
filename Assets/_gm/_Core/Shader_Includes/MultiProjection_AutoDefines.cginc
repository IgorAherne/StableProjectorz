#ifndef SP_MULTI_PROJECTION_AUTODEFS
#define SP_MULTI_PROJECTION_AUTODEFS

    //POV (point of view) is position + rotation + fov of projector camera.
    //Camera can have several POV, which means it teleports a few times during a frame.
    //This can "shine" several screen-space art textures
    
    #if defined(NUM_POV_6)//Ensuring all the other defines are also enabled,
        #define NUM_POV_5 //because  #pragma multi_compile  NUM_POV_2  NUM_POV_3
        #define NUM_POV_4 //makes them mutually exclusive.
        #define NUM_POV_3
        #define NUM_POV_2
    #endif

    #if defined(NUM_POV_5)
        #define NUM_POV_4
        #define NUM_POV_3
        #define NUM_POV_2
    #endif

    #if defined(NUM_POV_4)
        #define NUM_POV_3
        #define NUM_POV_2
    #endif

    #if defined(NUM_POV_3)
        #define NUM_POV_2
    #endif


    #if defined(NUM_POV_6)
      #define _NumPOV 6

    #elif defined(NUM_POV_5)
      #define _NumPOV 5

    #elif defined(NUM_POV_4)
      #define _NumPOV 4

    #elif defined(NUM_POV_3)
      #define _NumPOV 3

    #elif defined(NUM_POV_2)
      #define _NumPOV 2
    #else
      #define _NumPOV 1
    #endif

#endif