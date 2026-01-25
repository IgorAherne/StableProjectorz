using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

namespace spz {

	public static class KeyMousePenInput{

	    public static bool isLMBpressed(bool checkOnlyPen=false){
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.leftButton.isPressed;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.tip.isPressed;
	        if(isPenPressed){
	            isPenPressed &= Pen.current.firstBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.secondBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.thirdBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.fourthBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.eraser.isPressed==false;
	            if(!isPenPressed){ isMousePressed=false; }//became false. So reset mouse also, to ensure it's false. Fights otherwise.
	        }
	        if(checkOnlyPen){ return isPenPressed; }
	        return isMousePressed || isPenPressed;
	    }

	    public static bool isLMBpressedThisFrame(){
	        bool isMousePressed = Mouse.current!=null  &&  Mouse.current.leftButton.wasPressedThisFrame;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.tip.wasPressedThisFrame;
	        if(isPenPressed){
	            isPenPressed &= Pen.current.firstBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.secondBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.thirdBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.fourthBarrelButton.isPressed==false;
	            isPenPressed &= Pen.current.eraser.isPressed==false;
	            if(!isPenPressed){ isMousePressed=false; }//became false. So reset mouse also, to ensure it's false. Fights otherwise.
	        }
	        return isMousePressed || isPenPressed;
	    }

	    public static bool isLMBreleasedThisFrame(){
	        bool isMouseReleased =  Mouse.current!=null  &&  Mouse.current.leftButton.wasReleasedThisFrame;
	        bool isPenReleased   =  Pen.current != null  &&  Pen.current.tip.wasReleasedThisFrame;
	        return isMouseReleased || isPenReleased;
	    }


	    public static bool isRMBpressed(){
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.rightButton.isPressed;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.firstBarrelButton.isPressed;
	        return isMousePressed || isPenPressed;
	    }

	    public static bool isRMBpressedThisFrame(){
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.rightButton.wasPressedThisFrame;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.firstBarrelButton.wasPressedThisFrame;
	        return isMousePressed || isPenPressed;
	    }


	    public static bool isMMBpressed(){
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.middleButton.isPressed;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.secondBarrelButton.isPressed;
	        return isMousePressed || isPenPressed;
	    }

	    public static bool isMMBpressedThisFrame(){
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.middleButton.wasPressedThisFrame;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.secondBarrelButton.wasPressedThisFrame;
	        return isMousePressed || isPenPressed;
	    }

	    public static bool isMMBreleasedThisFrame(){
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.middleButton.wasReleasedThisFrame;
	        bool isPenPressed  =  Pen.current != null  &&  Pen.current.secondBarrelButton.wasReleasedThisFrame;
	        return isMousePressed || isPenPressed;
	    }


	    // NO NEED TO SCALE its output by Time.deltaTime
	    // See https://discussions.unity.com/t/mouse-sensitivity-changes-between-editor-and-built-exe/20038
	    public static Vector2 delta_cursor( bool normalizeByScreenDiagonal=true ){
	        float inv_screenDiagonal =  1.0f / Mathf.Sqrt(Screen.width*Screen.width + Screen.height*Screen.height);
	        Vector2 mouseDT =  Mouse.current!=null ?  Mouse.current.delta.ReadValue() : Vector2.zero;
	        Vector2 penDT   =  Pen.current != null ?  Pen.current.delta.ReadValue() : Vector2.zero;
	        mouseDT *= normalizeByScreenDiagonal? inv_screenDiagonal : 1;
	        penDT   *= normalizeByScreenDiagonal? inv_screenDiagonal : 1;
	        penDT.y *= -1; // Invert Y if necessary for tablet setup
	        return (mouseDT.sqrMagnitude > penDT.sqrMagnitude) ? mouseDT : penDT;
	    }


	    // NO NEED TO SCALE its output by Time.deltaTime
	    // See https://discussions.unity.com/t/mouse-sensitivity-changes-between-editor-and-built-exe/20038
	    public static Vector2 delta_while_LMBpressed( bool normalizeByScreenDiagonal=true ){
	        float inv_screenDiagonal =  1.0f / Mathf.Sqrt(Screen.width*Screen.width + Screen.height*Screen.height);
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.leftButton.isPressed;
	        bool isPenPressed   =  Pen.current != null  &&  Pen.current.tip.isPressed;
	        if (isPenPressed){
	            Vector2 dt =Pen.current.delta.ReadValue();
	            dt.y *= -1; // Invert Y if necessary for tablet setup
	            dt *= normalizeByScreenDiagonal? inv_screenDiagonal : 1;
	            return dt;
	        }
	        if(isMousePressed){//CHECKING MOUSE ONLY IF PEN ISN'T PRESSED. Otherwise they fight and make huge deltas.
	            Vector2 dt = Mouse.current.delta.ReadValue();
	            dt *= normalizeByScreenDiagonal ? inv_screenDiagonal : 1;
	            return dt;
	        }
	        return Vector2.zero;
	    }


