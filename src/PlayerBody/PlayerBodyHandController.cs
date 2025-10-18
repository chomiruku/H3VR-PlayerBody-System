// Original Script by AngryNoob
// Modification by Cityrobo
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using RootMotion.FinalIK;
using H3MP;
using H3MP.Scripts;
using System.Linq;
using OpenScripts2;

namespace PlayerBodySystem
{
    /// <summary>
    /// Controls hand animations and tracking
    /// </summary>
    public class PlayerBodyHandController : MonoBehaviour
    {
        //[Tooltip("This GameObject has the H3MP player body script on it.")]
        //public GameObject PlayerBodyRoot;
        [Header("This component controls how the hands move.")]
        [Header("Many fields have tooltips, just hover over them!")]
        [Header("(On the left side, where the name of the field is.)")]
        [Tooltip("H3MP Player Body reference for multiplayer support.")]
        public PlayerBody H3MPPlayerBody;
        [Tooltip("VRIK script for hand IK targeting based on held objects.")]
        public VRIK VRIKInstance;
        [Tooltip("Animator controlling the player body rig.")]
        public Animator PlayerBodyAnimator;
        [Tooltip("Hand configurations: Left Hand (Element 0), Right Hand (Element 1). Must have exactly 2 elements.")]
        public HandConfig[] HandConfigs;

        public bool InEditorDebuggingEnabled => OpenScripts2_BasePlugin.IsInEditor;

        [Header("Two-Handed Grip Settings")]
        [Tooltip("JerryAr's Method: Empty hand presses trigger to activate two-handed grip. When disabled, activates automatically when hands are close.")]
        public bool UseJerryArDoubleHandingMethod = false;
        [Tooltip("Distance (meters) to activate two-handed mode.")]
        [Range(0.05f, 0.3f)]
        public float TwoHandActivationDistance = 0.18f;
        [Tooltip("Distance (meters) to deactivate two-handed mode. Should be larger than activation distance.")]
        [Range(0.1f, 0.4f)]
        public float TwoHandDeactivationDistance = 0.28f;

        [Header("In-Editor Debugging Settings")]
        [Header("Left Hand Debugging")]
        [Tooltip("Index of grip mapping to test from GripMappings array (0 = first grip).")]
        public int LeftHandDebuggingGripIndex = 0;
        [Tooltip("Simulate trigger pull value (0.0 to 1.0) while holding weapon.")]
        [Range(0.0f, 1.0f)]
        public float LeftHandDebbuggingTriggerPull = 0.0f;
        [Header("Right Hand Debugging")]
        [Tooltip("Index of grip mapping to test from GripMappings array (0 = first grip).")]
        public int RightHandDebuggingGripIndex = 0;
        [Tooltip("Simulate trigger pull value (0.0 to 1.0) while holding weapon.")]
        [Range(0.0f, 1.0f)]
        public float RightHandDebbuggingTriggerPull = 0.0f;

        /// <summary>
        /// Maps a grip type identifier to its IK target and animator parameter
        /// </summary>
        [Serializable]
        public class GripMapping
        {
            [Tooltip("Unique identifier for this grip type (e.g., 'Pistol', 'Magazine', 'Foregrip')")]
            public string gripId;
            [Tooltip("IK target transform for this grip type")]
            public Transform ikTarget;
            [Tooltip("Animator bool parameter name for this grip type")]
            public string animatorParameter;
        }

        [Serializable]
        public class HandConfig
        {
            [Tooltip("Grip mappings for each grip type.")]
            public GripMapping[] GripMappings;
            [Tooltip("Animator parameter name for trigger press state when holding a gun.")]
            public string TriggerPressedBoolTransitionName;

            [HideInInspector]
            public Dictionary<string, GripMapping> gripMappingDict;
            //public AngryNoob_FingerTracking_Translated FingerTracking;

            [HideInInspector]
            public HandConfig OtherHandConfig;
            [HideInInspector]
            public bool TwoHandHolding = false;
            [HideInInspector]
            public bool JerryArToggleActive = false;
            [HideInInspector]
            public bool WasTriggerPressedLastFrame = false;
            [HideInInspector]
            public FVRViveHand Controller;
            [HideInInspector]
            public FVRInteractiveObject CurrentInteractable => Controller.CurrentInteractable;
            [HideInInspector]
            public FVRViveHand OtherHand => Controller.OtherHand;
            [HideInInspector]
            public IKSolverVR.Arm ConnectedIKArm;
            [HideInInspector]
            public Transform IKParent;
            [HideInInspector]
            public Vector3 OrigIKParentPos;
            [HideInInspector]
            public Quaternion OrigIKParentRot;
            [HideInInspector]
            public bool IsThisTheRightHand => Controller.IsThisTheRightHand;
            [HideInInspector]
            public bool IsLockedToHandguardPosition = false;
            [HideInInspector]
            public FVRInteractiveObject LockedForegripReference = null;
            [HideInInspector]
            public Transform LockedForegripTransform = null;
        }

