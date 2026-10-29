# Freecam — local SPT 5.0 / EFT 48146 port

Branch `spt-5.0-48146` in [James's fork](https://github.com/jpeckham/SPT-Freecam/tree/spt-5.0-48146) ports acidphantasm's upstream commit `fa45a86d48aa6b9311f9a255b03a82a84be291ba`. No existing 5.0 port was found in the upstream branch/PR list or the other listed fork. Version **2.0.0** identifies this experimental port; it is not an upstream release.

## Build and install this branch

Requires .NET 10 SDK and a working SPT 5.0 / EFT 1.2.5.0.48146 installation with generated IL2CPP interop assemblies. From this repository's root:

```powershell
dotnet build project/Terkoiz.Freecam/Terkoiz.Freecam.csproj -c Release `
  '-p:SPTPath=C:\sptarkov\SPTarkov 5.0.0-local'
```

Close the client/server, back up any previous plugin/config, and copy the contents of `project/Terkoiz.Freecam/Build/BepInEx/plugins/Freecam` into the installation's `BepInEx/plugins/Freecam`. Include `LICENSE`; remove duplicate older Freecam DLLs elsewhere. Builds stage locally and do not automatically install. No game/reference assemblies are distributed. To uninstall, close the game and remove `Terkoiz.Freecam.dll` from that folder.

The port uses BepInEx 6 IL2CPP `BasePlugin`, native component registration and constructors, the IL2CPP KeyboardShortcut type, and the SPTushonka patch namespace. It accesses the native player-body/health-controller properties directly instead of managed field reflection/injection. The fall prefix returns zero when suppressing the native float-returning method and targets only the local player. Additional fixes guard incomplete initialization, preserve detached-camera state when controls are paused, reset raid state, restore input/UI on component teardown, and use Euler yaw for teleport rotation instead of a quaternion component. Original license and author credit are retained.

## Controls and validation

### 2.0.1: camera leash correction

James's first gameplay test found that numpad + followed by W repeatedly snapped the camera back. The original 2.0.0 startup harness only tested the reset-patch guard; it did not exercise camera movement. Inspection of the 48146 native camera code showed that `UpdatePointOfView` dispatches to the active first/third-person state, while `LateUpdate` and fixed/update loops continue driving that state. The old body-POV trick did not transfer camera ownership to Freecam, and guarding `CameraManager.ForceSetPosition` alone was insufficient.

Version **2.0.1** keeps the body in third person and suspends `PlayerCameraController` while detached. Freecam's separate component handles movement. Pausing camera controls keeps the native controller suspended; exit/teleport and component teardown restore its prior enabled state. The original native patch remains as an additional guard.

The added in-client regression test failed before this change and passed afterward using a native `PlayerCameraController`: suspend, repeated suspend, restore, and preservation of an already-disabled controller. Build passed with 0 warnings/errors. **The original in-raid leash reproduction still needs a user retest**; this result establishes the ownership handoff, not a completed gameplay validation.

- Numpad **+**: toggle freecam.
- Numpad **.**: toggle camera/player controls while detached.
- Numpad **Enter**: teleport to camera and exit freecam.
- Numpad **\***: toggle UI.
- WASD/arrows: move; Shift: faster; Q/E and R/F (or Page Up/Down): height; mouse: look; wheel: move forward/back.

F12 exposes settings in `com.terkoiz.freecam.cfg`. Defaults retain smart fall prevention on, global fall-damage suppression off, and remembered camera position off.

Build: **0 warnings, 0 errors**. Client startup registered both native components and enabled `FreecamPatch`, `FallDamagePatch`, and `ForceSetCameraPositionPatch`. A temporary in-client harness passed native component creation, the pre-raid update guard, and normal/free/paused camera-reset gating. The harness was removed after testing. Final formatting/license-staging cleanup was rebuilt after that smoke test without behavioral changes.

To repeat the harness, build `project/Terkoiz.Freecam/tests/RuntimeTests.csproj` with the same SPTPath, temporarily install its `Freecam.RuntimeTests.dll` alongside the production plugin, and launch a disposable test profile. Check `BepInEx/LogOutput.log` for `Freecam Runtime Tests` and `PASS:`; close the game and remove the test DLL afterward. This is an in-client harness, not a `dotnet test` project.

**Still unverified in a raid:** camera attachment/movement, paused-control transitions, teleport placement, fall prevention, UI hiding, and cleanup between raids. Use a test profile for those checks. Startup and helper checks alone do not establish gameplay compatibility; upstream's distant-LOD limitation may remain.

## Original upstream documentation (historical Mono build instructions)

A BepInEx plugin for SPT-AKI that allows you to detach the camera and fly around freely in Escape From Tarkov.

### Controls

The default controls are as follows:
- Keypad Plus - toggle free camera mode
- Keypad Enter - teleport player to camera position
- Keypad Multiply - toggle UI

If you need to change them, press F12 in-game and expand the `Freecam #.#.#` section and configure your keybinds there.
Alternatively, you can find the `com.terkoiz.freecam.cfg` file in your `BepInEx/config/` folder after you've started up the game at least once with Freecam installed, and change the keybinds there.


### How to install

1. Download the latest release here: [link](https://dev.sp-tarkov.com/Terkoiz/Freecam/releases) -OR- build from source (instructions below)
2. Simply extract the zip file contents into your root SPT-AKI folder (where EscapeFromTarkov.exe is).
3. Your `BepInEx/plugins` folder should now contain a `Terkoiz.Freecam.dll` file inside.

### Known issues

1. When teleporting to camera position, the camera rotation gets copied exactly, potentially causing the player model to tilt
2. Game version UI element is not hidden when toggling UI
3. When flying to distant parts of the map in freecam mode, LODs are not triggered (these seem to follow the player)

### How to build from source

1. Download/clone this repository
2. Open your current SPT directory and copy all files from `\EscapeFromTarkov_Data\Managed` into this solution's `\References\EFT_Managed` folder.
3. Rebuild the project in the Release configuration.
4. Grab the `Terkoiz.Freecam.dll` file from the `bin/Release` folder and use it wherever. Refer to the "How to install" section if you need help here.