	    // NO NEED TO SCALE its output by Time.deltaTime
	    // See https://discussions.unity.com/t/mouse-sensitivity-changes-between-editor-and-built-exe/20038
	    public static Vector2 delta_while_RMBpressed( bool normalizeByScreenDiagonal=true ){
	        float inv_screenDiagonal =  1.0f / Mathf.Sqrt(Screen.width*Screen.width + Screen.height*Screen.height);
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.rightButton.isPressed;
	        bool isPenPressed   =  Pen.current != null  &&  Pen.current.firstBarrelButton.isPressed;
	        if (isPenPressed){
	            Vector2 dt =Pen.current.delta.ReadValue();
	            dt.y *= -1; // Invert Y if necessary for tablet setup
	            dt *= normalizeByScreenDiagonal? inv_screenDiagonal : 1;
	            return dt;
	        }
	        if(isMousePressed){//CHECKING MOUSE ONLY IF PEN ISN'T PRESSED. Otherwise they fight and make huge deltas.
	            Vector2 dt = Mouse.current.delta.ReadValue();
	            dt *= normalizeByScreenDiagonal ? inv_screenDiagonal : 1;
	            return dt;
	        }
	        return Vector2.zero;
	    }


	    // NO NEED TO SCALE its output by Time.deltaTime
	    // See https://discussions.unity.com/t/mouse-sensitivity-changes-between-editor-and-built-exe/20038
	    public static Vector2 delta_while_MMBpressed( bool normalizeByScreenDiagonal=true ){
	        float inv_screenDiagonal =  1.0f / Mathf.Sqrt(Screen.width*Screen.width + Screen.height*Screen.height);
	        bool isMousePressed =  Mouse.current!=null  &&  Mouse.current.middleButton.isPressed;
	        bool isPenPressed   =  Pen.current != null  &&  Pen.current.secondBarrelButton.isPressed;
	        if (isPenPressed){
	            Vector2 dt =Pen.current.delta.ReadValue();
	            dt.y *= -1; // Invert Y if necessary for tablet setup
	            dt *= normalizeByScreenDiagonal? inv_screenDiagonal : 1;
	            return dt;
	        }
	        if(isMousePressed){//CHECKING MOUSE ONLY IF PEN ISN'T PRESSED. Otherwise they fight and make huge deltas.
	            Vector2 dt = Mouse.current.delta.ReadValue();
	            dt *= normalizeByScreenDiagonal ? inv_screenDiagonal : 1;
	            return dt;
	        }
	        return Vector2.zero;
	    }


	    public static Vector2 cursorScreenPos(){//entire window (NOT MAIN VIEW), pixel coords
	        Vector2 screenPos = Vector2.zero;
	        if (Mouse.current != null){ screenPos = Mouse.current.position.ReadValue(); }
	        else if (Pen.current != null){ screenPos = Pen.current.position.ReadValue(); }
	        return screenPos;
	    }

	    public static Vector2 cursorViewPos01(){//entire window (NOT MAIN VIEW) normalized in [0,1] range
	        Vector2 screenPos = Vector2.zero;
	        if (Mouse.current != null){ screenPos = Mouse.current.position.ReadValue(); }
	        else if (Pen.current != null){ screenPos = Pen.current.position.ReadValue(); }
	        screenPos /= new Vector2(Screen.width, Screen.height);
	        return screenPos;
	    }

	    //rectangle inside the entire window.
	    public static Vector2 cursorMainViewPos01(bool isInner_SD_view){
	        return isInner_SD_view? MainViewport_UI.instance.cursorInnerViewportPos01
	                              : MainViewport_UI.instance.cursorMainViewportPos01;
	    }

	    public static bool isKey_CtrlOrCommand_pressedThisFrame(){
	        if (Keyboard.current == null){ return false; }
	        if (Keyboard.current.ctrlKey.wasPressedThisFrame){ return true; }
	        if (Keyboard.current.leftCommandKey.wasPressedThisFrame){ return true; }
	        if (Keyboard.current.rightCommandKey.wasPressedThisFrame){ return true; }
	        return false;
	    }

	    public static bool isKey_CtrlOrCommand_pressed(){
	        if (Keyboard.current == null) { return false; }
	        if (Keyboard.current.ctrlKey.isPressed) { return true; }
	        if (Keyboard.current.leftCommandKey.isPressed){ return true; }
	        if (Keyboard.current.rightCommandKey.isPressed){ return true; }
	        return false;
	    }

	    public static bool isKey_Shift_pressed(){
	        if (Keyboard.current == null){ return false; }
	        if (Keyboard.current.shiftKey.isPressed) { return true; }
	        return false;
	    }
    
	    public static bool isKey_Shift_pressedThisFrame(){
	        if (Keyboard.current == null){ return false; }
	        return Keyboard.current.shiftKey.wasPressedThisFrame;
	    }

	    public static bool isKey_alt_pressed(){
	        if (Keyboard.current == null){ return false; }
	        if (Keyboard.current.altKey.isPressed) { return true; }
	        return false;
	    }

	    public static bool isKey_alt_pressedThisFrame(){
	        if (Keyboard.current == null){ return false; }
	        return Keyboard.current.altKey.wasPressedThisFrame;
	    }

	    // For example, text is being typed into an text prompt, etc.
	    // Usually we check it before recognising viewport shorcuts like 'R', or 'TAB'.
	    public static bool isSomeInputFieldActive() 
	        => EventSystem.current.currentSelectedGameObject != null &&
	           EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()!=null &&
	           EventSystem.current.currentSelectedGameObject.activeInHierarchy;

	}
}//end namespace