        private readonly string[] FingerNames =
        {
            "Thumb",
            "Index",
            "Middle",
            "Ring",
            "Pinky"
        };

        /// <summary>
        /// Standard grip IDs - used for consistency across all player bodies
        /// </summary>
        private static class GripIds
        {
            public const string Empty = "Empty";
            public const string Gun = "Gun";
            public const string Magazine = "Magazine";
            public const string Handguard = "Handguard";
            public const string HandgunSlide = "HandgunSlide";
            public const string Bullet = "Bullet";
            public const string PinnedGrenade = "PinnedGrenade";
            public const string TopCover = "TopCover";
            public const string DoubleHand = "DoubleHand";
            public const string ClosedBoltHandle = "ClosedBoltHandle";
            public const string ClosedBolt = "ClosedBolt";
            public const string BoltActionHandle = "BoltActionHandle";
            public const string TubeFedShotgunHandle = "TubeFedShotgunHandle";
        }

        public void Awake()
        {
            if (HandConfigs.Length != 2) Debug.LogError(this + ": Either you have less than two hands, or more. If you have less, I'm sorry, if you have more, lucky you! In any case, this won't work with PlayerBodies. Sorry! (HandConfigs.Length != 2");
            else
            {
                // Subscribe to H3MP PlayerBodyInit event
                GameManager.OnPlayerBodyInit += OnPlayerBodyInit;
                for (int i = 0; i < HandConfigs.Length; i++)
                {
                    HandConfigs[i].ConnectedIKArm = i == 0 ? VRIKInstance.solver.leftArm : VRIKInstance.solver.rightArm;
                    HandConfigs[i].OtherHandConfig = HandConfigs[1 - i];

                    // Initialize grip mapping dictionary first (needed for getting Empty grip)
                    InitializeGripMappings(HandConfigs[i]);

                    // Get the Empty grip's IK target to determine IKParent
                    if (HandConfigs[i].gripMappingDict.TryGetValue(GripIds.Empty, out GripMapping emptyGrip) && emptyGrip.ikTarget != null)
                    {
                        HandConfigs[i].IKParent = emptyGrip.ikTarget.parent.parent.parent;
                        HandConfigs[i].OrigIKParentPos = HandConfigs[i].IKParent.localPosition;
                        HandConfigs[i].OrigIKParentRot = HandConfigs[i].IKParent.localRotation;
                    }
                    else
                    {
                        Debug.LogError($"Empty grip mapping not found or has null ikTarget for {(i == 0 ? "left" : "right")} hand!");
                    }
                }
            }
        }

        /// <summary>
        /// Initialize grip mappings from GripMappings array
        /// </summary>
        private void InitializeGripMappings(HandConfig config)
        {
            config.gripMappingDict = new Dictionary<string, GripMapping>();

            if (config.GripMappings != null && config.GripMappings.Length > 0)
            {
                foreach (var mapping in config.GripMappings)
                {
                    if (!string.IsNullOrEmpty(mapping.gripId))
                    {
                        config.gripMappingDict[mapping.gripId] = mapping;
                    }
                }
            }
            else
            {
                Debug.LogError($"GripMappings array is empty for {(config.IsThisTheRightHand ? "right" : "left")} hand! Please configure GripMappings in the inspector.");
            }
        }

        public void OnDestroy()
        {
            // Unsunbscribe from H3MP PlayerBodyInit event
            GameManager.OnPlayerBodyInit -= OnPlayerBodyInit;
        }

        public void Start()
        {
            if (!InEditorDebuggingEnabled)
            {
                FVRPlayerBody currentPlayerBody = GM.CurrentPlayerBody;
                //FVRMovementManager movementManager = GM.CurrentMovementManager;
                if (Mod.managerObject == null) // H3MP not connected
                {
                    for (int i = 0; i < HandConfigs.Length; i++)
                    {
                        HandConfig config = HandConfigs[i];
                        config.Controller = i == 0 ? currentPlayerBody.LeftHand.GetComponent<FVRViveHand>() : currentPlayerBody.RightHand.GetComponent<FVRViveHand>();
                        //config.Controller = movementManager.Hands[i];
                    }
                }
                else // H3MP connected, must check whether this body is ours
                {
                    if (GameManager.currentPlayerBody == H3MPPlayerBody) // Body is ours
                    {
                        for (int i = 0; i < HandConfigs.Length; i++)
                        {
                            HandConfig config = HandConfigs[i];
                            config.Controller = i == 0 ? currentPlayerBody.LeftHand.GetComponent<FVRViveHand>() : currentPlayerBody.RightHand.GetComponent<FVRViveHand>();
                            //config.Controller = movementManager.Hands[i];
                        }
                    }
                    else // Not ours, destroy this because it will never be used anyway and will just cause errors due to missing hands
                    {
                        Destroy(this);
                    }
                }
            }
        }

