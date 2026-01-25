using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace spz {

	[ExecuteInEditMode]
	[RequireComponent(typeof(RectTransform))]
	public class AspectRatioHandler : UIBehaviour, ILayoutElement
	{
	    public enum AspectMode
	    {
	        WidthControlsHeight,
	        HeightControlsWidth
	    }

	    [SerializeField]
	    private AspectMode _aspectMode = AspectMode.WidthControlsHeight;

	    [SerializeField]
	    private float _aspectRatio = 1f;

	    [SerializeField]
	    private float _additionalPadding_px = 0f;

	    [Tooltip("If true, padding is added after aspect ratio calculation. If false, padding is added to the controlling dimension before calculation.")]
	    [SerializeField]
	    private bool _addPaddingAfterCalculation = true;

	    private RectTransform _rectTransform;
	    private float _width = -1;
	    private float _height = -1;

	    protected override void OnEnable()
	    {
	        base.OnEnable();
	        _rectTransform = GetComponent<RectTransform>();
	        UpdateRectTransform();
	    }

	    protected override void OnRectTransformDimensionsChange()
	    {
	        UpdateRectTransform();
	    }

	#if UNITY_EDITOR
	    protected override void OnValidate()
	    {
	        _aspectRatio = Mathf.Max(0.001f, _aspectRatio);
	        _additionalPadding_px = Mathf.Max(0f, _additionalPadding_px);
	        UpdateRectTransform();
	    }
	#endif

	    public void SetAspectRatio(float newAspectRatio)
	    {
	        _aspectRatio = Mathf.Max(0.001f, newAspectRatio);
	        UpdateRectTransform();
	    }

	    public void SetAspectMode(AspectMode newMode)
	    {
	        _aspectMode = newMode;
	        UpdateRectTransform();
	    }

	    public void SetAdditionalPadding(float padding)
	    {
	        _additionalPadding_px = Mathf.Max(0f, padding);
	        UpdateRectTransform();
	    }

	    private void UpdateRectTransform()
	    {
	        if (!IsActive() || !_rectTransform)
	            return;

	        LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
	    }

	    public void CalculateLayoutInputHorizontal()
	    {
	        if (!_rectTransform) return;

	        if (_aspectMode == AspectMode.HeightControlsWidth)
	        {
	            float height = _rectTransform.rect.height;
	            if (!_addPaddingAfterCalculation)
	            {
	                height += _additionalPadding_px;
	            }
	            _width = height * _aspectRatio;
	            if (_addPaddingAfterCalculation)
	            {
	                _width += _additionalPadding_px;
	            }
	        }
	        else
	        {
	            _width = -1;
	        }
	    }

	    public void CalculateLayoutInputVertical()
	    {
	        if (!_rectTransform) return;

	        if (_aspectMode == AspectMode.WidthControlsHeight)
	        {
	            float width = _rectTransform.rect.width;
	            if (!_addPaddingAfterCalculation)
	            {
	                width += _additionalPadding_px;
	            }
	            _height = width / _aspectRatio;
	            if (_addPaddingAfterCalculation)
	            {
	                _height += _additionalPadding_px;
	            }
	        }
	        else
	        {
	            _height = -1;
	        }
	    }

	    public float minWidth => _aspectMode == AspectMode.HeightControlsWidth ? _width : -1;
	    public float preferredWidth => _aspectMode == AspectMode.HeightControlsWidth ? _width : -1;
	    public float flexibleWidth => _aspectMode == AspectMode.HeightControlsWidth ? -1 : 1;
	    public float minHeight => _aspectMode == AspectMode.WidthControlsHeight ? _height : -1;
	    public float preferredHeight => _aspectMode == AspectMode.WidthControlsHeight ? _height : -1;
	    public float flexibleHeight => _aspectMode == AspectMode.WidthControlsHeight ? -1 : 1;
	    public int layoutPriority => 1;
	}
}//end namespace
