using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerBodySystem 
{
    public class PlayerBodyLegsController : MonoBehaviour 
    {
        [Header("This component controls the leg movement animation.")]
        [Header("Has some tooltips for your pleasure.")]
        [Tooltip("Speed at which the animation starts playing.")]
        public float AnimationActivationSpeedTreshold = 0.7f;
        [Tooltip("Animation Smoothing between different speed values.")]
        [Range(0,1)]
        public float AnimationSmoothing = 0.2f;

        [Tooltip("Animator on the PlayerBody rig.")]
        public Animator PlayerBodyAnimator;

        [Header("Animation transition names:")]
        public string IsMovingAnimationStateName = "IsMoving";
        public string LeftRightAnimationFloatName = "D_X";
        public string ForwardBackAnimationFloatName = "D_Y";

        private Vector3 _lastPos;

        private float _lastD_X = 0f;
        private float _lastD_Y = 0f;

        public void Awake()
        {
            _lastPos = transform.position;
        }
    
        public void Update () 
        {
            // calculate global speed vector of headset. we only care about velocity on the XZ plane, so Y will be zeroed out.
            Vector3 globalHeadsetSpeedVector = (transform.position - _lastPos) / Time.deltaTime;
            globalHeadsetSpeedVector.y = 0;
            // convert to local speed vector adjusted for headset direction
            Vector3 localHeadsetSpeedVector = transform.InverseTransformDirection(globalHeadsetSpeedVector);

            // calculate animation properties
            _lastD_X = Mathf.Lerp(_lastD_X, Mathf.Clamp(localHeadsetSpeedVector.x, -1, 1), AnimationSmoothing);
            _lastD_Y = Mathf.Lerp(_lastD_Y, Mathf.Clamp(localHeadsetSpeedVector.z, -1, 1), AnimationSmoothing);
            bool isMoving = localHeadsetSpeedVector.magnitude > AnimationActivationSpeedTreshold;

            // set animation properties
            PlayerBodyAnimator.SetBool(IsMovingAnimationStateName, isMoving);
            PlayerBodyAnimator.SetFloat(LeftRightAnimationFloatName, _lastD_X);
            PlayerBodyAnimator.SetFloat(ForwardBackAnimationFloatName, _lastD_Y);

            // set last position for speed calculation next frame
            _lastPos = transform.position;
        }
    }
}