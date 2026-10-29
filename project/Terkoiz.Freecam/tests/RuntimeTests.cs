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
        GameObject parent = null;
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
            var acquire = typeof(Freecam).GetMethod("AcquireCamera");
            var release = typeof(Freecam).GetMethod("ReleaseCamera");
            var late = typeof(Freecam).GetMethod("LateUpdate");
            var render = typeof(Freecam).GetMethod("OnPreCull");
            if (acquire == null || release == null || late == null || render == null)
                throw new Exception("Missing independent free-camera pose ownership");
            var expectedPosition = new Vector3(50, 20, 70);
            var expectedRotation = Quaternion.Euler(12, 75, 0);
            test.transform.SetPositionAndRotation(expectedPosition, expectedRotation);
            acquire.Invoke(camera, null);
            parent = new GameObject("Freecam moving parent test");
            test.transform.SetParent(parent.transform, true);
            // A native/manual camera writer can run even when its component is disabled.
            for (var i = 0; i < 20; i++)
            {
                parent.transform.SetPositionAndRotation(new Vector3(i, i, i), Quaternion.Euler(0, i * 5, 0));
                test.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                camera.Update(); // paused controls must retain pose ownership
                if (Vector3.Distance(test.transform.position, expectedPosition) > .001f)
                    throw new Exception("External camera reset contaminated paused position");
                test.transform.position = Vector3.one;
                late.Invoke(camera, null);
                test.transform.rotation = Quaternion.identity;
                render.Invoke(camera, null);
                if (Vector3.Distance(test.transform.position, expectedPosition) > .001f || Quaternion.Angle(test.transform.rotation, expectedRotation) > .01f)
                    throw new Exception("External camera writes survived late/render pose restoration");
            }
            release.Invoke(camera, null);
            test.transform.position = Vector3.zero;
            late.Invoke(camera, null);
            if (test.transform.position != Vector3.zero) throw new Exception("Released camera still overrides native position");
            Log.LogInfo("PASS: independent world pose survives 20 native resets and parent moves; paused/late/render restore and release; native camera suspend/restore and reset gating");
        }
        catch (Exception ex) { Log.LogError("FAIL: " + ex); }
        finally
        {
            FreecamPlugin.FreecamControllerInstance = null;
            if (test != null) UnityEngine.Object.Destroy(test);
            if (parent != null) UnityEngine.Object.Destroy(parent);
        }
    }
}
