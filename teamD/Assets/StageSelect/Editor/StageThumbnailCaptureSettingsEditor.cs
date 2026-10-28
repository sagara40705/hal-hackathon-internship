using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StageThumbnailCaptureSettings))]
public class StageThumbnailCaptureSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var settings = (StageThumbnailCaptureSettings)target;
        string error = StageThumbnailCapture.ValidateSettings(settings);
        if (error != null)
        {
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        }

        using (new EditorGUI.DisabledGroupScope(error != null || EditorApplication.isCompiling ||
                   EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("登録したシーンの写真を一括撮影", GUILayout.Height(32)))
            {
                try
                {
                    StageThumbnailCapture.CaptureAll(settings);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, settings);
                }
            }
        }
    }
}
