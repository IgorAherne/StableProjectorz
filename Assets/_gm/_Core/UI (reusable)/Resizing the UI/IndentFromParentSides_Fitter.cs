using UnityEngine;
using UnityEngine.UI;

namespace spz {

	public enum OffsetValueStrategy
	{
	    value_as_pxl,//just apply raw pixel offset to the desired dimention
	    value_as_pcnt_ofOtherDimention,
	    value_as_pcnt_ofOwnDimention,
	}

	[System.Serializable]
	public struct OffsetKind
	{
	    public float _value;
	    public OffsetValueStrategy _strategy;
	}


	//allows to inset this element relative to sides of its parent transform. Each side can be indented via 3 modes.
	//Look at the OffsetValueStrategy enum for more information.
	// But good news is, even ContentSizeFitter on the parent will do :) you don't need vertical or horizontal group. 
	// Also, children (including THIS object) don't need to have LayoutElement
	[RequireComponent(typeof(RectTransform))]
	public class IndentFromParentSides_Fitter
	    : UI_with_OptimizedUpdates, ILayoutSelfController, ILayoutElement
	{
	    [Space(10)]
	    [SerializeField] OffsetKind _left = new OffsetKind();
	    [SerializeField] OffsetKind _right = new OffsetKind();
	    [SerializeField] OffsetKind _up = new OffsetKind();
	    [SerializeField] OffsetKind _down = new OffsetKind();

	    private DrivenRectTransformTracker m_Tracker;
	    private RectTransform m_Rect;

	    // Public getters
	    public OffsetKind Left => _left;
	    public OffsetKind Right => _right;
	    public OffsetKind Up => _up;
	    public OffsetKind Down => _down;

	    // Public setters
	    public void SetLeftOffset(OffsetKind left)
	    {
	        _left = left;
	        OnUpdate();
	    }

	    public void SetRightOffset(OffsetKind right)
	    {
	        _right = right;
	        OnUpdate();
	    }

	    public void SetUpOffset(OffsetKind up)
	    {
	        _up = up;
	        OnUpdate();
	    }

	    public void SetDownOffset(OffsetKind down)
	    {
	        _down = down;
	        OnUpdate();
	    }

	    protected override void OnEnable()
	    {
	        base.OnEnable();
	        m_Rect = (RectTransform)transform;

	        // Force top-left anchoring so SetInsetAndSizeFromParentEdge
	        // behaves predictably with edge-based offsets.
	        m_Rect.anchorMin = new Vector2(0f, 1f);
	        m_Rect.anchorMax = new Vector2(0f, 1f);
	        m_Rect.pivot = new Vector2(0f, 1f);

	        ForceUpdate();
	    }

	    protected override void OnDisable()
	    {
	        // Clear driven properties so we don't keep overriding them
	        m_Tracker.Clear();
	        base.OnDisable();
	    }

	    protected override void OnRectTransformDimensionsChange()
	    {
	        base.OnRectTransformDimensionsChange();
	        ForceUpdate();
	    }

	    protected override void OnTransformParentChanged()
	    {
	        base.OnTransformParentChanged();
	        ForceUpdate();
	    }

	    private void ForceUpdate()
	    {
	        if (gameObject.activeInHierarchy)
	        {
	            LayoutRebuilder.MarkLayoutForRebuild(m_Rect);
	        }
	    }

	    protected override void OnUpdate()
	    {
	        // Mark the layout system to rebuild
	        ForceUpdate();
	    }

	    // --------------------------------------------------------------
	    // ILayoutElement Implementation
	    // --------------------------------------------------------------
	    public void CalculateLayoutInputHorizontal() { }
	    public void CalculateLayoutInputVertical() { }

	    public float minWidth => -1f;
	    public float preferredWidth => -1f;
	    public float flexibleWidth => -1f;
	    public float minHeight => -1f;
	    public float preferredHeight => -1f;
	    public float flexibleHeight => -1f;
	    public int layoutPriority => 1;

	    // --------------------------------------------------------------
	    // ILayoutSelfController Implementation
	    // --------------------------------------------------------------
	    public void SetLayoutHorizontal()
	    {
	        // Always clear first to avoid stacking
	        m_Tracker.Clear();

	        // Drive anchoredPosition and sizeDelta in the horizontal direction
	        m_Tracker.Add(this, m_Rect,
	            DrivenTransformProperties.AnchoredPosition |
	            DrivenTransformProperties.SizeDelta);

	        UpdateRectHoriz();
	    }

	    public void SetLayoutVertical()
	    {
	        // Clear again so that each layout pass is in a clean state
	        m_Tracker.Clear();

	        // Similarly, drive anchoredPosition and sizeDelta for vertical
	        // (If you prefer, you can drive anchorMin/anchorMax as well,
	        //  but typically you want them static if using edge insets.)
	        m_Tracker.Add(this, m_Rect,
	            DrivenTransformProperties.AnchoredPosition |
	            DrivenTransformProperties.SizeDelta);

	        UpdateRectVert();
	    }

	    // --------------------------------------------------------------
	    // Internal Helpers
	    // --------------------------------------------------------------
	    Vector2 GetParentSize()
	    {
	        RectTransform parent = transform.parent as RectTransform;
	        return parent == null ? Vector2.zero : parent.rect.size;
	    }

	    void UpdateRectHoriz()
	    {
	        if (IsNaNTransform(transform))
	            return;

	        Vector2 parentSize = GetParentSize();
	        float leftInset = GetHorizInsetVal(parentSize, _left);
	        float rightInset = GetHorizInsetVal(parentSize, _right);

	        float width = parentSize.x - leftInset - rightInset;
	        m_Rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, leftInset, width);
	    }

