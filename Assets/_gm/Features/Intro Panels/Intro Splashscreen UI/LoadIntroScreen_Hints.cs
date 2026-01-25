using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace spz {

	public class LoadIntroScreen_Hints : MonoBehaviour
	{
	    [SerializeField] TextMeshProUGUI _hintText;

	    List<string> _hints = new List<string>(){
	        "Black 3d models? Ensure UVs are non-overlapping.",
	        "Circle-sliders: just drag them left-right, not around.",
	        "Loras for custom style or Albedo: use their keywords in text prompt.",
	        "2+ materials? Arrange into UDIMs to make several textures.",
	        "Symmetry? Arrange 2 halves into UDIMs, to avoid visual glitches",
	        "Import a custom texture from the Art panel, or Art BG",
	        "CTRL + hover an icon to find it on your geometry",
	        "Hover viewport + hold 'R' to see the current projection in Red.",
	        "The 'No-Color brush' is perfect for re-thinking any seams.",
	        "Use FAR and SFT slider inside an icon to remove borders",
	        "CFG scale plays a big role in Inpaint. Try '12'. And the 'Re-Think'.",
	        "Press CTRL while orbiting: snaps to nearest 45 degrees.",
	        "Getting wireframe from SDXL? Blur the depth.",
	        "Increase cameras, render, then Blend them (using the +brush)",
	        "For a prompt-by-image, use a second ControlNet (IPAdapter).",
	        "Hold Right Mouse Button + WASD or QE to fly around the scene.",
	        "Press X to change brush color between black and white.",
	        "Press H to change brush hardness.  0,1,2,3.. to change strength.",
	        "'Where Empty' allows to \"Inpaint\" roughly, over the gaps.",
	        "Right click any slider to restore to a default value",
	        "Too many icons? Click a Bucket button at the top of the Art icon list.",
	        "Please make a video/post about StableProjectorz, to tell others.",
	        "Any Background Image secretly activates a full-object inpaint.",
	        "Try painting in the Color Mode, and render with 70% 'Re-Think' slider.",
	        "512 x 512 size is per projection, so final output is at least 2k res.",
	        "SDXL was made to render 1024 images, smaller sizes give weird results.",
	        "SD 1.5 was made to render 512 res, other sizes give weird results.",
	        "Higher resolutions work well for Multi-camera setup. 1024 for SD 1.5",
	        "Saving? DATA folder always stores a final 4k texture just in case.",
	        "Click 'ns' bucket-button in Art Panel, to delete non-selected icons.",
	        "Right click the Main Viewport to show a color palette. Or press SPACE.",
	        "Seamless terrain? Use IP Adapter, no depth ctrlnet. WhereEmpty + Tileable",
	        "Try enabling 2 control nets, Depth + Normalbae. Use correct input images!",//<--longest allowed sentence
	        "Projections leaking through thin surfaces? 'Settings -> Precision x4'",
	        "CTRL+G to Gen Art,  Ctrl+Shift+G to Gen BG!",
	        "Shift+W to toggle the wireframe.",
	        "Change settings in the bottom right corner, next to the connection icon.",
	        "Get help or updates by clicking 'settings' at the Bottom Right corner.",
	        "Consider supporting the project :)",
	    };

	    void Start(){
	        SelectNewHint();
	    }

	    void SelectNewHint(){
	        int currentIndex = PlayerPrefs.GetInt("IntroCurrentHintIndex", -1);
	        currentIndex++;

	        if (currentIndex >= _hints.Count){ //loop around
	            currentIndex = 0;
	        }
	        PlayerPrefs.SetInt("IntroCurrentHintIndex", currentIndex);
	        PlayerPrefs.Save();

	        _hintText.text = _hints[currentIndex];
	    }
	}
}//end namespace
