using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Voxell.Inspector.List
{
  public static class EditorListUtil
  {
    public static ReorderableList CreateReorderableList(
      SerializedObject serializedObject, SerializedProperty property, EditorListConfig listConfig
    )
    {
      ReorderableList list = new ReorderableList(
        serializedObject, property,
        listConfig.draggable, listConfig.displayHeader,
        listConfig.displayAddButton, listConfig.displayRemoveButton
      );

      InitializeReorderableList(list, listConfig);

      return list;
    }

    public static void InitializeReorderableList(ReorderableList list, EditorListConfig listConfig)
    {
      list.multiSelect = listConfig.multiSelect;
      SetupDrawHeaderCallback(list, listConfig);
      SetupDrawElementCallback(list, listConfig);
      SetupElementHeightCallback(list);
      SetupDrawNoneCallback(list, listConfig);
      SetupDrawFooterCallback(list);
    }

    public static void SetupDrawHeaderCallback(ReorderableList list, EditorListConfig listConfig)
    {
      string header = string.IsNullOrEmpty(listConfig.header)
        ? list.serializedProperty.displayName : listConfig.header;

      list.drawHeaderCallback = (Rect rect) =>
      {
        SerializedProperty property = list.serializedProperty;
        rect.height += 3.0f;
        EditorGUI.indentLevel += 1;

        property.isExpanded = EditorGUI.Foldout(
          rect, property.isExpanded, header, true, VXEditorStyles.ReordableFoldoutStyle
        );
        list.draggable = property.isExpanded && listConfig.draggable;
        list.displayAdd = property.isExpanded && listConfig.displayAddButton;
        list.displayRemove = property.isExpanded && listConfig.displayRemoveButton;

        EditorGUI.indentLevel -= 1;
      };
    }

    public static void SetupDrawElementCallback(ReorderableList list, EditorListConfig listConfig)
    {
      bool showPrefix = !string.IsNullOrEmpty(listConfig.prefix);

      list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
      {
        SerializedProperty property = list.serializedProperty;
        if (!property.isExpanded) return;

        SerializedProperty elemProperty = property.GetArrayElementAtIndex(index);
        bool isGeneric = elemProperty.propertyType == SerializedPropertyType.Generic;
        if (isGeneric)
        {
          rect.x += 10.0f;
          rect.width -= 10.0f;
        }

        EditorGUI.PropertyField(
          rect, elemProperty,
          new GUIContent(showPrefix ? $"{listConfig.prefix}{index}" : ""), true
        );
      };
    }

    public static void SetupElementHeightCallback(ReorderableList list)
    {
      list.elementHeightCallback = (int indexer) =>
      {
        SerializedProperty property = list.serializedProperty;
        if (!property.isExpanded) return 0.0f;
        else
        {
          if (indexer < property.arraySize)
            return EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(indexer));

          return 0.0f;
        }
      };
    }

    public static void SetupDrawNoneCallback(ReorderableList list, EditorListConfig listConfig)
    {
      string emptyMsg = !string.IsNullOrEmpty(listConfig.emptyMsg) ? listConfig.emptyMsg : "List is Empty";

      list.drawNoneElementCallback = (Rect rect) =>
      {
        SerializedProperty property = list.serializedProperty;
        if (property.isExpanded)
        {
          list.elementHeight = 22.0f;
          EditorGUI.LabelField(rect, emptyMsg);
        } else list.elementHeight = 0.0f;
      };
    }

    public static void SetupDrawFooterCallback(ReorderableList list)
    {
      list.drawFooterCallback = (Rect rect) =>
      {
        SerializedProperty property = list.serializedProperty;
        if (property.isExpanded)
        {
          list.footerHeight = 22.0f;
          ReorderableList.defaultBehaviours.DrawFooter(rect, list);
        } else list.footerHeight = 0.0f;
      };
    }
  }
}