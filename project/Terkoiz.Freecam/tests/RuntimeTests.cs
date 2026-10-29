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
            Log.LogInfo("PASS: native component creation, pre-raid update guard, normal/free/paused camera reset gating");
        }
        catch (Exception ex) { Log.LogError("FAIL: " + ex); }
        finally
        {
            FreecamPlugin.FreecamControllerInstance = null;
            if (test != null) UnityEngine.Object.Destroy(test);
        }
    }
}
