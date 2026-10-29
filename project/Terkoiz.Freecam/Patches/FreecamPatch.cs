using System.Reflection;
using SPTushonka.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;

namespace Terkoiz.Freecam.Patches
{
    public class FreecamPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
        }

        [PatchPostfix]
        public static void PatchPostFix()
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (gameWorld == null)
                return;

            FallDamagePatch.HasTeleported = false;
            // Add FreecamController to GameWorld GameObject
            FreecamPlugin.FreecamControllerInstance = gameWorld.gameObject.GetComponent<FreecamController>() ?? gameWorld.gameObject.AddComponent<FreecamController>();
        }
    }
}
