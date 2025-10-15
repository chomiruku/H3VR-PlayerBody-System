using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace PlayerBodySystem
{
    [BepInPlugin("h3vr.chomilk.H3VRPlayerBodySystemFork", "H3VR PlayerBody System", "1.3.0")]
    public class H3VRPlayerBodySystem_BepInEx : BaseUnityPlugin
    {
        [HideInInspector]
        public static bool H3MPLoaded;

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(PlayerBodyFaceController));

            H3MPLoaded = Chainloader.PluginInfos.ContainsKey("VIP.TommySoucy.H3MP");
        }
#if !DEBUG

#endif
    }
}