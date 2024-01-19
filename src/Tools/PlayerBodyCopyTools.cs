using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlayerBodySystem
{
    public class PlayerBodyCopyTools : MonoBehaviour
    {
        [Header("Component to copy the player body system to a new rig.")]
        [Header("Use context menu (cogwheel in the top right corner of the component).")]
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

        [ContextMenu("Setup new animator")]
        public void SetupNewAnimator()
        {
            if (NewPlayerBodyAnimator != null && OldPlayerBodyAnimator != null)
            {
                NewPlayerBodyAnimator.runtimeAnimatorController = OldPlayerBodyAnimator.runtimeAnimatorController;
                NewPlayerBodyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                NewPlayerBodyAnimator.applyRootMotion = false;
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
                    Debug.LogError("Could not move colliders, Animator is missing Avatar!");
                }
                else if (NewPlayerBodyAnimator.avatar != null && !NewPlayerBodyAnimator.avatar.isHuman)
                {
                    Debug.LogError("Could not move colliders, Player body not imported as humanoid rig!");
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
    }
}