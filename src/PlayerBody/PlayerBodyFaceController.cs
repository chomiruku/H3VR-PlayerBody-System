using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using HarmonyLib;

namespace PlayerBodySystem
{
    public class PlayerBodyFaceController : MonoBehaviour
    {
        public Animator PlayerBodyAnimator;

        [Header("Blink Animation System")]
        public string BlinkAnimationTriggerPropertyName = "Blink";
        [Tooltip("Found these values online for the average time between blinks.\nFeel free to change them.")]
        public Vector2 RandomBlinkIntervalRange = new(2f, 6f);

        [Header("Pain Animation System")]
        public string PainAnimationTriggerPropertyName = "Pain";

        public void Start()
        {
            StartCoroutine(WaitForBlink());
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FVRPlayerBody),nameof(FVRPlayerBody.RegisterPlayerHit))]
        public void FVRPlayerBodyRegisterPlayerHitPatch()
        {
            PlayerBodyAnimator.SetTrigger(PainAnimationTriggerPropertyName);
        }
    }
}