using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace spz {

	public class SD_Upscalers_MainPanel_UI : MonoBehaviour
	{
	    [SerializeField] TMP_Dropdown _upscalersDropdown;
	    [SerializeField] Animation _anim;
	    [SerializeField] AnimationClip _attention_CLIP;
	    [Space(10)]
	    [SerializeField] Button _upscaleVisible_x2_button;
	    [SerializeField] Button _upscaleVisible_x4_button;

	    void Start(){
	        StaticEvents.SubscribeAppend<List<string>>("SD_Upscalers:ListUpdated", Populate_Dropdown);
	        StaticEvents.SubscribeAppend<bool>("SD_Upscalers:SetButtonsInteractable", SetButtonsInteractable);
	        StaticEvents.SubscribeAppend("SD_Upscalers:PlayAttentionAnim", PlayAttentionAnim);
	        StaticEvents.SubscribeAppend<string>("SD_Upscalers:SetSelectedByName", SetSelectedUpscaler);

	        _upscalersDropdown.onValueChanged.AddListener((ix) => StaticEvents.Invoke<int>("SD_Upscalers_UI", ix));
	        _upscaleVisible_x2_button.onClick.AddListener(() => StaticEvents.Invoke("SD_Upscalers_UI:OnUpscaleX2"));
	        _upscaleVisible_x4_button.onClick.AddListener(() => StaticEvents.Invoke("SD_Upscalers_UI:OnUpscaleX4"));
	    }

	    void OnDestroy(){
	        StaticEvents.Unsubscribe<List<string>>("SD_Upscalers:ListUpdated", Populate_Dropdown);
	        StaticEvents.Unsubscribe<bool>("SD_Upscalers:SetButtonsInteractable", SetButtonsInteractable);
	        StaticEvents.Unsubscribe("SD_Upscalers:PlayAttentionAnim", PlayAttentionAnim);
	        StaticEvents.Unsubscribe<string>("SD_Upscalers:SetSelectedByName", SetSelectedUpscaler);
	    }
    
	    private void PlayAttentionAnim(){
	        if (_anim == null || _attention_CLIP == null) return;
	        _anim.clip = _attention_CLIP;
	        _anim.Play();
	    }
    
	    private void SetButtonsInteractable(bool interactable){
	        if (_upscaleVisible_x2_button == null || _upscaleVisible_x4_button == null) return;
        
	        var artColor = _upscaleVisible_x2_button.image.color;
	        var bgColor  = _upscaleVisible_x4_button.image.color;
	        artColor.a = interactable ? 1f : 0.5f;
	        bgColor.a  = interactable ? 1f : 0.5f;
	        _upscaleVisible_x2_button.image.color = artColor;
	        _upscaleVisible_x4_button.image.color  = bgColor;
	    }
    
	    private void Populate_Dropdown(List<string> upscalerNames){
	        if (_upscalersDropdown == null) return;
        
	        string previousSelection = (_upscalersDropdown.options.Count > _upscalersDropdown.value && _upscalersDropdown.value >= 0) 
	            ? _upscalersDropdown.options[_upscalersDropdown.value].text 
	            : "";

	        _upscalersDropdown.ClearOptions();
	        _upscalersDropdown.AddOptions(upscalerNames.Select(name => new TMP_Dropdown.OptionData(name)).ToList());

	        int newIndex = -1;
	        if (!string.IsNullOrEmpty(previousSelection)){
	            newIndex = _upscalersDropdown.options.FindIndex(opt => opt.text == previousSelection);
	        }
        
	        if (newIndex >= 0){
	            _upscalersDropdown.SetValueWithoutNotify(newIndex);
	        } else if (_upscalersDropdown.options.Count > 0) {
	            _upscalersDropdown.SetValueWithoutNotify(0);
	        }
        
	        _upscalersDropdown.RefreshShownValue();
	    }
    
	    private void SetSelectedUpscaler(string upscalerName){
	        if (_upscalersDropdown == null) return;
	        int index = _upscalersDropdown.options.FindIndex(opt => opt.text == upscalerName);
	        if (index >= 0){
	            _upscalersDropdown.SetValueWithoutNotify(index);
	        }
	    }
	}
}//end namespace
