using UnityEngine;
using UnityEditor;

namespace spz {

	[CustomEditor(typeof(SP_Comment))]
	public class SP_CommentEditor : Editor{
	    public override void OnInspectorGUI(){
	        SP_Comment comment = (SP_Comment)target;

	        EditorGUILayout.LabelField("Comment", EditorStyles.boldLabel);
        
	        // Make the text field yellow to stand out
	        GUI.backgroundColor = new Color(1f, 1f, 0.8f);
        
	        comment.comment = EditorGUILayout.TextArea(comment.comment, 
	            GUILayout.MinHeight(150));
        
	        // Reset color
	        GUI.backgroundColor = Color.white;

	        if (GUI.changed){
	            EditorUtility.SetDirty(target);
	        }
	    }
	}
}//end namespace
