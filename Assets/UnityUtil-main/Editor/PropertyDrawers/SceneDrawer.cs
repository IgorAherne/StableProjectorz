using UnityEditor;
using UnityEngine;

namespace Voxell.Inspector
{
  [CustomPropertyDrawer(typeof(SceneAttribute))]
  public class SceneDrawer : PropertyDrawer
  {
    public override void OnGUI (Rect rect, SerializedProperty property, GUIContent label)
    {
      if (property.propertyType == SerializedPropertyType.String)
      {
        SceneAsset sceneObject = GetSceneObject(property.stringValue);
        SceneAsset scene = EditorGUI.ObjectField(rect, label, sceneObject, typeof(SceneAsset), true) as SceneAsset;
        if (scene == null)
        {
          property.stringValue = "";
        } else if (scene.name != property.stringValue)
        {
          SceneAsset sceneObj = GetSceneObject(scene.name);
          if (sceneObj == null)
            Debug.LogWarning($"The scene {scene.name} cannot be used. To use this scene add it to the build settings for the project");
          else property.stringValue = scene.name;
        }
      }
      else EditorGUI.LabelField(rect, label.text, "Use [Scene] with strings.");
    }

    private SceneAsset GetSceneObject(string sceneObjectName)
    {
      if (string.IsNullOrEmpty(sceneObjectName)) return null;

      foreach (EditorBuildSettingsScene editorScene in EditorBuildSettings.scenes)
      {
        if (editorScene.path.IndexOf(sceneObjectName) != -1)
          return AssetDatabase.LoadAssetAtPath<SceneAsset>(editorScene.path);
      }
      Debug.LogWarning($"Scene [{sceneObjectName}] cannot be used. Add this scene to the 'Scenes in the Build' in build settings.");
      return null;
    }
  }
}