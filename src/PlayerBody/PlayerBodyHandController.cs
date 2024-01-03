// Original Script by AngryNoob
// Modification by Cityrobo
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using Valve.VR;
using RootMotion.FinalIK;
using H3MP;
using H3MP.Scripts;
using UnityEngine.Serialization;
using System.Linq;

namespace PlayerBodySystem
{
    public class PlayerBodyHandController : MonoBehaviour
    {
        //[Tooltip("This GameObject has the H3MP player body script on it.")]
        //public GameObject PlayerBodyRoot;
        [Header("This component controls how the hands move.")]
        [Header("Many fields have tooltips, just hover over them!")]
        [Header("(On the left side, where the name of the field is.)")]
        [Tooltip("H3MP Player Body reference used for determining player body state in multiplayer.")]
        public PlayerBody H3MPPlayerBody;
        [Tooltip("VRIK script, used to swap out hand IK target depending on what type of object is being grabbed.")]
        public VRIK VRIKInstance;
        [Tooltip("Animator on the player body rig.")]
        public Animator PlayerBodyAnimator;
        [Tooltip("Left Hand (Element 0), then right hand (Element 1).\nIf you have more than two hands, lucky you, if you have less, I'm sorry!\nHowever, this is sadly not supported!")]
        public HandConfig[] HandConfigs;

        [Header("In-Editor Debugging Settings")]
        [Header("Make sure to turn off before building your playerbody for in game use!)")]
        public bool InEditorDebuggingEnabled;
        [Header("Left Hand Debugging")]
        [Tooltip("Simulate left hand held item state:\n0 = no item in hands, \n1 = weapon grip, \n2 = magazine grip, \n3 = handguard grip, \n4 = bolt handle grip, \n5 = pistol slide grip, \n6 = bullet grip, \n7 = grenade grip, \n8 = pistol two-handed grip, \n9 = top cover grip.")]
        [Range(0,9)]
        public int LeftHandDebuggingInteractableIndex = 0;
        [Tooltip("Simulate trigger pressed while holding weapon.")]
        public bool LeftHandDebbuggingTriggerPressed = false;
        [Header("Right Hand Debugging")]
        [Tooltip("Simulate right hand held item state:\n0 = no item in hands, \n1 = weapon grip, \n2 = magazine grip, \n3 = handguard grip, \n4 = bolt handle grip, \n5 = pistol slide grip, \n6 = bullet grip, \n7 = grenade grip, \n8 = pistol two-handed grip, \n9 = top cover grip.")]
        [Range(0, 9)]
        public int RightHandDebuggingInteractableIndex = 0;
        [Tooltip("Simulate trigger pressed while holding weapon.")]
        public bool RightHandDebbuggingTriggerPressed = false;

        [Serializable]
        public class HandConfig
        {
            [Tooltip("Hand IK tracking positions: \n0 = no item in hands, \n1 = weapon grip, \n2 = magazine grip, \n3 = handguard grip, \n4 = bolt handle grip, \n5 = pistol slide grip, \n6 = bullet grip, \n7 = grenade grip, \n8 = pistol two-handed grip, \n9 = top cover grip.")]
            public Transform[] HandIKTargets;
            [Tooltip("Names of the transition bool that trigger the grab dependent finger animations: \n0 = weapon grip, \n1 = magazine grip, \n2 = handguard grip, \n3 = bolt handle grip, \n4 = pistol slide grip, \n5 = bullet grip, \n6 = grenade grip, \n7 = offhand two-handed pistol grip, \n8 = top cover grip.")]
            public string[] AnimatorBoolTransitionNames;
            [Tooltip("Name of the transition bool that triggers when pressing the trigger while holding a gun.")]
            public string TriggerPressedBoolTransitionName;
            //public AngryNoob_FingerTracking_Translated FingerTracking;

