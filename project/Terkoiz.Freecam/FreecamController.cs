using Comfort.Common;
using EFT;
using EFT.CameraControl;
using EFT.UI;

using Terkoiz.Freecam.Patches;
using UnityEngine;

namespace Terkoiz.Freecam
{
    public class FreecamController : MonoBehaviour
    {
        public FreecamController(System.IntPtr pointer) : base(pointer) { }
        private GameObject _mainCamera;
        private Freecam _freeCamScript;
        private PlayerCameraController _playerCameraController;
        private bool _cameraSuspended;
        private bool _cameraWasEnabled;

        private EftBattleUIScreen _playerUi;
        private bool _uiHidden;

        private GamePlayerOwner _gamePlayerOwner;

        private Vector3? _lastPosition;
        private Quaternion? _lastRotation;

        private bool _controlsToggled;
        public bool IsFreecamActive => (_freeCamScript != null && _freeCamScript.IsActive) || _controlsToggled;

        public void Start()
        {
            var player = GetLocalPlayerFromWorld();
            if (player == null) return;
            // Get Main Camera
            _playerCameraController = player.GetComponent<PlayerCameraController>();
            _mainCamera = _playerCameraController?.Camera?.gameObject;
            if (_mainCamera == null)
            {
                FreecamPlugin.Logger.LogError("Failed to locate main camera");
                return;
            }

            // Get Player UI
            _playerUi = Singleton<CommonUI>.Instance?.EftBattleUIScreen;
            if (_playerUi == null)
            {
                FreecamPlugin.Logger.LogError("Failed to locate player UI");
                return;
            }

            // Add Freecam script to main camera in scene
            _freeCamScript = _mainCamera.AddComponent<Freecam>();
            if (_freeCamScript == null)
            {
                FreecamPlugin.Logger.LogError("Failed to add Freecam script to Camera");
            }

            // Get GamePlayerOwner component
            _gamePlayerOwner = GetLocalPlayerFromWorld().GetComponentInChildren<GamePlayerOwner>();
            if (_gamePlayerOwner == null)
            {
                FreecamPlugin.Logger.LogError("Failed to locate GamePlayerOwner");
            }
        }

        public void Update()
        {
            if (_mainCamera == null || _freeCamScript == null || _playerUi == null || _gamePlayerOwner == null)
                return;
            if (FreecamPlugin.ToggleUi.Value.IsDown())
            {
                ToggleUi();
            }

            if (FreecamPlugin.ToggleFreecamMode.Value.IsDown())
            {
                ToggleCamera();
            }

            if (FreecamPlugin.ToggleFreecamControls.Value.IsDown())
            {
                ToggleCameraControls();
            }

            if (FreecamPlugin.TeleportToCamera.Value.IsDown())
            {
                MovePlayerToCamera();
            }
        }

        /// <summary>
        /// Toggles the Freecam mode
        /// </summary>
        private void ToggleCamera()
        {
            // Get our own Player instance. Null means we're not in a raid
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null)
                return;

            if (!IsFreecamActive)
            {
                SetPlayerToFreecamMode(localPlayer);
            }
            else
            {
                SetPlayerToFirstPersonMode(localPlayer);
            }
        }

        /// <summary>
        /// When triggered during Freecam mode, teleports the player to where the camera was and exits Freecam mode
        /// </summary>
        private void MovePlayerToCamera()
        {
            var localPlayer = GetLocalPlayerFromWorld();
            if (localPlayer == null)
                return;

            // Move the player to the camera's current position and switch to First Person mode
            if (IsFreecamActive)
            {
                // Tell the fall damage patch that we just teleported. Used for the "smart" fall damage prevention feature
                FallDamagePatch.HasTeleported = true;

                // We grab the camera's position, but we subtract a bit off the Y axis, because the players coordinate origin is at the feet
                var position = new Vector3(_mainCamera.transform.position.x, _mainCamera.transform.position.y - 1.8f, _mainCamera.transform.position.z);
                localPlayer.gameObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0, _mainCamera.transform.eulerAngles.y, 0));

