// Original Script by AngryNoob
// Modification by Cityrobo, chomilk
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
    ///
    /// CUSTOM GUN-SPECIFIC GRIP POSES:
    /// You can define custom hand poses for specific guns or gun families using pattern matching in the gripId field.
    ///
    /// Pattern Syntax:
    /// - EXACT:gunname:griptype       - Matches exact GameObject name (e.g., "EXACT:krissvector45:HandGuard")
    /// - STARTSWITH:pattern:griptype  - Matches names starting with pattern (e.g., "STARTSWITH:krissvector:HandGuard")
    /// - CONTAINS:pattern:griptype    - Matches names containing pattern (e.g., "CONTAINS:vector:HandGuard")
    /// - griptype                     - Default grip (no pattern matching)
    ///
    /// Matching Priority (most specific to least specific):
    /// 1. EXACT matches
    /// 2. STARTSWITH matches
    /// 3. CONTAINS matches
    /// 4. Default grip
    ///
    /// Example Use Cases:
    /// 1. Vertical foregrip for Kriss Vector family:
    ///    - gripId: "STARTSWITH:krissvector:HandGuard"
    ///    - Will match: krissvector45, krissvector9mm, krissvector45acp, etc.
    ///
    /// 2. Single gun override:
    ///    - gripId: "EXACT:krissvector45:HandGuard"
    ///    - Will match: Only krissvector45 (exact GameObject name)
    ///
    /// 3. Generic vector family:
    ///    - gripId: "CONTAINS:vector:HandGuard"
    ///    - Will match: Any gun with "vector" in the name
    ///
    /// All pattern matching is case-insensitive.
    ///
    /// How to Set Up in Unity Inspector:
    /// 1. Add your default "HandGuard" grip mapping as usual
    /// 2. Create additional grip mappings for specific guns:
    ///    - Set gripId to "STARTSWITH:krissvector:HandGuard" (or EXACT/CONTAINS)
    ///    - Configure the IK target and animator parameter for this custom pose
    ///    - Adjust rotation/position as needed for the vertical grip
    /// 3. When you grab a gun named "krissvector45", it will use the custom pose
    /// 4. When you grab other guns, they'll use the default "HandGuard" pose
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
            [Tooltip("Unique identifier for this grip type (e.g., 'Pistol', 'Magazine', 'Foregrip')\n\n" +
                     "PATTERN MATCHING (for gun-specific custom poses):\n" +
                     "- EXACT:gunname:griptype - Exact match (e.g., 'EXACT:krissvector45:HandGuard')\n" +
                     "- STARTSWITH:pattern:griptype - Starts with pattern (e.g., 'STARTSWITH:krissvector:HandGuard')\n" +
                     "- CONTAINS:pattern:griptype - Contains pattern (e.g., 'CONTAINS:vector:HandGuard')\n" +
                     "Priority: EXACT > STARTSWITH > CONTAINS > default grip")]
            public string gripId;
            [Tooltip("IK target transform for this grip type")]
            public Transform ikTarget;
            [Tooltip("Animator bool parameter name for this grip type")]
            public string animatorParameter;
            [Tooltip("If true, hand follows the rotation of the grabbed gameobject (e.g., bolt handles, bolt action handles). If false, hand follows controller rotation for more natural poses.")]
            public bool followGameObjectRotation = true;
            [Tooltip("Optional GameObject to use as rotation offset source. If set, this GameObject's rotation will be captured at initialization and used as the offset. If not set, rotationOffsetEuler will be used instead. Applied when followGameObjectRotation is FALSE (controller rotation mode).")]
            public Transform rotationOffsetSource;
            [Tooltip("Rotation offset in euler angles (X, Y, Z) applied when followGameObjectRotation is FALSE (controller rotation mode). Only used if rotationOffsetSource is not set.")]
            public Vector3 rotationOffsetEuler = Vector3.zero;
            [Tooltip("If true, this grip mapping will be ignored when using MirrorIK functions in PlayerBodyCopyTools.")]
            public bool ignoreMirrorIK = false;

            [HideInInspector]
            public Quaternion cachedRotationOffset = Quaternion.identity;

            // Pattern matching fields (parsed from gripId)
            [HideInInspector]
            public GripMatchMode matchMode = GripMatchMode.Default;
            [HideInInspector]
            public string matchPattern = "";
            [HideInInspector]
            public string baseGripType = "";
        }

        /// <summary>
        /// Matching modes for gun-specific grip poses
        /// </summary>
        public enum GripMatchMode
        {
            Default,      // Standard grip matching (no pattern)
            Exact,        // Exact GameObject name match
            StartsWith,   // GameObject name starts with pattern
            Contains      // GameObject name contains pattern
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
            public const string DoubleHandDerringer = "DoubleHandDerringer";
            public const string ClosedBoltHandle = "ClosedBoltHandle";
            public const string ClosedBolt = "ClosedBolt";
            public const string BoltActionHandle = "BoltActionHandle";
            public const string TubeFedShotgunHandle = "TubeFedShotgunHandle";
            public const string Derringer = "Derringer";
            public const string OpenBoltChargingHandle = "OpenBoltChargingHandle";
            public const string OpenBoltReceiverBolt = "OpenBoltReceiverBolt";
            public const string OpenBoltRotatingChargingHandle = "OpenBoltRotatingChargingHandle";
            public const string LeverActionFirearm = "LeverActionFirearm";
            public const string LeverActionFirearmUnlocked = "LeverActionFirearmUnlocked";
            public const string CappedGrenade = "CappedGrenade";
            public const string G11ChargingHandle = "G11ChargingHandle";
            public const string RPG7Foregrip = "RPG7Foregrip";
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
                        HandConfigs[i].IKParent = emptyGrip.ikTarget.parent.parent;
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
        /// Initialize grip mappings from GripMappings array and cache rotation offsets
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

                        // Parse pattern matching syntax (EXACT:name:grip, STARTSWITH:pattern:grip, CONTAINS:pattern:grip)
                        ParseGripPattern(mapping);

                        // Cache rotation offset at initialization
                        if (mapping.rotationOffsetSource != null)
                        {
                            mapping.cachedRotationOffset = mapping.rotationOffsetSource.rotation;
                        }
                        else
                        {
                            mapping.cachedRotationOffset = Quaternion.Euler(mapping.rotationOffsetEuler);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"GripMappings array is empty for {(config.IsThisTheRightHand ? "right" : "left")} hand! Please configure GripMappings in the inspector.");
            }
        }

        /// <summary>
        /// Parse grip pattern from gripId string (e.g., "EXACT:krissvector45:HandGuard")
        /// </summary>
        private void ParseGripPattern(GripMapping mapping)
        {
            string gripId = mapping.gripId;

            // Check for pattern matching syntax
            if (gripId.Contains(":"))
            {
                string[] parts = gripId.Split(':');
                if (parts.Length == 3)
                {
                    string modeStr = parts[0].ToUpper();
                    mapping.matchPattern = parts[1].ToLower(); // Case-insensitive matching
                    mapping.baseGripType = parts[2];

                    switch (modeStr)
                    {
                        case "EXACT":
                            mapping.matchMode = GripMatchMode.Exact;
                            break;
                        case "STARTSWITH":
                            mapping.matchMode = GripMatchMode.StartsWith;
                            break;
                        case "CONTAINS":
                            mapping.matchMode = GripMatchMode.Contains;
                            break;
                        default:
                            Debug.LogWarning($"Unknown grip match mode '{modeStr}' in gripId '{gripId}'. Using Default mode.");
                            mapping.matchMode = GripMatchMode.Default;
                            mapping.baseGripType = gripId;
                            break;
                    }
                }
                else
                {
                    // Invalid format, treat as default
                    mapping.matchMode = GripMatchMode.Default;
                    mapping.baseGripType = gripId;
                }
            }
            else
            {
                // No pattern, use standard grip matching
                mapping.matchMode = GripMatchMode.Default;
                mapping.baseGripType = gripId;
            }
        }

        /// <summary>
        /// Get the object name to use for pattern matching
        /// For foregrips/alternate grips, returns the parent gun's name instead of the foregrip's name
        /// This allows pattern matching to work on gun names regardless of which part you're holding
        /// </summary>
        private string GetObjectNameForPatternMatching(HandConfig config)
        {
            if (config.CurrentInteractable == null)
                return null;

            // Check if we're holding a foregrip/alternate grip
            if (config.CurrentInteractable is FVRAlternateGrip altGrip)
            {
                // Use the parent gun's name for pattern matching
                if (altGrip.PrimaryObject != null)
                    return altGrip.PrimaryObject.gameObject.name;
            }
            // Check if we're holding a TubeFedShotgunHandle
            else if (config.CurrentInteractable is TubeFedShotgunHandle shotgunHandle)
            {
                // Use the parent shotgun's name for pattern matching
                if (shotgunHandle.Shotgun != null)
                    return shotgunHandle.Shotgun.gameObject.name;
            }
            // Check if we're holding a gun through its foregrip (IsAltHeld)
            else if (config.CurrentInteractable is FVRPhysicalObject physObj && physObj.IsAltHeld)
            {
                // Already holding the gun itself, just grabbed via foregrip
                return physObj.gameObject.name;
            }

            // Default: return the object's own name
            return config.CurrentInteractable.gameObject.name;
        }

        /// <summary>
        /// Find the best matching grip for a given grip type and object name
        /// Priority: EXACT match > STARTSWITH match > CONTAINS match > default grip
        /// </summary>
        private GripMapping FindBestGripMatch(HandConfig config, string baseGripType, string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                // No object name, fall back to standard grip
                if (config.gripMappingDict.TryGetValue(baseGripType, out GripMapping defaultGrip))
                    return defaultGrip;
                return null;
            }

            string objectNameLower = objectName.ToLower();
            GripMapping exactMatch = null;
            GripMapping startsWithMatch = null;
            GripMapping containsMatch = null;
            GripMapping defaultMatch = null;

            // Search through all grip mappings for this base type
            foreach (var mapping in config.GripMappings)
            {
                // Skip if base grip type doesn't match
                if (mapping.baseGripType != baseGripType)
                    continue;

                // Check match type
                switch (mapping.matchMode)
                {
                    case GripMatchMode.Exact:
                        if (objectNameLower == mapping.matchPattern && exactMatch == null)
                            exactMatch = mapping;
                        break;

                    case GripMatchMode.StartsWith:
                        if (objectNameLower.StartsWith(mapping.matchPattern) && startsWithMatch == null)
                            startsWithMatch = mapping;
                        break;

                    case GripMatchMode.Contains:
                        if (objectNameLower.Contains(mapping.matchPattern) && containsMatch == null)
                            containsMatch = mapping;
                        break;

                    case GripMatchMode.Default:
                        if (defaultMatch == null)
                            defaultMatch = mapping;
                        break;
                }
            }

            // Return best match based on priority
            if (exactMatch != null) return exactMatch;
            if (startsWithMatch != null) return startsWithMatch;
            if (containsMatch != null) return containsMatch;
            if (defaultMatch != null) return defaultMatch;

            // No match found, try dictionary lookup as final fallback
            if (config.gripMappingDict.TryGetValue(baseGripType, out GripMapping fallback))
                return fallback;

            return null;
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
        /// Get cached rotation offset from GripMapping (initialized once at startup)
        /// </summary>
        private Quaternion GetRotationOffset(GripMapping gripMapping)
        {
            return gripMapping.cachedRotationOffset;
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

            // Get the GameObject name for pattern matching
            // For foregrips, use the parent gun's name instead of the foregrip's name
            string objectName = GetObjectNameForPatternMatching(config);

            // Find the best matching grip (supports pattern matching for gun-specific poses)
            GripMapping currentGrip = gripId != null ? FindBestGripMatch(config, gripId, objectName) : null;

            if (currentGrip != null)
            {
                // Grabbing something or double handing
                if (!string.IsNullOrEmpty(currentGrip.animatorParameter))
                {
                    PlayerBodyAnimator.SetBool(currentGrip.animatorParameter, true);
                }
                config.ConnectedIKArm.target = currentGrip.ikTarget;

                if (gripId != GripIds.DoubleHand && gripId != GripIds.DoubleHandDerringer)
                {
                    // Use new priority-based transform selection
                    Transform targetTransform = GetTransformForIK(config, gripId);

                    // Apply IK offset to the selected transform
                    if (targetTransform != null)
                    {
                        // Always update position to the object's position
                        if (!config.IKParent.position.Approximately(targetTransform.position))
                            config.IKParent.position = targetTransform.position;

                        // Choose rotation source based on followGameObjectRotation setting
                        if (currentGrip.followGameObjectRotation)
                        {
                            // Follow the object's rotation (e.g., for handguards, top covers)
                            // This maintains the hand pose relative to the object as it rotates
                            // Use Approximately check since object rotation doesn't update every frame
                            if (!config.IKParent.rotation.Approximately(targetTransform.rotation))
                                config.IKParent.rotation = targetTransform.rotation;
                        }
                        else
                        {
                            // Follow controller's rotation instead of object rotation
                            // This keeps hand angles natural when manipulating things like bolt handles
                            // Position is still at the object, but rotation follows the controller
                            // Apply rotation offset to align with unpredictable controller orientations
                            // Always update since controller is constantly moving
                            Quaternion baseRotation = config.Controller.transform.rotation;
                            Quaternion offsetRotation = GetRotationOffset(currentGrip);
                            Quaternion finalRotation = baseRotation * offsetRotation;

                            config.IKParent.rotation = finalRotation;
                        }
                    }
                }
                // Double Hand Grab - Use simplified priority for other hand's object
                else if (gripId == GripIds.DoubleHand || gripId == GripIds.DoubleHandDerringer)
                {
                    // For double-hand grip, check other hand's object with simplified priority
                    Transform targetTransform = null;
                    if (config.OtherHand.CurrentInteractable != null)
                    {
                        if (config.OtherHand.CurrentInteractable.PoseOverride_Touch != null)
                            targetTransform = config.OtherHand.CurrentInteractable.PoseOverride_Touch;
                        else if (config.OtherHand.CurrentInteractable.PoseOverride != null)
                            targetTransform = config.OtherHand.CurrentInteractable.PoseOverride;
                        else
                            targetTransform = config.OtherHand.CurrentInteractable.transform;
                    }

                    if (targetTransform != null)
                    {
                        // Always update position
                        if (!config.IKParent.position.Approximately(targetTransform.TransformPoint(config.OrigIKParentPos)))
                        //if (!config.IKParent.position.Approximately(targetTransform.position))
                            config.IKParent.position = targetTransform.position;
                            //config.IKParent.position = targetTransform.TransformPoint(config.OrigIKParentPos);

                        // Only update rotation if followGameObjectRotation is true
                        if (currentGrip.followGameObjectRotation)
                        {
                            if (!config.IKParent.rotation.Approximately(targetTransform.rotation))
                                config.IKParent.rotation = targetTransform.rotation;
                        }
                        else
                        {
                            // Use controller's rotation instead of gameobject rotation
                            // Apply rotation offset to align with unpredictable controller orientations
                            Quaternion baseRotation = config.Controller.transform.rotation;
                            Quaternion offsetRotation = GetRotationOffset(currentGrip);
                            Quaternion finalRotation = baseRotation * offsetRotation;

                            if (!config.IKParent.rotation.Approximately(finalRotation))
                                config.IKParent.rotation = finalRotation;
                        }
                    }
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

            // Handle trigger press animation (always set, users can choose to use it in animator or not)
            PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, GetTriggerPullValue(config));
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

            // Handle trigger pull for gun grips, derringer, and RPG7 foregrip
            float triggerPull = handIndex == 0 ? LeftHandDebbuggingTriggerPull : RightHandDebbuggingTriggerPull;
            if (debugGrip != null && (debugGrip.gripId == GripIds.Gun || debugGrip.gripId == GripIds.Derringer || debugGrip.gripId == GripIds.RPG7Foregrip))
            {
                PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, triggerPull);
            }
            else
            {
                PlayerBodyAnimator.SetFloat(config.TriggerPressedBoolTransitionName, 0.0f);
            }
        }

        /// <summary>
        /// Get the appropriate transform for IK positioning based on priority:
        /// Locked Foregrip > Direct Foregrip Hold > IsAltHeld > PoseOverride_Touch > PoseOverride > Fallback
        /// </summary>
        private Transform GetTransformForIK(HandConfig config, string gripId)
        {
            if (config.CurrentInteractable == null)
                return null;

            // Priority 0: If handguard position is locked, always use locked transform
            if (config.IsLockedToHandguardPosition && config.LockedForegripTransform != null)
            {
                // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using locked foregrip transform");
                return config.LockedForegripTransform;
            }

            // Priority 1: Direct Transform for handguards
            // This handles the case where you grab the foregrip first OR hold gun through foregrip
            if (gripId == GripIds.Handguard || gripId == GripIds.TubeFedShotgunHandle)
            {
                // Check if directly holding TubeFedShotgunHandle
                if (config.CurrentInteractable is TubeFedShotgunHandle shotgunHandle)
                {
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using direct TubeFedShotgunHandle transform: {shotgunHandle.transform.name}");
                    return shotgunHandle.transform;
                }
                // Check if directly holding foregrip component
                else if (config.CurrentInteractable is FVRAlternateGrip altGripComponent)
                {
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using direct FVRAlternateGrip transform: {altGripComponent.transform.name}");
                    return altGripComponent.transform;
                }
                // Check if holding gun through foregrip (IsAltHeld)
                else if (config.CurrentInteractable is FVRPhysicalObject physObj && physObj.IsAltHeld)
                {
                    // First try to use AltGrip if it's set
                    if (physObj.AltGrip != null)
                    {
                        // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using AltGrip direct transform (IsAltHeld): {physObj.AltGrip.transform.name}");
                        return physObj.AltGrip.transform;
                    }
                    // AltGrip is null - search for foregrip components (happens when grabbing foregrip first)
                    else
                    {
                        FVRAlternateGrip[] foregrips = physObj.GetComponentsInChildren<FVRAlternateGrip>();
                        if (foregrips != null && foregrips.Length > 0)
                        {
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Found foregrip via GetComponentsInChildren: {foregrips[0].transform.name}");
                            return foregrips[0].transform;
                        }
                        else
                        {
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] IsAltHeld but no foregrip found, falling through");
                        }
                    }
                }
            }

            // Priority 2: PoseOverride_Touch (for normal gun grip and other objects)
            if (config.CurrentInteractable.PoseOverride_Touch != null)
            {
                // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using PoseOverride_Touch: {config.CurrentInteractable.PoseOverride_Touch.name}");
                return config.CurrentInteractable.PoseOverride_Touch;
            }

            // Priority 3: PoseOverride (standard H3VR pose override)
            if (config.CurrentInteractable.PoseOverride != null)
            {
                // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using PoseOverride: {config.CurrentInteractable.PoseOverride.name}");
                return config.CurrentInteractable.PoseOverride;
            }

            // Priority 4: AltGrip transform (if available but not already used)
            if (config.CurrentInteractable is FVRPhysicalObject physObjAlt && physObjAlt.AltGrip != null)
            {
                // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using AltGrip transform as fallback: {physObjAlt.AltGrip.transform.name}");
                return physObjAlt.AltGrip.transform;
            }

            // Priority 5: Fallback - Direct interactable transform
            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Using fallback interactable transform: {config.CurrentInteractable.transform.name}");
            return config.CurrentInteractable.transform;
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

                    // For all alternate grips (including shotguns and regular handguards), use direct transform (center position)
                    // This should provide more consistent positioning
                    config.LockedForegripTransform = config.CurrentInteractable.transform;
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locking foregrip with direct transform (center position)");
                }
            }
        }

        /// <summary>
        /// Check if a LeverActionFirearm's lever is unlocked (not at rear position)
        /// Returns true if lever is NOT at rear position (action open or cycling)
        /// This is used to switch to a hand pose that doesn't clip when the lever is being operated
        /// </summary>
        private bool IsLeverActionUnlocked(LeverActionFirearm leverGun)
        {
            if (leverGun == null) return false;

            // Access private fields using reflection since they're not public
            // m_curLeverPos: Current lever position (Forward, Middle, or Rear)

            System.Reflection.FieldInfo curLeverPosField = typeof(LeverActionFirearm).GetField("m_curLeverPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (curLeverPosField == null)
            {
                Debug.LogWarning("Failed to get lever action fields via reflection - lever unlock detection will not work");
                return false;
            }

            // Get ZPos enum value (0=Forward, 1=Middle, 2=Rear)
            int curLeverPos = (int)curLeverPosField.GetValue(leverGun);

            // Use the unlocked pose when lever is NOT at rear position
            // This prevents hand clipping when cycling the lever
            // ZPos.Rear = 2, so return true when NOT at rear (0 or 1)
            const int ZPos_Rear = 2;
            return curLeverPos != ZPos_Rear;
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
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked foregrip is TubeFedShotgunHandle");
                }
                else if (config.LockedForegripReference is FVRAlternateGrip lockedAltGrip &&
                         lockedAltGrip.PrimaryObject != null &&
                         typeof(TubeFedShotgun).IsAssignableFrom(lockedAltGrip.PrimaryObject.GetType()))
                {
                    lockedGripId = GripIds.TubeFedShotgunHandle;
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked foregrip parent is TubeFedShotgun, using TubeFedShotgunHandle pose");
                }

                if (stillHoldingForegrip)
                {
                    // Still holding the foregrip - maintain handguard position
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked to handguard, still holding foregrip");
                    return lockedGripId;
                }
                else if (holdingForegripsParentGun)
                {
                    // Gun transferred to this hand - still maintain handguard position
                    // Keep using handguard pose since we originally grabbed it via the foregrip
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Locked to handguard, gun transferred but maintaining handguard pose");
                    return lockedGripId;
                }
                else
                {
                    // User released - unlock
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Unlocking handguard position");
                    config.IsLockedToHandguardPosition = false;
                    config.LockedForegripReference = null;
                    config.LockedForegripTransform = null;
                }
            } 

            if (config.CurrentInteractable != null)
            {
                Type currentInteractableType = config.CurrentInteractable.GetType();
                // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] CurrentInteractable type: {currentInteractableType.Name}");

                // Grabbing gun
                if (typeof(FVRFireArm).IsAssignableFrom(currentInteractableType))
                {
                    // Check if this gun is being held through an alternate grip (foregrip)
                    FVRPhysicalObject physObj = config.CurrentInteractable as FVRPhysicalObject;
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Gun detected. AltGrip: {physObj?.AltGrip?.name}, IsAltHeld: {physObj?.IsAltHeld}");

                    if (physObj != null && physObj.IsAltHeld)
                    {
                        // THIS hand is holding gun through foregrip (IsAltHeld means this hand grabbed via foregrip)

                        // Check if it's an RPG7 held through its foregrip - use RPG7Foregrip pose instead of generic handguard
                        if (physObj.AltGrip != null && physObj.AltGrip.GetType().Name == "RPG7Foregrip")
                        {
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] RPG7 held through foregrip, using RPG7Foregrip pose");
                            gripId = GripIds.RPG7Foregrip;
                        }
                        // Check if it's a TubeFedShotgun - use shotgun handle pose instead of generic handguard
                        else if (typeof(TubeFedShotgun).IsAssignableFrom(currentInteractableType))
                        {
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] TubeFedShotgun held through foregrip, using TubeFedShotgunHandle pose");
                            gripId = GripIds.TubeFedShotgunHandle;
                        }
                        else
                        {
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Gun held through foregrip, using Handguard pose");
                            gripId = GripIds.Handguard;
                        }
                    }
                    else
                    {
                        // Gun is being held normally by the trigger grip
                        // Check if it's a Derringer - use Derringer pose instead of generic Gun pose
                        if (currentInteractableType.Name == "Derringer")
                        {
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] Derringer detected, using Derringer pose");
                            gripId = GripIds.Derringer;
                        }
                        // Check if it's a LeverActionFirearm - check if lever is unlocked to avoid clipping
                        else if (typeof(LeverActionFirearm).IsAssignableFrom(currentInteractableType))
                        {
                            LeverActionFirearm leverGun = config.CurrentInteractable as LeverActionFirearm;
                            bool isUnlocked = IsLeverActionUnlocked(leverGun);

                            // Use unlocked pose when lever is open/cycling to avoid hand clipping
                            gripId = isUnlocked ? GripIds.LeverActionFirearmUnlocked : GripIds.LeverActionFirearm;
                            // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] LeverActionFirearm detected, lever {(isUnlocked ? "UNLOCKED" : "LOCKED")}, using {gripId} pose");
                        }
                        else
                        {
                            // Standard gun pose
                            gripId = GripIds.Gun;
                        }
                    }
                }
                // Grabbing mag
                else if (typeof(FVRFireArmMagazine) == currentInteractableType) gripId = GripIds.Magazine;
                // Grabbing RPG7 foregrip (MUST come before FVRAlternateGrip check since it's a subclass)
                // RPG7Foregrip has trigger controls, so it should use a gun-like pose with trigger animation
                else if (currentInteractableType.Name == "RPG7Foregrip")
                {
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED RPG7Foregrip!");
                    gripId = GripIds.RPG7Foregrip;
                    // Don't lock to alternate grip for RPG7 - it's meant to be held like a gun grip
                }
                // Grabbing tube fed shotgun handle (MUST come before FVRAlternateGrip check since it's a subclass)
                else if (typeof(TubeFedShotgunHandle).IsAssignableFrom(currentInteractableType))
                {
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED TubeFedShotgunHandle!");
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
                // Grabbing open bolt charging handle
                else if (typeof(OpenBoltChargingHandle) == currentInteractableType) gripId = GripIds.OpenBoltChargingHandle;
                // Grabbing open bolt receiver bolt
                else if (typeof(OpenBoltReceiverBolt) == currentInteractableType) gripId = GripIds.OpenBoltReceiverBolt;
                // Grabbing open bolt rotating charging handle
                else if (typeof(OpenBoltRotatingChargingHandle) == currentInteractableType) gripId = GripIds.OpenBoltRotatingChargingHandle;
                // Grabbing capped grenade
                else if (typeof(FVRCappedGrenade) == currentInteractableType) gripId = GripIds.CappedGrenade;
                // Grabbing G11 charging handle
                else if (typeof(G11ChargingHandle) == currentInteractableType) gripId = GripIds.G11ChargingHandle;
                /*
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
                // Grabbing lever action firearm
                else if (typeof(LeverActionFirearm) == currentInteractableType) grabbedObjectIndex = 21;
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
                    // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] MATCHED FVRAlternateGrip!");
                    FVRAlternateGrip altGrip = config.CurrentInteractable as FVRAlternateGrip;

                    // Check if the parent gun is a TubeFedShotgun
                    if (altGrip?.PrimaryObject != null &&
                        typeof(TubeFedShotgun).IsAssignableFrom(altGrip.PrimaryObject.GetType()))
                    {
                        // Debug.Log($"[{(config.IsThisTheRightHand ? "RIGHT" : "LEFT")}] FVRAlternateGrip parent is TubeFedShotgun, using TubeFedShotgunHandle pose");
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
                    // Check what type of gun the other hand is holding
                    Type otherHandGunType = config.OtherHand.CurrentInteractable.GetType();

                    // If other hand is holding a Derringer, use DoubleHandDerringer pose
                    if (otherHandGunType.Name == "Derringer")
                    {
                        gripId = GripIds.DoubleHandDerringer;
                    }
                    else
                    {
                        // Standard two-handed grip for normal guns
                        gripId = GripIds.DoubleHand;
                    }
                }
            }
            return gripId;
        }

        /// <summary>
        /// Get trigger pull value with three states: discipline, resting, pulling
        /// Returns 0.0-0.7 for resting (finger on trigger but not pulling)
        /// Returns 0.7-1.0 for actual trigger pull
        /// Uses hybrid detection: capacitive touch OR finger curl position for universal controller support
        /// </summary>
        private float GetTriggerPullValue(HandConfig config)
        {
            float rawTriggerValue = config.Controller.Input.TriggerFloat;
            float indexFingerCurl = config.Controller.Input.FingerCurl_Index;
            bool capacitiveTouch = config.Controller.Input.TriggerTouched;

            // Hybrid touch detection: works on ALL controller types
            // - Capacitive touch: Index/Oculus Touch controllers (true capacitive sensors)
            // - Finger curl > 0.6: When holding gun, finger curl increases to ~0.65 when touching trigger
            bool isTouching = capacitiveTouch || (indexFingerCurl > 0.6f);

            const float restingThreshold = 0.15f;    // Light touch/rest detection threshold
            const float pullThreshold = 0.7f;        // Actual pull starts here
            const float fullPullValue = 0.95f;       // Value considered full pull
            const float fingerCurlTouchMin = 0.1f;   // When finger starts moving toward trigger
            const float fingerCurlTouchMax = 0.65f;  // When finger fully touches trigger

            // State 1: Trigger Discipline - Not touching at all
            if (!isTouching && rawTriggerValue < restingThreshold)
                return 0.0f;

            // State 2: Finger Resting on Trigger - Touching or light pressure but not pulling
            if (rawTriggerValue < pullThreshold)
            {
                // Use finger curl to smoothly transition from discipline (0.0) to resting (0.35)
                // FingerCurl ramps from ~0.0 to 0.65 when touching, we'll use that for smooth animation
                float fingerCurlBlend = Mathf.Clamp01((indexFingerCurl - fingerCurlTouchMin) / (fingerCurlTouchMax - fingerCurlTouchMin));
                float baseRestingValue = Mathf.Lerp(0.0f, 0.35f, fingerCurlBlend);

                // If also applying trigger pressure, blend towards ready-to-pull (0.35 to 0.7)
                float pressureBlend = rawTriggerValue / pullThreshold;
                return Mathf.Lerp(baseRestingValue, 0.7f, pressureBlend);
            }

            // State 3: Actually Pulling - Remap to upper range [0.7, 1.0]
            float pullProgress = (rawTriggerValue - pullThreshold) / (fullPullValue - pullThreshold);
            float remapped = 0.7f + (0.3f * pullProgress);
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