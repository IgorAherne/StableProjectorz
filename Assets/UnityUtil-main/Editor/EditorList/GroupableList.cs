using System;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

namespace Voxell.Inspector.List
{
  public sealed class GroupableList : ReorderableList
  {
    public readonly int GroupIdx;

    public GroupableList(
      SerializedObject serializedObject, SerializedProperty elements, int groupIdx
    ) : base(serializedObject, elements) => GroupIdx = groupIdx;

    public GroupableList(
      IList elements, Type elementType, int groupIdx
    ) : base(elements, elementType) => GroupIdx = groupIdx;

    public GroupableList(
      SerializedObject serializedObject, SerializedProperty elements, int groupIdx,
      bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton
    ) : base (
      serializedObject, elements, draggable, displayHeader, displayAddButton, displayRemoveButton
    ) => GroupIdx = groupIdx;

    public GroupableList(
      IList elements, Type elementType, int groupIdx,
      bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton
    ) : base (
      elements, elementType, draggable, displayHeader, displayAddButton, displayRemoveButton
    ) => GroupIdx = groupIdx;

    public static GroupableList Create(
      SerializedObject serializedObject, SerializedProperty property, int groupIdx, EditorListConfig listConfig
    )
    {
      GroupableList groupableList = new GroupableList(
        serializedObject, property, groupIdx,
        listConfig.draggable, listConfig.displayHeader,
        listConfig.displayAddButton, listConfig.displayRemoveButton
      );

      EditorListUtil.InitializeReorderableList(groupableList, listConfig);

      return groupableList;
    }
  }
}