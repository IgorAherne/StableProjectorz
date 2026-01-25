using UnityEngine;
using UnityEditor;

namespace Voxell.Inspector
{
  public abstract class AbstractVXEditor : Editor
  {
    public abstract void OnEnable();

    public virtual void OnRender() => base.OnInspectorGUI();

    public override void OnInspectorGUI()
    {
      serializedObject.Update();
      EditorGUI.BeginChangeCheck();

      OnRender();

      if (EditorGUI.EndChangeCheck()) OnChange();
      serializedObject.ApplyModifiedProperties();
    }

    public virtual void OnChange() {}
  }
}