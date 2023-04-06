using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProjectParser))]
public class ProjectParserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProjectParser settings = (ProjectParser)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Select Root Directory"))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Root Directory", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                settings.rootProjectDirectory = selectedPath;
                EditorUtility.SetDirty(settings);
            }
        }
    }
}
