using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

namespace spz {

	// Holds sliders, buttons, which are used by the other script, Settings_MGR.
	public class Settings_UI : MonoBehaviour
	{
	    [SerializeField] Button _openHelpSettingsPanel_button;
	    [SerializeField] Button _openSettingsPanel_button;
	    [SerializeField] GameObject _settingsPanel_go;
	    [SerializeField] ScrollRect_AutoScroll _autoScroll;
	    [Space(10)]
	    [SerializeField] ColorPalette_Panel_UI _settings_colorPicker;
	    [Space(10)]
	    [SerializeField] IntegerInputField _targetFrameRate_input;
	    [FormerlySerializedAs("_brushPrecision_2048_toggle")][SerializeField] Toggle _brushPrecision_4k_toggle;
	    [SerializeField] Toggle _prompt_textHighlight_toggle;
	    [SerializeField] Toggle _alwaysFocusCameraPivot;
	    [SerializeField] Toggle _show_cameraInfoText_toggle;
	    [SerializeField] Toggle _enableTooltips_toggle;
	    [SerializeField] Toggle _avoid_NSFW_generations_toggle;
	    [SerializeField] Toggle _viewport_in_center_toggle;
	    [SerializeField] Toggle _viewport_isSwapVerticalRibbons_toggle;
	    [SerializeField] Button _wireframeColor_button;
	    [SerializeField] SliderUI_Snapping _prompt_textSize_slider;
	    [SerializeField] SliderUI_Snapping _wireframeOpacity_slider;
	    [SerializeField] SliderUI_Snapping _shadowR_chunkSize_slider;
	    [SerializeField] TextMeshProUGUI _shadowR_chunkSize_descript;
	    [SerializeField] Button _restoreDefaults_button;
	    [SerializeField] SliderUI_Snapping _uvWarpSpeed_slider;
	    [Space(10)]
	    [SerializeField] SliderUI_Snapping _bgNoiseSpeed_slider;
	    [SerializeField] Button _noiseColor_button;
	    [Space(10)]
	    [SerializeField] Toggle _layout_askServerOften_toggle;//helpful if we are developing a ui layout txt document.
	    [SerializeField] Toggle _useCtrlScroll_WorkflowMode_swaps_toggle;//ProjMask ->Color -> No Color.
	    [SerializeField] Toggle _ignoreCtrl_if_clickSelectMeshes_toggle;//holding ctrl will not activate the 'ClickSelect_Meshes mode'.

	    void Start(){
	        // Buttons
	        EventsBinder.Bind_Clickable_to_event("Settings:OpenHelpSettingsPanel", _openHelpSettingsPanel_button);
	        EventsBinder.Bind_Clickable_to_event("Settings:OpenSettingsPanel", _openSettingsPanel_button);
	        EventsBinder.Bind_Clickable_to_event("Settings:OnButton_WireframeColor", _wireframeColor_button);
	        EventsBinder.Bind_Clickable_to_event("Settings:OnButton_NoiseColor", _noiseColor_button);
	        EventsBinder.Bind_Clickable_to_event("Settings:OnButton_RestoreDefaults", _restoreDefaults_button);

	        // Toggles
	        EventsBinder.Bind_Clickable_to_event("Settings:set_brushPrecision_res", _brushPrecision_4k_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_prompt_textHighlight", _prompt_textHighlight_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_isAlwaysFocusCameraPivot", _alwaysFocusCameraPivot);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_isShow_CameraInfoText", _show_cameraInfoText_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_isAllowTooltips", _enableTooltips_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_avoid_NSFW_generations", _avoid_NSFW_generations_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_viewport_in_center", _viewport_in_center_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_viewport_isSwapVerticalRibbons", _viewport_isSwapVerticalRibbons_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_layout_askServerOften", _layout_askServerOften_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_useCtrlScroll_for_WorkflowMode_swaps", _useCtrlScroll_WorkflowMode_swaps_toggle);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_ignoreCtrl_if_clickSelectingMeshes", _ignoreCtrl_if_clickSelectMeshes_toggle);

	        // Custom Sliders
	        EventsBinder.Bind_Clickable_to_event("Settings:set_prompt_textSize", _prompt_textSize_slider);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_wireframeOpacity", _wireframeOpacity_slider);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_ShadowR_chunkSize", _shadowR_chunkSize_slider);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_uvWarpSpeed01", _uvWarpSpeed_slider);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_noiseSpeed", _bgNoiseSpeed_slider);

	        // Custom Input Fields
	        EventsBinder.Bind_Clickable_to_event("Settings:set_targetFrameRate", _targetFrameRate_input);

	        EventsBinder.Bind_Clickable_to_event("Settings:ColorPicker", _settings_colorPicker);

	        EventsBinder.Bind_Clickable_to_event("Settings:SettingsPanel", _settingsPanel_go);
	        EventsBinder.Bind_Clickable_to_event("Settings:AutoScroll", _autoScroll);
	        EventsBinder.Bind_Clickable_to_event("Settings:set_ShadowR_chunkSize_descript_text", _shadowR_chunkSize_descript);
	    }
	}
}//end namespace
