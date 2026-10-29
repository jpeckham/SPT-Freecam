using System.Reflection;
using EFT.CameraControl;
using HarmonyLib;
using SPTushonka.Reflection.Patching;

namespace Terkoiz.Freecam.Patches
{
    /// <summary>
    /// Due to some hacky fix by BSG to prevent camera desync with relation to the BTR, a check is ran in <see cref="EFT.CameraControl.PlayerCameraController.LateUpdate"/>
    /// to forcefully reset the camera position if it's too far away from the player's body. This interferes with freecam and therefore needs to be disabled while in Freecam mode
    /// </summary>
    public class ForceSetCameraPositionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(CameraManager), nameof(CameraManager.ForceSetPosition));
        }

        [PatchPrefix]
        public static bool PatchPrefix()
        {
            if (FreecamPlugin.FreecamControllerInstance == null)
            {
                return true; // Run original method
            }

            // If freecam is active, then return false to not let the method run when called. Otherwise, the method will run without any interference
            return !FreecamPlugin.FreecamControllerInstance.IsFreecamActive;
        }
    }
}
