using UnityEditor;

namespace Voxell.Inspector.List
{
  public struct EditorListConfig
  {
    public static readonly EditorListConfig Default = new EditorListConfig
    {
      draggable = true,
      displayHeader = true,
      displayAddButton = true,
      displayRemoveButton = true,
      multiSelect = true,

      prefix = "",
      header = "",
      emptyMsg = "",
    };

    public bool draggable;
    public bool displayHeader;
    public bool displayAddButton;
    public bool displayRemoveButton;
    public bool multiSelect;

    public string prefix;
    public string header;
    public string emptyMsg;
  }
}