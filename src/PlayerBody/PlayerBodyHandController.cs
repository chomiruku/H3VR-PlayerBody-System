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
        //[Header("Make sure to turn off before building your playerbody for in game use!)")]
        [Header("Left Hand Debugging")]
        [Tooltip("Simulate held item state for testing (0-48). See: https://github.com/chomiruku/H3VR-PlayerBody-System/wiki/Hand-Poses")]
        [Range(0,48)]
        public int LeftHandDebuggingInteractableIndex = 0;
        [Tooltip("Simulate trigger press while holding weapon.")]
        public bool LeftHandDebbuggingTriggerPressed = false;
        [Header("Right Hand Debugging")]
        [Tooltip("Simulate held item state for testing (0-48). See: https://github.com/chomiruku/H3VR-PlayerBody-System/wiki/Hand-Poses")]
        [Range(0, 48)]
        public int RightHandDebuggingInteractableIndex = 0;
        [Tooltip("Simulate trigger press while holding weapon.")]
        public bool RightHandDebbuggingTriggerPressed = false;

        [Serializable]
        public class HandConfig
        {
            [Tooltip("Hand IK targets for different grip types (49 elements: 0=empty, 1-48=various grips). See: https://github.com/chomiruku/H3VR-PlayerBody-System/wiki/Hand-Poses")]
            public Transform[] HandIKTargets;
            [Tooltip("Animator bool names for hand poses (48 elements for indices 1-48). See: https://github.com/chomiruku/H3VR-PlayerBody-System/wiki/Hand-Poses")]
            public string[] AnimatorBoolTransitionNames;
            [Tooltip("Animator parameter name for trigger press state when holding a gun.")]
            public string TriggerPressedBoolTransitionName;
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
                    HandConfigs[i].IKParent = HandConfigs[i].HandIKTargets[0].parent.parent.parent;
                    HandConfigs[i].OrigIKParentPos = HandConfigs[i].IKParent.localPosition;
                    HandConfigs[i].OrigIKParentRot = HandConfigs[i].IKParent.localRotation;
                }
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
            int grabbedObjectIndex = GetGrabbedObjectIndex(config);
            for (int i = 0; i < config.AnimatorBoolTransitionNames.Length; i++)
            {
                string parameterName = config.AnimatorBoolTransitionNames[i];
                if (i != grabbedObjectIndex) PlayerBodyAnimator.SetBool(parameterName, false);
            }
            if (grabbedObjectIndex != -1)
            {
                // Grabbing something or double handing
                PlayerBodyAnimator.SetBool(config.AnimatorBoolTransitionNames[grabbedObjectIndex], true);
                config.ConnectedIKArm.target = config.HandIKTargets[grabbedObjectIndex + 1];

                if (grabbedObjectIndex != 7 /*&& grabbedObjectIndex != 2*/)
                {
                    // Use locked foregrip transform if handguard position is locked, otherwise use current interactable
                    Transform targetTransform;
                    if (config.IsLockedToHandguardPosition && config.LockedForegripTransform != null)
                    {
                        targetTransform = config.LockedForegripTransform;
                    }
                    else if (grabbedObjectIndex == 2 && config.CurrentInteractable is FVRPhysicalObject physObj && physObj.IsAltHeld)
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
                else if (grabbedObjectIndex == 7)
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
                // not grabbing something
                config.ConnectedIKArm.target = config.HandIKTargets[0];
                UpdateFingerTracking(config);

                if (!config.IKParent.localPosition.Approximately(config.OrigIKParentPos)) config.IKParent.localPosition = config.OrigIKParentPos;
                if (!config.IKParent.localRotation.Approximately(config.OrigIKParentRot)) config.IKParent.localRotation = config.OrigIKParentRot;
            }

            /*if (grabbedObjectIndex == 0) PlayerBodyAnimator.SetBool(config.TriggerPressedBoolTransitionName, CheckTriggerPressed(config));
            else PlayerBodyAnimator.SetBool(config.TriggerPressedBoolTransitionName, false);*/
            if (grabbedObjectIndex == 0) PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, CheckTriggerPressed(config) ? 1.0f : 0.0f);
            else PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, 0.0f);
        }

        /// <summary>
        /// In Editor hand tester
        /// </summary>
        /// <param name="config">current hand config to test</param>
        /// <param name="handIndex">current grabbed item index to test</param>

        private void DebuggingHandsAnimationControl(HandConfig config, int handIndex)
        {
            int grabbedObjectIndex = handIndex == 0 ? LeftHandDebuggingInteractableIndex : RightHandDebuggingInteractableIndex;
            for (int i = 0; i < config.AnimatorBoolTransitionNames.Length; i++)
            {
                string parameterName = config.AnimatorBoolTransitionNames[i];
                if (i != grabbedObjectIndex - 1) PlayerBodyAnimator.SetBool(parameterName, false);
            }
            if (grabbedObjectIndex > 0) PlayerBodyAnimator.SetBool(config.AnimatorBoolTransitionNames[grabbedObjectIndex - 1], true);

            if (grabbedObjectIndex == 1) PlayerBodyAnimator.SetBool(config.TriggerPressedBoolTransitionName, handIndex == 0 ? LeftHandDebbuggingTriggerPressed : RightHandDebbuggingTriggerPressed) ;
            else PlayerBodyAnimator.SetBool(config.TriggerPressedBoolTransitionName, false);

            config.ConnectedIKArm.target = config.HandIKTargets[grabbedObjectIndex];
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
                    // Store the grip's transform (PoseOverride or the interactable's transform)
                    config.LockedForegripTransform = config.CurrentInteractable.PoseOverride ?? config.CurrentInteractable.transform;
                }
            }
        }

        /// <summary>
        /// Determine the type of object grabbed by the hand
        /// </summary>
        private int GetGrabbedObjectIndex(HandConfig config)
        {
            int grabbedObjectIndex = -1;

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

                if (stillHoldingForegrip || holdingForegripsParentGun)
                {
                    // Still holding the foregrip OR the gun transferred to this hand - maintain handguard position
                    return 2;
                }
                else
                {
                    // User released - unlock
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
                        // Gun is being held by the foregrip, use handguard position
                        Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using handguard position for gun held through foregrip");
                        grabbedObjectIndex = 2;
                    }
                    else
                    {
                        // Gun is being held normally by the trigger grip
                        grabbedObjectIndex = 0;
                    }
                }
                // Grabbung mag
                else if (typeof(FVRFireArmMagazine) == currentInteractableType) grabbedObjectIndex = 1;
                // Grabbing tube fed shotgun handle (MUST come before FVRAlternateGrip check since it's a subclass)
                else if (typeof(TubeFedShotgunHandle).IsAssignableFrom(currentInteractableType))
                {
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED TubeFedShotgunHandle! Setting index 12");
                    grabbedObjectIndex = 12;
                    TubeFedShotgunHandle shotgunHandle = config.CurrentInteractable as TubeFedShotgunHandle;
                    TryLockToAlternateGrip(config, shotgunHandle?.Shotgun);
                }
                // Grabbing foregrip
                else if (typeof(FVRAlternateGrip).IsAssignableFrom(currentInteractableType))
                {
                    Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED FVRAlternateGrip! Setting index 2");
                    grabbedObjectIndex = 2;
                    FVRAlternateGrip altGrip = config.CurrentInteractable as FVRAlternateGrip;
                    TryLockToAlternateGrip(config, altGrip?.PrimaryObject);
                }
                // Grabbing handgun slide
                else if (typeof(HandgunSlide) == currentInteractableType) grabbedObjectIndex = 3;
                // Grabbing round
                else if (typeof(FVRFireArmRound) == currentInteractableType) grabbedObjectIndex = 4;
                // Grabbing pinned grenade
                else if (typeof(PinnedGrenade) == currentInteractableType) grabbedObjectIndex = 5;
                // Grabbing top cover
                else if (typeof(FVRFireArmTopCover) == currentInteractableType) grabbedObjectIndex = 6;
                // Grabbing bolt handle
                else if (typeof(ClosedBoltHandle) == currentInteractableType) grabbedObjectIndex = 8;
                // Grabbing closed bolt
                else if (typeof(ClosedBolt) == currentInteractableType) grabbedObjectIndex = 9;
                // Grabbing bolt action rifle handle
                else if (typeof(BoltActionRifle_Handle) == currentInteractableType) grabbedObjectIndex = 10;
                // Grabbing open bolt charging handle
                else if (typeof(OpenBoltChargingHandle) == currentInteractableType) grabbedObjectIndex = 11;
                /*// Grabbing open bolt receiver bolt
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
                    grabbedObjectIndex = 7;
                }
            }
            return grabbedObjectIndex;
        }

        /// <summary>
        ///  Determine whether the trigger is pressed
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