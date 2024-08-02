using UnityEditor;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// A custom editor to give the main script buttons for the context menu entries.
    /// </summary>
    [CustomEditor(typeof(PlayerBodyCopyTools))]
    public class PlayerBodyCopyToolsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            PlayerBodyCopyTools t = (PlayerBodyCopyTools)target;

            if (t.NewPlayerBodyAnimator == null)
            {
                EditorGUILayout.HelpBox("Please assign animator on new player body rig!", MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Setup new animator"))
                {
                    t.SetupNewAnimator();
                }
                if (GUILayout.Button("Adjust Head IK Position"))
                {
                    t.AdjustHeadIKPosition();
                }
                if (GUILayout.Button("Copy PlayerBody components to new rig"))
                {
                    t.CopyPlayerBodyComponents();
                }
                if (GUILayout.Button("Move colliders and hitboxes to new rig"))
                {
                    t.MoveCollidersContextMenu();
                }
            }
        }
    }
}
