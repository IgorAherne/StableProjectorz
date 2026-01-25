using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Reflection;

namespace spz {

	// Custom editor for the Scenes_Start_MGR component. Adds buttons
	// to the inspector for loading and managing scene hierarchies in the editor.
	[CustomEditor(typeof(Start_Scene_Global_MGR))]
	public class Scenes_Start_MGR_Editor : Editor
	{
	    public override void OnInspectorGUI(){
	        Start_Scene_Global_MGR managerScript = (Start_Scene_Global_MGR)target;

	        DrawDefaultInspector();
	        EditorGUILayout.Space(10);

	        if (GUILayout.Button("Load All Scenes in Editor (and Collapse)")){
	            managerScript.LoadAllScenes_EDITOR_ONLY();
	            SetAllScenesExpanded(false); // After loading, immediately collapse
	        }

	        EditorGUILayout.Space(5);
	        EditorGUILayout.LabelField("Hierarchy Utilities", EditorStyles.boldLabel);

	        EditorGUILayout.BeginHorizontal();
	        {
	            if (GUILayout.Button("Collapse All")){ SetAllScenesExpanded(false); }
	            if (GUILayout.Button("Expand All")){ SetAllScenesExpanded(true); }
	        }
	        EditorGUILayout.EndHorizontal();
	    }

	    // Correctly expands or collapses all scenes in the Hierarchy window.
	    void SetAllScenesExpanded(bool expand){
	        var hierarchyWindowType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
	        var hierarchyWindow = EditorWindow.GetWindow(hierarchyWindowType);
	        if (hierarchyWindow == null) return;

	        var setExpandedRecursiveMethod = hierarchyWindow.GetType().GetMethod("SetExpandedRecursive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int), typeof(bool) }, null);
	        if (setExpandedRecursiveMethod == null){
	            Debug.LogError("Could not find method 'UnityEditor.SceneHierarchyWindow.SetExpandedRecursive'.");
	            return;
	        }

	        var sceneHandleField = typeof(Scene).GetField("m_Handle", BindingFlags.NonPublic | BindingFlags.Instance);
	        if (sceneHandleField == null){
	            Debug.LogError("Could not find field 'int UnityEngine.SceneManagement.Scene.m_Handle'.");
	            return;
	        }

	        for (int i = 0; i < SceneManager.sceneCount; i++){
	            Scene scene = SceneManager.GetSceneAt(i);
	            var sceneHandle = sceneHandleField.GetValue(scene);
	            setExpandedRecursiveMethod.Invoke(hierarchyWindow, new[] { sceneHandle, expand });
	        }
	    }
	}
}//end namespace
