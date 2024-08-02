using BepInEx.Bootstrap;
using FistVR;
using OpenScripts2;
using System.Reflection;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// This component handles primarily setting up the trackers. It can also take over tracking if H3MP is not installed.
    /// </summary>
    public class PlayerBodyTrackingController : MonoBehaviour
    {
        [Header("This component controls how the PlayerBody root and")]
        [Header("IK (Inverse Kinematics) targets follow the controllers and VR headset.")]
        [Header("It is able to take over tracking if H3MP cannot be found.")]
        public Transform PlayerBodyRoot;
        public Transform HeadsetTracker;
        public Transform LeftControllerTracker;
        public Transform RightControllerTracker;

        // Use this for initialization
        public void Start()
        {
            // Unparent IK Targets for better performance. Even though the increase may be small, every little bit counts.
            HeadsetTracker.SetParent(null);
            LeftControllerTracker.SetParent(null);
            RightControllerTracker.SetParent(null);
        }

        // Update is called once per frame
        public void Update()
        {
            // Update PlayerBody Root position and rotation. The root is being kept vertically aligned while looking into the same direction and at the same position as the headset.
            PlayerBodyRoot.position = HeadsetTracker.position;
            Vector3 headForward = HeadsetTracker.forward;
            headForward.y = 0;
            // Normalize direction after setting Y to zero to keep length at 1.
            headForward = headForward.normalized;
            // Apply direction using Quaternion magic!
            PlayerBodyRoot.rotation = Quaternion.LookRotation(headForward, Vector3.up);

            // Take over controller and headset tracking if H3MP couldn't be found.
            if (!OpenScripts2_BasePlugin.IsInEditor && H3VRPlayerBodySystem_BepInEx.H3MPLoaded)
            {
                HeadsetTracker.position = GM.CurrentPlayerBody.Head.position;
                HeadsetTracker.rotation = GM.CurrentPlayerBody.Head.rotation;

                LeftControllerTracker.position = GM.CurrentPlayerBody.LeftHand.position;
                LeftControllerTracker.rotation = GM.CurrentPlayerBody.LeftHand.rotation;

                RightControllerTracker.position = GM.CurrentPlayerBody.RightHand.position;
                RightControllerTracker.rotation = GM.CurrentPlayerBody.RightHand.rotation;
            }
        }
    }
}