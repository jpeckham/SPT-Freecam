using System;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Terkoiz.Freecam;
using Terkoiz.Freecam.Patches;
using UnityEngine;

[BepInPlugin("com.jpeckham.freecam.tests", "Freecam Runtime Tests", "1.0.0")]
[BepInDependency("com.terkoiz.freecam")]
public class RuntimeTests : BasePlugin
{
    public override void Load()
    {
        GameObject test = null;
        try
        {
            test = new GameObject("Freecam runtime test");
            test.SetActive(false);
            var camera = test.AddComponent<Freecam>();
            var controller = test.AddComponent<FreecamController>();
            controller.Update(); // Must tolerate missing raid initialization.
            if (controller.IsFreecamActive) throw new Exception("Unexpected initial active state");
            typeof(FreecamController).GetField("_freeCamScript", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, camera);
            FreecamPlugin.FreecamControllerInstance = controller;
            if (!ForceSetCameraPositionPatch.PatchPrefix()) throw new Exception("Normal camera blocked");
            camera.IsActive = true;
            if (ForceSetCameraPositionPatch.PatchPrefix()) throw new Exception("Free camera reset allowed");
            camera.IsActive = false;
            typeof(FreecamController).GetField("_controlsToggled", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, true);
            if (!controller.IsFreecamActive || ForceSetCameraPositionPatch.PatchPrefix()) throw new Exception("Paused free camera reset allowed");
            var nativeCamera = test.AddComponent<EFT.CameraControl.PlayerCameraController>();
            var cameraField = typeof(FreecamController).GetField("_playerCameraController", BindingFlags.Instance | BindingFlags.NonPublic);
            var suspend = typeof(FreecamController).GetMethod("SuspendPlayerCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            var restore = typeof(FreecamController).GetMethod("RestorePlayerCamera", BindingFlags.Instance | BindingFlags.NonPublic);
            if (cameraField == null || suspend == null || restore == null) throw new Exception("Missing native camera ownership handoff");
            cameraField.SetValue(controller, nativeCamera);
            nativeCamera.enabled = true;
            suspend.Invoke(controller, null);
            if (nativeCamera.enabled) throw new Exception("Native camera still owns updates while detached");
            suspend.Invoke(controller, null);
            restore.Invoke(controller, null);
            if (!nativeCamera.enabled) throw new Exception("Native camera not restored");
            nativeCamera.enabled = false;
            suspend.Invoke(controller, null);
            restore.Invoke(controller, null);
            if (nativeCamera.enabled) throw new Exception("Previously disabled camera unexpectedly enabled");
            Log.LogInfo("PASS: native camera ownership suspend/restore, component creation, pre-raid guard, normal/free/paused reset gating");
        }
        catch (Exception ex) { Log.LogError("FAIL: " + ex); }
        finally
        {
            FreecamPlugin.FreecamControllerInstance = null;
            if (test != null) UnityEngine.Object.Destroy(test);
        }
    }
}