            [HideInInspector]
            public HandConfig OtherHandConfig;
            [HideInInspector]
            public bool TwoHandHolding = false;
            [HideInInspector]
            public FVRViveHand Controller;
            [HideInInspector]
            public FVRInteractiveObject CurrentInteractable => Controller.CurrentInteractable;
            [HideInInspector]
            public FVRViveHand OtherHand => Controller.OtherHand;
            [HideInInspector]
            public IKSolverVR.Arm ConnectedIKArm;
            [HideInInspector]
            public bool IsThisTheRightHand => Controller.IsThisTheRightHand;
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
            if (HandConfigs.Length != 2) Debug.LogError(this + ": Either you have less than two hands, or more. If you have less, I'm sorry, if you have more, lucky you! In any case, this won't work with PlayerBodies. Sorry!");
            else 
            {
                // Subscribe to H3MP PlayerBodyInit event
                GameManager.OnPlayerBodyInit += OnPlayerBodyInit;
                for (int i = 0; i < HandConfigs.Length; i++)
                {
                    HandConfigs[i].ConnectedIKArm = i == 0 ? VRIKInstance.solver.leftArm : VRIKInstance.solver.rightArm;
                    HandConfigs[i].OtherHandConfig = HandConfigs[1 - i];
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
                    CheckIfItemInHand(config);
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
        /// Check if there's and object in hand and activate index finger tracking if not or continue with animation procedure
        /// </summary>
        private void CheckIfItemInHand(HandConfig config)
        {
            if (config.CurrentInteractable != null)
            {
                //config.FingerTracking.ItemBeingGrabbed = true;

                UpdateIKTargetAndAnimate(config);
            }
            else if (config.OtherHand.CurrentInteractable != null && config.CurrentInteractable == null && DoubleHandMasturbating(config))
            {
                //config.FingerTracking.ItemBeingGrabbed = true;

                UpdateIKTargetAndAnimate(config);
            }
            else
            {
                foreach (var parameterName in config.AnimatorBoolTransitionNames)
                {
                    PlayerBodyAnimator.SetBool(parameterName, false);
                }
                config.ConnectedIKArm.target = config.HandIKTargets[0];

                //config.FingerTracking.ItemBeingGrabbed = false;

                UpdateFingerTracking(config);
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
                PlayerBodyAnimator.SetBool(config.AnimatorBoolTransitionNames[grabbedObjectIndex], true);
                config.ConnectedIKArm.target = config.HandIKTargets[grabbedObjectIndex + 1];
            }
            else
            {
                PlayerBodyAnimator.SetBool(config.AnimatorBoolTransitionNames[0], true);
                config.ConnectedIKArm.target = config.HandIKTargets[1];
            }

            if (grabbedObjectIndex == 0) PlayerBodyAnimator.SetBool(config.TriggerPressedBoolTransitionName, CheckTriggerPressed(config));
            else PlayerBodyAnimator.SetBool(config.TriggerPressedBoolTransitionName, false);
        }

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
        /// Determine the type of object grabbed by the hand
        /// </summary>
        private int GetGrabbedObjectIndex(HandConfig config)
        {
            int grabbedObjectIndex = -1;
            Type currentInteractableType = config.CurrentInteractable.GetType();
            // Grabbing gun
            if (typeof(FVRFireArm).IsAssignableFrom(currentInteractableType)) grabbedObjectIndex = 0;
            // Grabbung mag
            else if (typeof(FVRFireArmMagazine) == currentInteractableType) grabbedObjectIndex = 1;
            // Grabbing foregrip
            else if (typeof(FVRAlternateGrip).IsAssignableFrom(currentInteractableType)) grabbedObjectIndex = 2;
            // Grabbing bolt handle
            else if (typeof(ClosedBoltHandle) == currentInteractableType || typeof(ClosedBolt) == currentInteractableType
                 || typeof(BoltActionRifle_Handle) == currentInteractableType || typeof(OpenBoltChargingHandle) == currentInteractableType
                 || typeof(OpenBoltReceiverBolt) == currentInteractableType || typeof(TubeFedShotgunBolt) == currentInteractableType) grabbedObjectIndex = 3;
            // Grabbing handgun slide
            else if (typeof(HandgunSlide) == currentInteractableType) grabbedObjectIndex = 4;
            // Grabbing round
            else if (typeof(FVRFireArmRound) == currentInteractableType) grabbedObjectIndex = 5;
            // Grabbing Grenade
            else if (typeof(PinnedGrenade) == currentInteractableType || typeof(FVRCappedGrenade) == currentInteractableType) grabbedObjectIndex = 6;
            // Grabbing pistol with two hands
            else if (DoubleHandMasturbating(config) == true && config.CurrentInteractable == null) grabbedObjectIndex = 7;
            // Grabbing top cover
            else if (typeof(FVRFireArmTopCover) == currentInteractableType) grabbedObjectIndex = 8;
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
            if (DistanceBetweenBothHands() <= 0.15f && config.CurrentInteractable == null)
            {
                config.TwoHandHolding = true;
            }
            if (DistanceBetweenBothHands() > 0.22f)
            {
                config.TwoHandHolding = false;
            }
            return config.TwoHandHolding;
        }

        private float DistanceBetweenBothHands() => Vector3.Distance(GM.CurrentMovementManager.LeftHand.position, GM.CurrentMovementManager.RightHand.position);
    }
}