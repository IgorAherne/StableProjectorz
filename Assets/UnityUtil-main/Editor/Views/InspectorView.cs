using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Voxell.Inspector
{
  public class InspectorView : VisualElement
  {
    public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits> {}
    private Editor _editor;

    public Editor InitializeInspector(Object obj) => InitializeInspector(obj, null);
    public Editor InitializeInspector(Object obj, System.Type editorType)
    {
      ClearEditor();

      _editor = CreateEditor(obj, editorType);
      IMGUIContainer container = new IMGUIContainer(_editor.OnInspectorGUI);
      ScrollView scrollView = new ScrollView();
      scrollView.Add(container);
      Add(scrollView);

      return _editor;
    }

    public Editor InitializeInspector(Object[] objs) => InitializeInspector(objs, null);
    public Editor InitializeInspector(Object[] objs, System.Type editorType)
    {
      ClearEditor();

      _editor = CreateEditor(objs, editorType);
      IMGUIContainer container = new IMGUIContainer(_editor.OnInspectorGUI);
      ScrollView scrollView = new ScrollView();
      scrollView.Add(container);
      Add(scrollView);

      return _editor;
    }

    public void ClearEditor()
    {
      Clear();
      Object.DestroyImmediate(_editor);
    }

    private static Editor CreateEditor(Object[] objs, System.Type editorType)
    {
      if (editorType == null) return Editor.CreateEditor(objs);
      return Editor.CreateEditor(objs, editorType);
    }

    private static Editor CreateEditor(Object obj, System.Type editorType)
    {
      if (editorType == null) return Editor.CreateEditor(obj);
      return Editor.CreateEditor(obj, editorType);
    }
  }
}