        /// <summary>
        /// A PlayerBody can go across scenes, meaning the current FVRPlayerBody can change
        /// In that case our hands will be destroyed along with the scene we are leaving
        /// Once the new FVRPlayerBody gets instantiated as part of the new scene we want to set our hands again
        /// </summary>
        public void OnPlayerBodyInit(FVRPlayerBody playerBody)
        {
            for (int i = 0; i < HandConfigs.Length; i++)
            {
                HandConfigs[i].Controller = i == 0 ? playerBody.LeftHand.GetComponent<FVRViveHand>() : playerBody.RightHand.GetComponent<FVRViveHand>();
            }
        }

        public void Update()
        {
            // Have to check if hands are set because it is now possible that they aren't. See comment on OnPlayerBodyInit            
            if (!InEditorDebuggingEnabled && HandConfigs.All(hc => hc.Controller != null))
            {
                foreach (var config in HandConfigs)
                {
                    //CheckIfItemInHand(config);

                    UpdateIKTargetAndAnimate(config);
                }
            }
            else if (InEditorDebuggingEnabled)
            {
                for (int i = 0; i < HandConfigs.Length; i++)
                {
                    DebuggingHandsAnimationControl(HandConfigs[i], i);
                }
            }
        }

        /// <summary>
        /// New finger tracking code using humanoid rig animation properties and blend trees
        /// </summary>
        private void UpdateFingerTracking(HandConfig config)
        {
            HandInput input = config.Controller.Input;
            float[] fingerCurls = 
            {
                input.FingerCurl_Thumb,
                input.FingerCurl_Index,
                input.FingerCurl_Middle,
                input.FingerCurl_Ring,
                input.FingerCurl_Pinky
            };
            string handedness = config.IsThisTheRightHand ? "Right " : "Left ";

            for (int i = 0; i < FingerNames.Length; i++)
            {
                PlayerBodyAnimator.SetFloat(handedness + FingerNames[i], fingerCurls[i]);
            }
        }

