using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using HarmonyLib;
using H3MP;
using static PlayerBodySystem.PlayerBodyHandController;
using OpenScripts2;

namespace PlayerBodySystem
{
    /// <summary>
    /// I never tested this one, but it was supposed to be for automatic blinking and facial animations for getting hit.
    /// </summary>
    public class PlayerBodyFaceController : MonoBehaviour
    {
        public Animator PlayerBodyAnimator;

        [Header("Blink Animation System")]
        public string BlinkAnimationTriggerPropertyName = "Blink";
        [Tooltip("Found these values online for the average time between blinks.\nFeel free to change them.")]
        public Vector2 RandomBlinkIntervalRange = new(2f, 6f);

        [Header("Pain Animation System")]
        public string PainAnimationTriggerPropertyName = "Pain";

        private static PlayerBodyFaceController PlayerInstance;

        public void Start()
        {
            if (OpenScripts2_BasePlugin.IsInEditor) return;

            StartCoroutine(WaitForBlink());

            FVRPlayerBody currentPlayerBody = GM.CurrentPlayerBody;
            //FVRMovementManager movementManager = GM.CurrentMovementManager;
            if (Mod.managerObject == null) // H3MP not connected
            {
                PlayerInstance = this;
            }
            else // H3MP connected, must check whether this body is ours
            {
                if (GameManager.currentPlayerBody == GetComponentInParent<FVRPlayerBody>()) // Body is ours
                {
                    PlayerInstance = this;
                }
                else // Not ours, destroy this
                {
                    Destroy(this);
                }
            }
        }

        private IEnumerator WaitForBlink()
        {
            while (true)
            {
                float blinkWaitTime = UnityEngine.Random.Range(RandomBlinkIntervalRange.x, RandomBlinkIntervalRange.y);

                yield return new WaitForSeconds(blinkWaitTime);

                PlayerBodyAnimator.SetTrigger(BlinkAnimationTriggerPropertyName);
            }
        }

        // Patch that hooks into the player getting hit to act as an event to play the hit animation.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FVRPlayerBody), nameof(FVRPlayerBody.RegisterPlayerHit))]
        public static void FVRPlayerBodyRegisterPlayerHitPatch()
        {
            PlayerInstance.PlayerBodyAnimator.SetTrigger(PlayerInstance.PainAnimationTriggerPropertyName);
        }
    }
}