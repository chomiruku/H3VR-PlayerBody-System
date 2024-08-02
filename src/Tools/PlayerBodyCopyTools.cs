using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// Helper script to help with copying the player body system to another humanoid rig
    /// </summary>
    public class PlayerBodyCopyTools : MonoBehaviour
    {
        [Header("Component to copy the player body system to a new rig.")]
        public Animator OldPlayerBodyAnimator;
        public Animator NewPlayerBodyAnimator;

        public Transform HeadTarget;

        [Header("Hitboxes")]
        public Transform HeadHitbox;
        public Transform HipHitbox;
        public Transform SpineHitbox;
        public Transform ChestHitbox;

        [Header("AI Entities")]
        public Transform HeadAIEntity;
        public Transform CenterAIEntitiy;

        /// <summary>
        /// Move animation controller to new rig and set it up to always animate, amongst other settings.
        /// </summary>
        [ContextMenu("Setup new animator")]
        public void SetupNewAnimator()
        {
            if (NewPlayerBodyAnimator != null && OldPlayerBodyAnimator != null)
            {
                Vector3 pos = NewPlayerBodyAnimator.transform.position;
                NewPlayerBodyAnimator.runtimeAnimatorController = OldPlayerBodyAnimator.runtimeAnimatorController;
                NewPlayerBodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                NewPlayerBodyAnimator.applyRootMotion = false;
                NewPlayerBodyAnimator.transform.position = pos;
            }
            else if (OldPlayerBodyAnimator == null)
            {
                Debug.LogError("Could not setup new animator, missing OldPlayerBodyAnimator reference!");
            }
            else if (NewPlayerBodyAnimator == null)
            {
                Debug.LogError("Could not setup new animator, missing NewPlayerBodyAnimator reference!");
            }
        }
        /// <summary>
        /// Alignes the tracking head position with the rig head position so that movement is centered around the eyes, aka the headset.
        /// </summary>
        [ContextMenu("Adjust Head IK Position")]
        public void AdjustHeadIKPosition()
        {
            if (HeadTarget != null && NewPlayerBodyAnimator != null)
            {
                if (NewPlayerBodyAnimator.avatar != null && NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Transform head = NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Head);

                    HeadTarget.position = head.position;
                    HeadTarget.rotation = head.rotation;
                }
                else if (NewPlayerBodyAnimator.avatar == null)
                {
                    Debug.LogError("Could not move head, Animator is missing Avatar!");
                }
                else if (NewPlayerBodyAnimator.avatar != null && !NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Debug.LogError("Could not move head, Player body not imported as humanoid rig!");
                }
            }
            else if (HeadTarget == null)
            {
                Debug.LogError("Could not adjust head IK position, missing HeadTarget reference!");
            }
            else if (NewPlayerBodyAnimator == null) 
            {
                Debug.LogError("Could not adjust head IK position, missing NewPlayerBodyAnimator reference!");
            }
        }

        /// <summary>
        /// This system needs a lot of components in very specific places. this method makes sure they get there.
        /// </summary>
        [ContextMenu("Copy PlayerBody components to new rig")]
        public void CopyPlayerBodyComponents()
        {
            string oldName = NewPlayerBodyAnimator.gameObject.name;
            if (NewPlayerBodyAnimator != null && OldPlayerBodyAnimator != null)
            {
                GameObject newRig = NewPlayerBodyAnimator.gameObject;
                
                PlayerBodyHandController controller = NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyHandController>();
                if (controller != null)
                {
                    controller.PlayerBodyAnimator = NewPlayerBodyAnimator;
                }
                else
                {
                    Debug.LogError("Couldn't find PlayerBodyAnimationController component in NewPlayerBodyAnimator parent!");
                }
                PlayerBodyLegsController legsController = NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyLegsController>();
                if (legsController != null)
                {
                    legsController.PlayerBodyAnimator = NewPlayerBodyAnimator;
                }
                else
                {
                    Debug.LogError("Couldn't find PlayerBodyLegsController component in NewPlayerBodyAnimator parent!");
                }

                VRIK VRIKComponent = OldPlayerBodyAnimator.GetComponent<VRIK>();
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

                PlayerBodyFootPlacementData[] oldFootPlacementDataComponents = OldPlayerBodyAnimator.GetComponents<PlayerBodyFootPlacementData>();
                PlayerBodyFootPlacementData[] newFootPlacementDataComponents = NewPlayerBodyAnimator.GetComponents<PlayerBodyFootPlacementData>();
                if (oldFootPlacementDataComponents.Length == 2 && newFootPlacementDataComponents.Length == 0)
                {
                    newFootPlacementDataComponents = new PlayerBodyFootPlacementData[]{ newRig.AddComponent<PlayerBodyFootPlacementData>(),newRig.AddComponent<PlayerBodyFootPlacementData>()};
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

                PlayerBodyFootPlacer footPlacer = OldPlayerBodyAnimator.GetComponent<PlayerBodyFootPlacer>();
                if (footPlacer != null)
                {
                    CopyComponentToGameObject(footPlacer, newRig);
                }
                else
                {
                    Debug.LogError("PlayerBodyFootPlacer component missing on source rig!");
                }

                NewPlayerBodyAnimator.gameObject.name = oldName;
            }
            else
            {
                Debug.LogError("Could not copy components, missing Animator reference!");
            }
        }

        /// <summary>
        /// Finally copy all the hitboxes and stuff.
        /// </summary>
        [ContextMenu("Move colliders and hitboxes to new rig")]
        public void MoveCollidersContextMenu()
        {
            if (NewPlayerBodyAnimator != null)
            {
                if (NewPlayerBodyAnimator.avatar != null && NewPlayerBodyAnimator.avatar.isHuman) 
                {
                    MoveColliders();
                }
                else if (NewPlayerBodyAnimator.avatar == null) 
                {
                    Debug.LogError("Could not move colliders, Animator is missing Avatar!");
                }
                else if (NewPlayerBodyAnimator.avatar != null && !NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Debug.LogError("Could not move colliders, Player body not imported as humanoid rig!");
                }
            }
            else
            {
                Debug.LogError("Could not move colliders, missing NewPlayerBodyAnimator reference!");
            }
        }

        public void MoveColliders()
        {
            Transform head = NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Head);
            Transform hips = NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Hips);
            Transform spine = NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = NewPlayerBodyAnimator.GetBoneTransform(HumanBodyBones.Chest);

            if (head != null)
            {
                ReparentAndZero(HeadHitbox, head);
                ReparentAndZero(HeadAIEntity, head);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have head bone assigned!");
            }

            if (hips != null)
            {
                ReparentAndZero(HipHitbox, hips);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have hips bone assigned!");
            }

            if (spine != null)
            {
                ReparentAndZero(SpineHitbox, spine);
                ReparentAndZero(CenterAIEntitiy, spine);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have spine bone assigned!");
            }

            if (chest != null)
            {
                ReparentAndZero(ChestHitbox, chest);
            }
            else
            {
                Debug.LogError("Humanoid rig doesn't have chest bone assigned!");
            }
        }

        private void ReparentAndZero(Transform transform, Transform parent)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// More helper stuff.
        /// </summary>
        [ContextMenu("MirrorIKFromLeftToRight")]
        public void MirrorIKLeftRight()
        {
            PlayerBodyHandController controller = NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyHandController>();
            Transform root = controller.transform;

            for (int i = 0; i < controller.HandConfigs[0].HandIKTargets.Length; i++)
            {
                Transform original = controller.HandConfigs[0].HandIKTargets[i];
                Transform target = controller.HandConfigs[1].HandIKTargets[i];

                ReflectTransform(original, target, root);
            }
        }

        /// <summary>
        /// Even more helper stuff. just mirrored!
        /// </summary>
        [ContextMenu("MirrorIKFromRightToLeft")]
        public void MirrorIKRightLeft()
        {
            PlayerBodyHandController controller = NewPlayerBodyAnimator.GetComponentInParent<PlayerBodyHandController>();
            Transform root = controller.transform;

            for (int i = 0; i < controller.HandConfigs[1].HandIKTargets.Length; i++)
            {
                Transform original = controller.HandConfigs[1].HandIKTargets[i];
                Transform target = controller.HandConfigs[0].HandIKTargets[i];

                ReflectTransform(original, target, root);
            }
        }

        // Methods that handle copying of components which is surprisingly not trivial!
        public static T CopyComponentToGameObject<T>(T original, GameObject destination) where T : Component
        {
            destination.SetActive(false);
            Type type = original.GetType();
            Component copy = destination.GetComponent(type) ?? destination.AddComponent(type);
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            FieldInfo[] fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            destination.SetActive(true);
            return copy as T;
        }

        public static T CopyComponent<T>(Component target, T reference) where T : Component
        {
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

        private void ReflectTransform(Transform original, Transform toReflect, Transform center)
        {
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