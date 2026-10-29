using System.Reflection;
using EFT;
using SPTushonka.Reflection.Patching;
using EFT.HealthSystem;
using HarmonyLib;

namespace Terkoiz.Freecam.Patches
{
    public class FallDamagePatch : ModulePatch
    {
        internal static bool HasTeleported;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.HandleFall));
        }

        [PatchPrefix]
        public static bool PatchPrefix(ActiveHealthController __instance, float height, ref float __result)
        {
            // WARNING: The 'HandleFall' method gets called every frame for every player and AI in a raid. Be very careful with logging or expensive operations in this prefix patch!

            // Check if it's our own player
            if (__instance.Player == null || !__instance.Player.IsYourPlayer)
            {
                return true; // Run original method
            }

            // Global fall damage flag overrides everything
            if (FreecamPlugin.GlobalDisableFallDamage.Value)
            {
                __result = 0f;
                return false; // Prevent original method from running
            }

            // If smart fall damage flag is enabled, check if we've recently teleported and if the fall height value was positive
            if (FreecamPlugin.SmartDisableFallDamage.Value && HasTeleported && height > 0)
            {
                HasTeleported = false;
                __result = 0f;
                return false; // Prevent original method from running
            }

            return true; // Run original method
        }
    }
}