        /// <summary>
        /// Set correct IK Target for grabbed object type and update animation property
        /// </summary>
        private void UpdateIKTargetAndAnimate(HandConfig config)
        {
            string gripId = GetGrabbedObjectGripId(config);

            // Turn off all animator parameters first
            foreach (var mapping in config.gripMappingDict.Values)
            {
                if (!string.IsNullOrEmpty(mapping.animatorParameter))
                {
                    PlayerBodyAnimator.SetBool(mapping.animatorParameter, false);
                }
            }

            if (gripId != null && config.gripMappingDict.TryGetValue(gripId, out GripMapping currentGrip))
            {
                // Grabbing something or double handing
                if (!string.IsNullOrEmpty(currentGrip.animatorParameter))
                {
                    PlayerBodyAnimator.SetBool(currentGrip.animatorParameter, true);
                }
                config.ConnectedIKArm.target = currentGrip.ikTarget;

                if (gripId != GripIds.DoubleHand /*&& gripId != GripIds.Foregrip*/)
                {
                    // Use locked foregrip transform if handguard position is locked, otherwise use current interactable
                    Transform targetTransform;
                    if (config.IsLockedToHandguardPosition && config.LockedForegripTransform != null)
                    {
                        targetTransform = config.LockedForegripTransform;
                    }
                    else if (gripId == GripIds.TubeFedShotgunHandle)
                    {
                        // For TubeFedShotgunHandle, check if we're holding the gun through the foregrip
                        if (config.CurrentInteractable is FVRPhysicalObject shotgunPhysObj && shotgunPhysObj.IsAltHeld)
                        {
                            // Holding shotgun through foregrip - find the foregrip's transform
                            if (shotgunPhysObj.AltGrip != null)
                            {
                                targetTransform = shotgunPhysObj.AltGrip.transform;
                                Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using TubeFedShotgun AltGrip direct transform for IK: {targetTransform.name}");
                            }
                            else
                            {
                                // AltGrip is null, need to find the foregrip
                                FVRAlternateGrip[] foregrips = shotgunPhysObj.GetComponentsInChildren<FVRAlternateGrip>();
                                if (foregrips != null && foregrips.Length > 0)
                                {
                                    targetTransform = foregrips[0].transform;
                                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Found shotgun foregrip via GetComponentsInChildren, using direct transform: {targetTransform.name}");
                                }
                                else
                                {
                                    // No foregrip found, fall back to gun transform
                                    targetTransform = config.CurrentInteractable.transform;
                                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] No shotgun foregrip found, using gun direct transform: {targetTransform.name}");
                                }
                            }
                        }
                        else
                        {
                            // Directly holding the TubeFedShotgunHandle component itself
                            targetTransform = config.CurrentInteractable.transform;
                            Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using TubeFedShotgunHandle direct transform for IK: {targetTransform.name}");
                        }
                    }
                    else if (gripId == GripIds.Handguard && config.CurrentInteractable is FVRPhysicalObject physObj && physObj.IsAltHeld)
                    {
                        // Holding gun through foregrip - use the foregrip's transform
                        if (physObj.AltGrip != null)
                        {
                            targetTransform = physObj.AltGrip.PoseOverride ?? physObj.AltGrip.transform;
                            Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using AltGrip transform for IK: {targetTransform.name}");
                        }
                        else
                        {
                            // AltGrip is null, need to find the foregrip
                            // Try to find FVRAlternateGrip component on the gun or its children
                            FVRAlternateGrip[] foregrips = physObj.GetComponentsInChildren<FVRAlternateGrip>();
                            if (foregrips != null && foregrips.Length > 0)
                            {
                                // Use the first foregrip found (most guns only have one)
                                targetTransform = foregrips[0].PoseOverride ?? foregrips[0].transform;
                                Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Found foregrip via GetComponentsInChildren: {targetTransform.name}");
                            }
                            else
                            {
                                // No foregrip found, fall back to gun transform
                                targetTransform = config.CurrentInteractable.PoseOverride ?? config.CurrentInteractable.transform;
                                Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] No foregrip found, using gun transform: {targetTransform.name}");
                            }
                        }
                    }
                    else
                    {
                        targetTransform = config.CurrentInteractable.PoseOverride ?? config.CurrentInteractable.transform;
                    }

                    Vector3 offsetPos = config.OrigIKParentPos;
                    Quaternion offsetRot = config.OrigIKParentRot;

                    if (!config.IKParent.position.Approximately(targetTransform.TransformPoint(offsetPos)))
                        config.IKParent.position = targetTransform.TransformPoint(offsetPos);
                    if (!config.IKParent.rotation.Approximately(targetTransform.TransformRotation(offsetRot)))
                        config.IKParent.rotation = targetTransform.TransformRotation(offsetRot);
                }
                // Double Hand Grab
                else if (gripId == GripIds.DoubleHand)
                {
                    Transform targetTransform = config.OtherHand.CurrentInteractable.PoseOverride ?? config.OtherHand.CurrentInteractable.transform;
                    if (!config.IKParent.position.Approximately(targetTransform.TransformPoint(config.OrigIKParentPos)))
                        config.IKParent.position = targetTransform.TransformPoint(config.OrigIKParentPos);
                    if (!config.IKParent.rotation.Approximately(targetTransform.TransformRotation(config.OrigIKParentRot)))
                        config.IKParent.rotation = targetTransform.TransformRotation(config.OrigIKParentRot);
                }
            }
            else
            {
                // not grabbing something - use empty grip
                if (config.gripMappingDict.TryGetValue(GripIds.Empty, out GripMapping emptyGrip))
                {
                    config.ConnectedIKArm.target = emptyGrip.ikTarget;
                }
                UpdateFingerTracking(config);

                if (!config.IKParent.localPosition.Approximately(config.OrigIKParentPos)) config.IKParent.localPosition = config.OrigIKParentPos;
                if (!config.IKParent.localRotation.Approximately(config.OrigIKParentRot)) config.IKParent.localRotation = config.OrigIKParentRot;
            }