                // localPlayer.gameObject.transform.SetPositionAndRotation(position, _mainCamera.transform.rotation);
                SetPlayerToFirstPersonMode(localPlayer);
            }
        }

        /// <summary>
        /// Hides the main UI (health, stamina, stance, hotbar, etc.)
        /// </summary>
        private void ToggleUi()
        {
            // Check if we're currently in a raid
            if (GetLocalPlayerFromWorld() == null)
                return;

            _playerUi.gameObject.SetActive(_uiHidden);
            _uiHidden = !_uiHidden;
        }

        /// <summary>
        /// A helper method to set the Player into Freecam mode
        /// </summary>
        /// <param name="localPlayer"></param>
        private void SetPlayerToFreecamMode(Player localPlayer)
        {
            // Keep the player's body visible while our separate component owns camera movement.
            localPlayer.PointOfView = EPointOfView.ThirdPerson;
            // EFT 48146 still runs the first/third-person camera state for FreeCamera POV.
            // Suspend all native camera update loops, including their inlined position writes.
            SuspendPlayerCamera();

            // Instead of Detouring, just turn off _gamePlayerOwner which takes the input
            _gamePlayerOwner.enabled = false;

            if (FreecamPlugin.CameraRememberLastPosition.Value && _lastPosition != null && _lastRotation != null)
            {
                _mainCamera.transform.position = _lastPosition.Value;
                _mainCamera.transform.rotation = _lastRotation.Value;
            }

            _freeCamScript.IsActive = true;
        }

        /// <summary>
        /// A helper method to reset the player view back to First Person
        /// </summary>
        /// <param name="localPlayer"></param>
        private void SetPlayerToFirstPersonMode(Player localPlayer)
        {
            _freeCamScript.IsActive = false;
            _controlsToggled = false;

            if (FreecamPlugin.CameraRememberLastPosition.Value)
            {
                _lastPosition = _mainCamera.transform.position;
                _lastRotation = _mainCamera.transform.rotation;
            }

            // re-enable _gamePlayerOwner
            _gamePlayerOwner.enabled = true;

            localPlayer.PointOfView = EPointOfView.FirstPerson;
            RestorePlayerCamera();
        }

        private void SuspendPlayerCamera()
        {
            if (_cameraSuspended || _playerCameraController == null) return;
            _cameraWasEnabled = _playerCameraController.enabled;
            _playerCameraController.enabled = false;
            _cameraSuspended = true;
        }

        private void RestorePlayerCamera()
        {
            if (!_cameraSuspended) return;
            if (_playerCameraController != null) _playerCameraController.enabled = _cameraWasEnabled;
            _cameraSuspended = false;
        }

        /// <summary>
        /// A helper method to toggle the Freecam Camera Controls
        /// </summary>
        private void ToggleCameraControls()
        {
            if (!IsFreecamActive) return;
            if (_freeCamScript.IsActive)
            {
                _controlsToggled = true;
                _freeCamScript.IsActive = false;
                _gamePlayerOwner.enabled = true;
            }
            else
            {
                _controlsToggled = false;
                _freeCamScript.IsActive = true;
                _gamePlayerOwner.enabled = false;
            }
        }

        /// <summary>
        /// Gets the current <see cref="Player"/> instance if it's available
        /// </summary>
        /// <returns>Local <see cref="Player"/> instance; returns null if the game is not in raid</returns>
        private Player GetLocalPlayerFromWorld()
        {
            // If the GameWorld instance is null or has no RegisteredPlayers, it most likely means we're not in a raid
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null || gameWorld.MainPlayer == null)
                return null;

            // One of the RegisteredPlayers will have the IsYourPlayer flag set, which will be our own Player instance
            return gameWorld.MainPlayer;
        }

        public void OnDestroy()
        {
            RestorePlayerCamera();
            if (_gamePlayerOwner != null) _gamePlayerOwner.enabled = true;
            if (_playerUi != null && _uiHidden) _playerUi.gameObject.SetActive(true);
            FreecamPlugin.FreecamControllerInstance = null;
            FallDamagePatch.HasTeleported = false;
            // Destroy FreeCamScript before FreeCamController if exists
            Destroy(_freeCamScript);
        }
    }
}
