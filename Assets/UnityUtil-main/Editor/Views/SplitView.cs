using UnityEngine.UIElements;

namespace Voxell.Inspector
{
  public class SplitView : TwoPaneSplitView
  {
    public new class UxmlFactory : UxmlFactory<SplitView, TwoPaneSplitView.UxmlTraits> {}
  }
}
