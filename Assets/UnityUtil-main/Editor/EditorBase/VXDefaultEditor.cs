using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Voxell.Inspector
{
  [CanEditMultipleObjects]
  [CustomEditor(typeof(UnityEngine.Object), true)]
  public class VXDefaultEditor : Editor
  {
    private IEnumerable<MethodInfo> _methods;

    protected virtual void OnEnable()
    {
      _methods = VXEditorUtil.GetAllMethods(
        target, m => m.GetCustomAttributes(typeof(ButtonAttribute), true).Length > 0);
    }

    public override void OnInspectorGUI()
    {
      base.DrawDefaultInspector();
      DrawButtons();
    }

    protected void DrawButtons()
    {
      if (_methods.Any())
      {
        EditorGUILayout.Space();

        foreach (MethodInfo method in _methods)
          MethodButton(serializedObject.targetObject, method);
      }
    }

    public static void MethodButton(UnityEngine.Object target, MethodInfo methodInfo)
    {
      ButtonAttribute buttonAttribute = (ButtonAttribute)methodInfo.GetCustomAttributes(typeof(ButtonAttribute), true)[0];
      string buttonName = string.IsNullOrEmpty(buttonAttribute.buttonName) ? ObjectNames.NicifyVariableName(methodInfo.Name) : buttonAttribute.buttonName;

      if (GUILayout.Button(buttonName))
      {
        object[] defaultParams = methodInfo.GetParameters().Select(p => p.DefaultValue).ToArray();
        IEnumerator methodResult = methodInfo.Invoke(target, defaultParams) as IEnumerator;
        if (!Application.isPlaying)
        {
          // Set target object and scene dirty to serialize changes to disk
          EditorUtility.SetDirty(target);

          PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
          // Prefab mode
          if (stage != null) EditorSceneManager.MarkSceneDirty(stage.scene);
          // Normal scene
          else EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        } else if (methodResult != null && target is MonoBehaviour behaviour)
          behaviour.StartCoroutine(methodResult);
      }
    }
  }
}
