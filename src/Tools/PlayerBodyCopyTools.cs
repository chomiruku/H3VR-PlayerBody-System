using RootMotion.FinalIK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PlayerBodySystem
{
    /// <summary>
    /// Helper script to help with copying the player body system to another humanoid rig and some other helpful features.
    /// The methods are all in the custom editor that belongs to this class (PlayerBodyCopyToolsEditor).
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
    }
}