            // Handle trigger press animation (only for pistol grip)
            if (gripId == GripIds.Gun) PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, GetTriggerPullValue(config));
            else PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, 0.0f);
        }

        /// <summary>
        /// In Editor hand tester
        /// </summary>
        /// <param name="config">current hand config to test</param>
        /// <param name="handIndex">current grabbed item index to test</param>
        private void DebuggingHandsAnimationControl(HandConfig config, int handIndex)
        {
            int debuggingGripIndex = handIndex == 0 ? LeftHandDebuggingGripIndex : RightHandDebuggingGripIndex;

            // Turn off all animator parameters
            foreach (var mapping in config.gripMappingDict.Values)
            {
                if (!string.IsNullOrEmpty(mapping.animatorParameter))
                {
                    PlayerBodyAnimator.SetBool(mapping.animatorParameter, false);
                }
            }

            // Get the grip mapping at the specified index
            GripMapping debugGrip = null;
            if (config.GripMappings != null && debuggingGripIndex >= 0 && debuggingGripIndex < config.GripMappings.Length)
            {
                debugGrip = config.GripMappings[debuggingGripIndex];
            }

            // Set the current grip's animator parameter and IK target
            if (debugGrip != null)
            {
                if (!string.IsNullOrEmpty(debugGrip.animatorParameter))
                {
                    PlayerBodyAnimator.SetBool(debugGrip.animatorParameter, true);
                }
                if (debugGrip.ikTarget != null)
                {
                    config.ConnectedIKArm.target = debugGrip.ikTarget;
                }
            }

            // Handle trigger pull for pistol grip
            float triggerPull = handIndex == 0 ? LeftHandDebbuggingTriggerPull : RightHandDebbuggingTriggerPull;
            if (debugGrip != null && debugGrip.gripId == GripIds.Gun)
            {
                PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, triggerPull);
            }
            else
            {
                PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, 0.0f);
            }
        }

        /// <summary>
        /// Helper method to apply handguard locking logic for alternate grips
        /// </summary>
        private void TryLockToAlternateGrip(HandConfig config, FVRPhysicalObject parentObject)
        {
            if (parentObject != null)
            {
                // Check if other hand is holding the parent object
                bool otherHandHoldingParent = config.OtherHand.CurrentInteractable != null &&
                                              config.OtherHand.CurrentInteractable == parentObject;

                if (otherHandHoldingParent)
                {
                    // Lock this hand to grip position until user releases it
                    config.IsLockedToHandguardPosition = true;
                    config.LockedForegripReference = config.CurrentInteractable;

                    // For shotgun foregrips, use direct transform instead of PoseOverride
                    if (config.CurrentInteractable is TubeFedShotgunHandle ||
                        (config.CurrentInteractable is FVRAlternateGrip altGrip &&
                         altGrip.PrimaryObject != null &&
                         typeof(TubeFedShotgun).IsAssignableFrom(altGrip.PrimaryObject.GetType())))
                    {
                        config.LockedForegripTransform = config.CurrentInteractable.transform;
                        Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locking shotgun foregrip with direct transform");
                    }
                    else
                    {
                        // For regular handguards, use PoseOverride if available
                        config.LockedForegripTransform = config.CurrentInteractable.PoseOverride ?? config.CurrentInteractable.transform;
                    }
                }
            }
        }

        /// <summary>
        /// Determine the type of object grabbed by the hand
        /// Returns the grip ID string, or null if nothing is grabbed
        /// </summary>
        private string GetGrabbedObjectGripId(HandConfig config)
        {
            string gripId = null;

            // Check if we should unlock the handguard position lock
            if (config.IsLockedToHandguardPosition)
            {
                // Check if still holding the foregrip
                bool stillHoldingForegrip = config.CurrentInteractable != null &&
                                           config.CurrentInteractable == config.LockedForegripReference;

                // Check if the gun itself got transferred to this hand (happens when other hand releases)
                bool holdingForegripsParentGun = false;
                if (config.LockedForegripReference != null && config.LockedForegripReference is FVRAlternateGrip lockedGrip)
                {
                    holdingForegripsParentGun = config.CurrentInteractable != null &&
                                               config.CurrentInteractable == lockedGrip.PrimaryObject;
                }

                // Determine which grip ID to return based on the locked foregrip type or parent gun type
                string lockedGripId = GripIds.Handguard; // Default to handguard
                if (config.LockedForegripReference is TubeFedShotgunHandle)
                {
                    lockedGripId = GripIds.TubeFedShotgunHandle;
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked foregrip is TubeFedShotgunHandle");
                }
                else if (config.LockedForegripReference is FVRAlternateGrip lockedAltGrip &&
                         lockedAltGrip.PrimaryObject != null &&
                         typeof(TubeFedShotgun).IsAssignableFrom(lockedAltGrip.PrimaryObject.GetType()))
                {
                    lockedGripId = GripIds.TubeFedShotgunHandle;
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked foregrip parent is TubeFedShotgun, using TubeFedShotgunHandle pose");
                }

                if (stillHoldingForegrip)
                {
                    // Still holding the foregrip - maintain handguard position
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked to handguard, still holding foregrip");
                    return lockedGripId;
                }
                else if (holdingForegripsParentGun)
                {
                    // Gun transferred to this hand - still maintain handguard position
                    // Keep using handguard pose since we originally grabbed it via the foregrip
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked to handguard, gun transferred but maintaining handguard pose");
                    return lockedGripId;
                }
                else
                {
                    // User released - unlock
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Unlocking handguard position");
                    config.IsLockedToHandguardPosition = false;
                    config.LockedForegripReference = null;
                    config.LockedForegripTransform = null;
                }
            } 

            if (config.CurrentInteractable != null)
            {
                Type currentInteractableType = config.CurrentInteractable.GetType();
                Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] CurrentInteractable type: {currentInteractableType.Name}");

                // Grabbing gun
                if (typeof(FVRFireArm).IsAssignableFrom(currentInteractableType))
                {
                    // Check if this gun is being held through an alternate grip (foregrip)
                    FVRPhysicalObject physObj = config.CurrentInteractable as FVRPhysicalObject;
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Gun detected. AltGrip: {physObj?.AltGrip?.name}, IsAltHeld: {physObj?.IsAltHeld}");

                    if (physObj != null && physObj.IsAltHeld)
                    {
                        // Gun is being held by the foregrip
                        // Check if it's a TubeFedShotgun - use shotgun handle pose instead of generic handguard
                        if (typeof(TubeFedShotgun).IsAssignableFrom(currentInteractableType))
                        {
                            Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] TubeFedShotgun held through foregrip, using TubeFedShotgunHandle pose");
                            gripId = GripIds.TubeFedShotgunHandle;
                        }
                        else
                        {
                            Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Gun held through foregrip, using Handguard pose");
                            gripId = GripIds.Handguard;
                        }
                    }
                    else
                    {
                        // Gun is being held normally by the trigger grip
                        gripId = GripIds.Gun;
                    }
                }
                // Grabbing mag
                else if (typeof(FVRFireArmMagazine) == currentInteractableType) gripId = GripIds.Magazine;
                // Grabbing tube fed shotgun handle (MUST come before FVRAlternateGrip check since it's a subclass)
                else if (typeof(TubeFedShotgunHandle).IsAssignableFrom(currentInteractableType))
                {
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED TubeFedShotgunHandle!");
                    gripId = GripIds.TubeFedShotgunHandle;
                    TubeFedShotgunHandle shotgunHandle = config.CurrentInteractable as TubeFedShotgunHandle;
                    TryLockToAlternateGrip(config, shotgunHandle?.Shotgun);
                }
                // Grabbing handgun slide
                else if (typeof(HandgunSlide) == currentInteractableType) gripId = GripIds.HandgunSlide;
                // Grabbing round
                else if (typeof(FVRFireArmRound) == currentInteractableType) gripId = GripIds.Bullet;
                // Grabbing pinned grenade
                else if (typeof(PinnedGrenade) == currentInteractableType) gripId = GripIds.PinnedGrenade;
                // Grabbing top cover
                else if (typeof(FVRFireArmTopCover) == currentInteractableType) gripId = GripIds.TopCover;
                // Grabbing bolt handle
                else if (typeof(ClosedBoltHandle) == currentInteractableType) gripId = GripIds.ClosedBoltHandle;
                // Grabbing closed bolt
                else if (typeof(ClosedBolt) == currentInteractableType) gripId = GripIds.ClosedBolt;
                // Grabbing bolt action rifle handle
                else if (typeof(BoltActionRifle_Handle) == currentInteractableType) gripId = GripIds.BoltActionHandle;
                /*// Grabbing open bolt charging handle
                else if (typeof(OpenBoltChargingHandle) == currentInteractableType) grabbedObjectIndex = 12;
                // Grabbing open bolt receiver bolt
                else if (typeof(OpenBoltReceiverBolt) == currentInteractableType) grabbedObjectIndex = 13;
                // Grabbing open bolt rotating charging handle
                else if (typeof(OpenBoltRotatingChargingHandle) == currentInteractableType) grabbedObjectIndex = 14;
                // Grabbing revolver cylinder
                else if (typeof(RevolverCylinder) == currentInteractableType) grabbedObjectIndex = 15;
                // Grabbing revolver ejector
                else if (typeof(RevolverEjector) == currentInteractableType) grabbedObjectIndex = 16;
                // Grabbing shotgun foregrip
                else if (typeof(FVRShotgunForegrip) == currentInteractableType) grabbedObjectIndex = 17;
                */
                /*// Grabbing clip (not magazine)
                else if (typeof(FVRFireArmClip) == currentInteractableType) grabbedObjectIndex = 18;
                // Grabbing open bolt ripcord
                else if (typeof(OpenBoltRipcord) == currentInteractableType) grabbedObjectIndex = 19;
                // Grabbing open bolt dust cover
                else if (typeof(OpenBoltDustCover) == currentInteractableType) grabbedObjectIndex = 20;
                // Grabbing lever action tube action
                else if (typeof(LeverActionTubeACtion) == currentInteractableType) grabbedObjectIndex = 21;
                // Grabbing break action manual ejector
                else if (typeof(BreakActionManualEjector) == currentInteractableType) grabbedObjectIndex = 22;
                // Grabbing single action ejector rod
                else if (typeof(SingleActionEjectorRod) == currentInteractableType) grabbedObjectIndex = 23;
                // Grabbing handgun mag release trigger
                else if (typeof(HandgunMagReleaseTrigger) == currentInteractableType) grabbedObjectIndex = 24;
                // Grabbing firearm belt grab trigger
                else if (typeof(FVRFireArmBeltGrabTrigger) == currentInteractableType) grabbedObjectIndex = 25;
                // Grabbing attachable tube fed fore
                else if (typeof(AttachableTubeFedFore) == currentInteractableType) grabbedObjectIndex = 26;
                // Grabbing attachable tube fed bolt
                else if (typeof(AttachableTubeFedBolt) == currentInteractableType) grabbedObjectIndex = 27;
                // Grabbing flintlock pseudo ramrod
                else if (typeof(FlintlockPseudoRamRod) == currentInteractableType) grabbedObjectIndex = 28;
                // Grabbing G11 charging handle
                else if (typeof(G11ChargingHandle) == currentInteractableType) grabbedObjectIndex = 29;
                // Grabbing folding stock X axis
                else if (typeof(FVRFoldingStockXAxis) == currentInteractableType) grabbedObjectIndex = 30;
                // Grabbing folding stock Y axis
                else if (typeof(FVRFoldingStockYAxis) == currentInteractableType) grabbedObjectIndex = 31;
                // Grabbing chainsaw handle
                else if (typeof(ChainsawHandle) == currentInteractableType) grabbedObjectIndex = 32;
                // Grabbing LAPD2019 bolt handle
                else if (typeof(LAPD2019BoltHandle) == currentInteractableType) grabbedObjectIndex = 33;
                // Grabbing LAPD2019 cylinder
                else if (typeof(LAPD2019Cylinder) == currentInteractableType) grabbedObjectIndex = 34;
                // Grabbing LAPD2019 ejector
                else if (typeof(LAPD2019Ejector) == currentInteractableType) grabbedObjectIndex = 35;
                // Grabbing Mac11 stock
                else if (typeof(Mac11_Stock) == currentInteractableType) grabbedObjectIndex = 36;
                // Grabbing Mac11 stock butt
                else if (typeof(Mac11_StockButt) == currentInteractableType) grabbedObjectIndex = 37;
                // Grabbing shotgun moveable stock
                else if (typeof(ShotgunMoveableStock) == currentInteractableType) grabbedObjectIndex = 38;
                // Grabbing derringer barrel cycler
                else if (typeof(DerringerBarrelCycler) == currentInteractableType) grabbedObjectIndex = 39;
                // Grabbing flintlock flint holder
                else if (typeof(FlintlockFlintHolder) == currentInteractableType) grabbedObjectIndex = 40;
                // Grabbing flintlock flint screw
                else if (typeof(FlintlockFlintScrew) == currentInteractableType) grabbedObjectIndex = 41;
                // Grabbing flintlock powder horn cap
                else if (typeof(FlintlockPowderHornCap) == currentInteractableType) grabbedObjectIndex = 42;
                // Grabbing firearm grip
                else if (typeof(FVRFireArmGrip) == currentInteractableType) grabbedObjectIndex = 43;
                // Grabbing top cover advanced
                else if (typeof(FVRFireArmTopCoverAdvanced) == currentInteractableType) grabbedObjectIndex = 44;
                // Grabbing M203 fore
                else if (typeof(M203_Fore) == currentInteractableType) grabbedObjectIndex = 45;
                // Grabbing airgun barrel
                else if (typeof(AirgunBarrel) == currentInteractableType) grabbedObjectIndex = 46;
                // Grabbing capped grenade
                else if (typeof(FVRCappedGrenade) == currentInteractableType) grabbedObjectIndex = 47;*/
                // Grabbing foregrip
                else if (typeof(FVRAlternateGrip).IsAssignableFrom(currentInteractableType))
                {
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED FVRAlternateGrip!");
                    FVRAlternateGrip altGrip = config.CurrentInteractable as FVRAlternateGrip;

                    // Check if the parent gun is a TubeFedShotgun
                    if (altGrip?.PrimaryObject != null &&
                        typeof(TubeFedShotgun).IsAssignableFrom(altGrip.PrimaryObject.GetType()))
                    {
                        Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] FVRAlternateGrip parent is TubeFedShotgun, using TubeFedShotgunHandle pose");
                        gripId = GripIds.TubeFedShotgunHandle;
                    }
                    else
                    {
                        gripId = GripIds.Handguard;
                    }

                    TryLockToAlternateGrip(config, altGrip?.PrimaryObject);
                }
            }
            // Grabbing pistol with two hands
            // Don't activate double-hand mode if:
            // - Either hand is locked to handguard position
            // - The other hand is holding a gun through its foregrip (IsAltHeld)
            else if (DoubleHandMasturbating(config) == true &&
                     config.CurrentInteractable == null &&
                     config.OtherHand.CurrentInteractable != null &&
                     !config.IsLockedToHandguardPosition &&
                     !config.OtherHandConfig.IsLockedToHandguardPosition)
            {
                // Check if other hand is holding a gun through foregrip
                bool otherHandUsingForegrip = config.OtherHand.CurrentInteractable is FVRPhysicalObject otherPhysObj &&
                                              typeof(FVRFireArm).IsAssignableFrom(config.OtherHand.CurrentInteractable.GetType()) &&
                                              otherPhysObj.IsAltHeld;

                if (!otherHandUsingForegrip)
                {
                    gripId = GripIds.DoubleHand;
                }
            }
            return gripId;
        }

        /// <summary>
        /// Get trigger pull value with ramp applied
        /// Uses a ramp to avoid false activation from capacitive touch
        /// Returns 0.0 until threshold is met, then ramps from 0.0 to 1.0
        /// </summary>
        private float GetTriggerPullValue(HandConfig config)
        {
            float rawTriggerValue = config.Controller.Input.TriggerFloat;
            const float activationThreshold = 0.7f;  // Threshold before ramping starts
            const float fullPullValue = 0.95f;       // Value considered full pull

            // Return 0 if below activation threshold
            if (rawTriggerValue < activationThreshold)
                return 0.0f;

            // Remap from [activationThreshold, fullPullValue] to [0.0, 1.0]
            float remapped = (rawTriggerValue - activationThreshold) / (fullPullValue - activationThreshold);
            return Mathf.Clamp01(remapped);
        }

        /// <summary>
        /// Determine whether the trigger is pressed (used for JerryAr double-handing)
        /// </summary>
        private bool CheckTriggerPressed(HandConfig config) => config.Controller.Input.TriggerFloat >= 0.7f;

        /// <summary>
        /// Check for two-handed gun control
        /// (Not my name)
        /// </summary>
        private bool DoubleHandMasturbating(HandConfig config)
        {
            float distance = DistanceBetweenBothHands();
            bool currentTriggerPressed = CheckTriggerPressed(config);

            if (UseJerryArDoubleHandingMethod)
            {
                // JerryAr's Method: Toggle two-handed mode with trigger press on empty hand

                // If hands move too far apart, reset everything
                if (distance > TwoHandDeactivationDistance)
                {
                    config.TwoHandHolding = false;
                    config.JerryArToggleActive = false;
                    config.WasTriggerPressedLastFrame = false;
                }
                // If hands are close enough
                else if (distance <= TwoHandActivationDistance)
                {
                    // Detect trigger press (rising edge: was not pressed last frame, is pressed now)
                    if (currentTriggerPressed && !config.WasTriggerPressedLastFrame)
                    {
                        // Toggle the JerryAr mode
                        config.JerryArToggleActive = !config.JerryArToggleActive;
                        config.TwoHandHolding = config.JerryArToggleActive;
                    }
                }

                // Update the trigger state for next frame
                config.WasTriggerPressedLastFrame = currentTriggerPressed;
            }
            else
            {
                // Automatic Method: Activate when hands are close together
                if (distance <= TwoHandActivationDistance)
                {
                    config.TwoHandHolding = true;
                }
                if (distance > TwoHandDeactivationDistance)
                {
                    config.TwoHandHolding = false;
                }
            }

            return config.TwoHandHolding;
        }

        /// <summary>
        /// Calculate distance between both hands
        /// </summary>
        /// <returns>Distance between hands as float</returns>
        private float DistanceBetweenBothHands() => Vector3.Distance(GM.CurrentMovementManager.LeftHand.position, GM.CurrentMovementManager.RightHand.position);
    }
}