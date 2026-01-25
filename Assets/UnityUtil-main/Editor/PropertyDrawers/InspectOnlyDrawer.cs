using UnityEditor;
using UnityEngine;

namespace Voxell.Inspector
{
  [CustomPropertyDrawer(typeof(InspectOnlyAttribute))]
  public class InspectOnlyDrawer : PropertyDrawer
  {
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
      bool guiEnabled = GUI.enabled;
      GUI.enabled = false;
      EditorGUI.PropertyField(rect, property, label, true);
      GUI.enabled = guiEnabled;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      => EditorGUI.GetPropertyHeight(property, true);
  }
}