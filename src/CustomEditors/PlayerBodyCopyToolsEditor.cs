using RootMotion.FinalIK;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// A custom editor to give the main script buttons for the context menu entries.
    /// Also contains the actual methods, so that the Undo system can be used.
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
                    Undo.SetCurrentGroupName("Setup New Animator");
                    int undoID = Undo.GetCurrentGroup();
                    SetupNewAnimator(t);
                    Undo.CollapseUndoOperations(undoID);
                }
                if (GUILayout.Button("Adjust Head IK Position"))
                {
                    Undo.SetCurrentGroupName("Adjust Head IK Position");
                    int undoID = Undo.GetCurrentGroup();
                    AdjustHeadIKPosition(t);
                    Undo.CollapseUndoOperations(undoID);
                }
                if (GUILayout.Button("Copy PlayerBody components to new rig"))
                {
                    Undo.SetCurrentGroupName("Copy Player Body Components");
                    int undoID = Undo.GetCurrentGroup();
                    CopyPlayerBodyComponents(t);
                    Undo.CollapseUndoOperations(undoID);
                }
                if (GUILayout.Button("Move colliders and hitboxes to new rig"))
                {
                    Undo.SetCurrentGroupName("Move Colliders");
                    int undoID = Undo.GetCurrentGroup();
                    MoveCollidersContextMenu(t);
                    Undo.CollapseUndoOperations(undoID);
                }
                if (GUILayout.Button("Mirror left hand IK targets to right hand"))
                {
                    Undo.SetCurrentGroupName("Mirror IK Left Right");
                    int undoID = Undo.GetCurrentGroup();
                    MirrorIKLeftRight(t);
                    Undo.CollapseUndoOperations(undoID);
                }
                if (GUILayout.Button("Mirror right hand IK targets to left hand"))
                {
                    Undo.SetCurrentGroupName("Mirror IK Right Left");
                    int undoID = Undo.GetCurrentGroup();
                    MirrorIKRightLeft(t);
                    Undo.CollapseUndoOperations(undoID);
                }
            }
        }

        /// <summary>
        /// Move animation controller to new rig and set it up to always animate, amongst other settings.
        /// </summary>
        public void SetupNewAnimator(PlayerBodyCopyTools t)
        {
            if (t.NewPlayerBodyAnimator != null && t.OldPlayerBodyAnimator != null)
            {
                Undo.RecordObject(t.NewPlayerBodyAnimator, "Setup new Animator");
                Vector3 pos = t.NewPlayerBodyAnimator.transform.position;
                t.NewPlayerBodyAnimator.runtimeAnimatorController = t.OldPlayerBodyAnimator.runtimeAnimatorController;
                t.NewPlayerBodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                t.NewPlayerBodyAnimator.applyRootMotion = false;
                t.NewPlayerBodyAnimator.transform.position = pos;
            }
            else if (t.OldPlayerBodyAnimator == null)
            {
                Debug.LogError("Could not setup new animator, missing OldPlayerBodyAnimator reference!");
            }
            else if (t.NewPlayerBodyAnimator == null)
            {
                Debug.LogError("Could not setup new animator, missing NewPlayerBodyAnimator reference!");
            }
        }

        /// <summary>
        /// Aligns the tracking head position with the rig head position so that movement is centered around the eyes, aka the headset.
        /// </summary>
        public void AdjustHeadIKPosition(PlayerBodyCopyTools t)
        {
            if (t.HeadTarget != null && t.NewPlayerBodyAnimator != null)
            {
                if (t.NewPlayerBodyAnimator.avatar != null && t.NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Transform head = t.NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Head);
                    Undo.RecordObject(t.HeadTarget, "Adjust Head IK Position");
                    t.HeadTarget.position = head.position;
                    t.HeadTarget.rotation = head.rotation;
                }
                else if (t.NewPlayerBodyAnimator.avatar == null)
                {
                    Debug.LogError("Could not move head, Animator is missing Avatar!");
                }
                else if (t.NewPlayerBodyAnimator.avatar != null && !t.NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Debug.LogError("Could not move head, Player body not imported as humanoid rig!");
                }
            }
            else if (t.HeadTarget == null)
            {
                Debug.LogError("Could not adjust head IK position, missing HeadTarget reference!");
            }
            else if (t.NewPlayerBodyAnimator == null)
            {
                Debug.LogError("Could not adjust head IK position, missing NewPlayerBodyAnimator reference!");
            }
        }

        /// <summary>
        /// This system needs a lot of components in very specific places. This method makes sure they get there.
        /// </summary>
        public void CopyPlayerBodyComponents(PlayerBodyCopyTools t)
        {
            string oldName = t.NewPlayerBodyAnimator.gameObject.name;
            if (t.NewPlayerBodyAnimator != null && t.OldPlayerBodyAnimator != null)
            {
                GameObject newRig = t.NewPlayerBodyAnimator.gameObject;
                PlayerBodyHandController controller = t.NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyHandController>();
                Undo.RecordObject(controller, "");
                if (controller != null)
                {
                    controller.PlayerBodyAnimator = t.NewPlayerBodyAnimator;
                }
                else
                {
                    Debug.LogError("Couldn't find PlayerBodyAnimationController component in NewPlayerBodyAnimator parent!");
                }
                PlayerBodyLegsController legsController = t.NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyLegsController>();
                Undo.RecordObject(legsController, "");
                if (legsController != null)
                {
                    legsController.PlayerBodyAnimator = t.NewPlayerBodyAnimator;
                }
                else
                {
                    Debug.LogError("Couldn't find PlayerBodyLegsController component in NewPlayerBodyAnimator parent!");
                }

                VRIK VRIKComponent = t.OldPlayerBodyAnimator.GetComponent<VRIK>();
                Undo.RecordObject(VRIKComponent, "");
                if (VRIKComponent != null)
                {
                    VRIKComponent = CopyComponentToGameObject(VRIKComponent, newRig);
                    VRIKComponent.AutoDetectReferences();

                    if (controller != null) controller.VRIKInstance = VRIKComponent;
                }
                else
                {
                    Debug.LogError("VRIK component missing on source rig!");
                }

                PlayerBodyFootPlacementData[] oldFootPlacementDataComponents = t.OldPlayerBodyAnimator.GetComponents<PlayerBodyFootPlacementData>();
                PlayerBodyFootPlacementData[] newFootPlacementDataComponents = t.NewPlayerBodyAnimator.GetComponents<PlayerBodyFootPlacementData>();
                Undo.RecordObjects(newFootPlacementDataComponents, "");
                if (oldFootPlacementDataComponents.Length == 2 && newFootPlacementDataComponents.Length == 0)
                {
                    newFootPlacementDataComponents = new PlayerBodyFootPlacementData[] { newRig.AddComponent<PlayerBodyFootPlacementData>(), newRig.AddComponent<PlayerBodyFootPlacementData>() };
                    for (int i = 0; i < oldFootPlacementDataComponents.Length; i++)
                    {
                        CopyComponent(newFootPlacementDataComponents[i], oldFootPlacementDataComponents[i]);
                    }
                }
                else if (oldFootPlacementDataComponents.Length == 2 && newFootPlacementDataComponents.Length == 2)
                {
                    for (int i = 0; i < oldFootPlacementDataComponents.Length; i++)
                    {
                        CopyComponent(newFootPlacementDataComponents[i], oldFootPlacementDataComponents[i]);
                    }
                }
                else if (oldFootPlacementDataComponents.Length < 2)
                {
                    Debug.LogError($"Not enough PlayerBodyFootPlacementData components on source rig! ({oldFootPlacementDataComponents.Length} < 2)");
                }
                else if (oldFootPlacementDataComponents.Length < 2)
                {
                    Debug.LogError($"Too many PlayerBodyFootPlacementData components on source rig! ({oldFootPlacementDataComponents.Length} > 2)");
                }

                PlayerBodyFootPlacer footPlacer = t.OldPlayerBodyAnimator.GetComponent<PlayerBodyFootPlacer>();
                Undo.RecordObject(footPlacer, "");
                if (footPlacer != null)
                {
                    CopyComponentToGameObject(footPlacer, newRig);
                }
                else
                {
                    Debug.LogError("PlayerBodyFootPlacer component missing on source rig!");
                }

                t.NewPlayerBodyAnimator.gameObject.name = oldName;
            }
            else
            {
                Debug.LogError("Could not copy components, missing Animator reference!");
            }
        }

        // Methods that handle copying of components which is surprisingly not trivial!

        /// <summary>
        /// Copy component to new gameobject, just like the editor context menu does.
        /// </summary>
        /// <typeparam name="T">Any type inheriting from 'Component'.</typeparam>
        /// <param name="original">Original component that should be copied.</param>
        /// <param name="destination">Target gameobject the component should be copied to.</param>
        /// <returns>Returns the component copy on the destination gameobject.</returns>
        public static T CopyComponentToGameObject<T>(T original, GameObject destination) where T : Component
        {
            destination.SetActive(false);
            Type type = original.GetType();
            Component copy = destination.GetComponent(type) ?? Undo.AddComponent(destination, type);
            Undo.RecordObject(copy, "Copy component to game object");
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            FieldInfo[] fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            destination.SetActive(true);
            return copy as T;
        }

        /// <summary>
        /// Copy component values to another component, just like the editor context menu does.
        /// </summary>
        /// <typeparam name="T">Any type inheriting from 'Component'.</typeparam>
        /// <param name="target">Target component that should have the values applied.</param>
        /// <param name="reference">Reference component whose values should be copied.</param>
        /// <returns>Returns the target that had the values applied.</returns>
        public static T CopyComponent<T>(Component target, T reference) where T : Component
        {
            Undo.RecordObject(target, "Copy component");
            Type type = reference.GetType();
            //if (type != reference.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(target, pinfo.GetValue(reference, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(target, finfo.GetValue(reference));
            }
            return target as T;
        }

        /// <summary>
        /// Move hitboxes to new player body
        /// </summary>
        public void MoveCollidersContextMenu(PlayerBodyCopyTools t)
        {
            if (t.NewPlayerBodyAnimator != null)
            {
                if (t.NewPlayerBodyAnimator.avatar != null && t.NewPlayerBodyAnimator.avatar.isHuman)
                {
                    MoveColliders(t);
                }
                else if (t.NewPlayerBodyAnimator.avatar == null)
                {
                    Debug.LogError("Could not move colliders, Animator is missing Avatar!");
                }
                else if (t.NewPlayerBodyAnimator.avatar != null && !t.NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Debug.LogError("Could not move colliders, Player body not imported as humanoid rig!");
                }
            }
            else
            {
                Debug.LogError("Could not move colliders, missing NewPlayerBodyAnimator reference!");
            }
        }

        private void MoveColliders(PlayerBodyCopyTools t)
        {
            Transform head = t.NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Head);
            Transform hips = t.NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = t.NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = t.NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Chest);

            if (head != null)
            {
                ReparentAndZero(t.HeadHitbox, head);
                ReparentAndZero(t.HeadAIEntity, head);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have head bone assigned!");
            }

            if (hips != null)
            {
                ReparentAndZero(t.HipHitbox, hips);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have hips bone assigned!");
            }

            if (spine != null)
            {
                ReparentAndZero(t.SpineHitbox, spine);
                ReparentAndZero(t.CenterAIEntitiy, spine);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have spine bone assigned!");
            }

            if (chest != null)
            {
                ReparentAndZero(t.ChestHitbox, chest);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have chest bone assigned!");
            }
        }

        private void ReparentAndZero(Transform transform, Transform parent)
        {
            Undo.RecordObject(transform, "Reparent and zero");
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Mirrors IK Targets from left hand to right hand
        /// </summary>
        [ContextMenu("MirrorIKFromLeftToRight")]
        public void MirrorIKLeftRight(PlayerBodyCopyTools t)
        {
            PlayerBodyHandController controller = t.NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyHandController>();
            Transform root = controller.transform;

            for (int i = 0; i < controller.HandConfigs[0].HandIKTargets.Length; i++)
            {
                Transform original = controller.HandConfigs[0].HandIKTargets[i];
                Transform target = controller.HandConfigs[1].HandIKTargets[i];

                ReflectTransform(original, target, root);
            }
        }

        /// <summary>
        /// Mirrors IK Targets from right hand to left hand
        /// </summary>
        [ContextMenu("MirrorIKFromRightToLeft")]
        public void MirrorIKRightLeft(PlayerBodyCopyTools t)
        {
            PlayerBodyHandController controller = t.NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyHandController>();
            Transform root = controller.transform;

            for (int i = 0; i < controller.HandConfigs[1].HandIKTargets.Length; i++)
            {
                Transform original = controller.HandConfigs[1].HandIKTargets[i];
                Transform target = controller.HandConfigs[0].HandIKTargets[i];

                ReflectTransform(original, target, root);
            }
        }

        private void ReflectTransform(Transform original, Transform toReflect, Transform center)
        {
            Undo.RecordObject(toReflect, "");
            toReflect.position = ReflectPosition(original.position, center);
            toReflect.rotation = ReflectRotation(original.rotation, center);
        }

        private Vector3 ReflectPosition(Vector3 position, Transform center)
        {
            return Vector3.Reflect(position - center.position, center.right) + center.position;
        }

        private Quaternion ReflectRotation(Quaternion source, Transform center)
        {
            return Quaternion.LookRotation(Vector3.Reflect(source * Vector3.forward, center.right), Vector3.Reflect(source * Vector3.up, center.right));
        }
    }
}
