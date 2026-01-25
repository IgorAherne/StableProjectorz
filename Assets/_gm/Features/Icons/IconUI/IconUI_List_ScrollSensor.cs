using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace spz {

	// sits inside the list that contains IconUI entities.
	// If mouse is dragging some IconUI and hovers this sensor,
	// It will cause the list to scroll in that direction.
	public class IconUI_List_ScrollSensor : MonoBehaviour
	{
	    [SerializeField] bool _isUpwardsSensor;
	    [SerializeField] ScrollRect _scrollRect;
	    [SerializeField] float _scrollSpeedMultiplier = 4;

	    public void Scroll(float itemHeight){
	        RectTransform contentRect  = _scrollRect.content;
	        RectTransform viewportRect = _scrollRect.viewport;
	        float scrollableHeight =  contentRect.rect.height - viewportRect.rect.height;

	        // Calculate scroll step (you might need to adjust the multiplier for speed)
	        float scrollStep = (itemHeight / scrollableHeight) * Time.deltaTime * _scrollSpeedMultiplier;

	        float normPos01  = _scrollRect.verticalNormalizedPosition;
	              normPos01 += _isUpwardsSensor? scrollStep : -scrollStep;
	              normPos01  =  Mathf.Clamp01(normPos01);
	        _scrollRect.verticalNormalizedPosition = normPos01;
	    }

	}
}//end namespace