	    void UpdateRectVert()
	    {
	        if (IsNaNTransform(transform))
	            return;

	        Vector2 parentSize = GetParentSize();
	        float upInset = GetVertInsetVal(parentSize, _up);
	        float downInset = GetVertInsetVal(parentSize, _down);

	        float height = parentSize.y - upInset - downInset;
	        m_Rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, upInset, height);
	    }

	    float GetHorizInsetVal(Vector2 parentSize, OffsetKind offset)
	    {
	        switch (offset._strategy)
	        {
	            case OffsetValueStrategy.value_as_pcnt_ofOtherDimention:
	                // As a percentage of parent's height
	                return parentSize.y * offset._value;

	            case OffsetValueStrategy.value_as_pxl:
	                return offset._value;

	            case OffsetValueStrategy.value_as_pcnt_ofOwnDimention:
	                // As a percentage of parent's width
	                return parentSize.x * offset._value;

	            default:
	                return 0f;
	        }
	    }

	    float GetVertInsetVal(Vector2 parentSize, OffsetKind offset)
	    {
	        switch (offset._strategy)
	        {
	            case OffsetValueStrategy.value_as_pcnt_ofOtherDimention:
	                // As a percentage of parent's width
	                return parentSize.x * offset._value;

	            case OffsetValueStrategy.value_as_pxl:
	                return offset._value;

	            case OffsetValueStrategy.value_as_pcnt_ofOwnDimention:
	                // As a percentage of parent's height
	                return parentSize.y * offset._value;

	            default:
	                return 0f;
	        }
	    }

	    bool IsNaNTransform(Transform t)
	    {
	        if (float.IsNaN(t.position.x) ||
	            float.IsNaN(t.position.y) ||
	            float.IsNaN(t.position.z))
	        {
	            return true;
	        }

	        if (float.IsNaN(t.localScale.x) ||
	            float.IsNaN(t.localScale.y) ||
	            float.IsNaN(t.localScale.z))
	        {
	            return true;
	        }

	        return false;
	    }
	}
}//end namespace
