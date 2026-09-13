using SDG.Unturned;
using UnityEditor;

[CustomEditor(typeof(CommentComponent))]
public class CommentEditor : UnityEditor.Editor
{
	public override void OnInspectorGUI()
	{
		// DrawDefaultInspector shows the script name and default serialized
		// text field, so we do not call it in order to save space.

		CommentComponent commentComponent = target as CommentComponent;
		if (commentComponent == null)
			return;

		EditorGUI.BeginChangeCheck();
		string message = EditorGUILayout.TextArea(commentComponent.message);
		bool changed = EditorGUI.EndChangeCheck();
		if (changed)
		{
			Undo.RecordObject(commentComponent, "Changed comment");
			commentComponent.message = message;
			PrefabUtility.RecordPrefabInstancePropertyModifications(commentComponent);
		}
	}
}
