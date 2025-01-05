using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// A custom editor to give the main script buttons for the context menu entries.
    /// </summary>
    [CustomEditor(typeof(MirrorRotationOnXAxis))]
    public class MirrorRotationOnXAxisEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MirrorRotationOnXAxis t = (MirrorRotationOnXAxis)target;

            if (t.ToMirror == null)
            {
                EditorGUILayout.HelpBox("Please assign thing to mirror!", MessageType.Warning);
            }
            else if (t.MirrorRoot == null)
            {
                EditorGUILayout.HelpBox("Please assign thing to mirror around!", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Mirror Position and Rotation"))
                {
                    Undo.RecordObject(t.transform, "Mirrored position and rotation from other transform.");
                    t.Mirror();
                }
            }
        }
    }
}
