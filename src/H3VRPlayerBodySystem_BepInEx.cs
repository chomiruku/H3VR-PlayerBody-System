using BepInEx;
using HarmonyLib;

namespace PlayerBodySystem
{
    [BepInPlugin("h3vr.cityrobo.H3VRPlayerBodySystem", "H3VR PlayerBody System", "1.0.0")]
    public class H3VRPlayerBodySystem_BepInEx : BaseUnityPlugin
    {
        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(PlayerBodyFaceController));
        }
#if !DEBUG

#endif
    }
}