using UnityEngine;

namespace Terkoiz.Freecam
{
    /// <summary>
    /// A simple free camera to be added to a Unity game object.
    ///
    /// Full credit to Ashley Davis on GitHub for the initial code:
    /// https://gist.github.com/ashleydavis/f025c03a9221bc840a2b
    ///
    /// </summary>
    public class Freecam : MonoBehaviour
    {
        public Freecam(System.IntPtr pointer) : base(pointer) { }
        public bool IsActive = false;
        private bool _ownsCamera;
        private Vector3 _position;
        private Quaternion _rotation;

        public void AcquireCamera()
        {
            if (_ownsCamera) return;
            _position = transform.position;
            _rotation = transform.rotation;
            _ownsCamera = true;
        }

        public void ReleaseCamera()
        {
            RestorePose();
            _ownsCamera = false;
        }

        private void RestorePose()
        {
            if (_ownsCamera) transform.SetPositionAndRotation(_position, _rotation);
        }

        public void LateUpdate() => RestorePose();
        public void OnPreCull() => RestorePose();

        public void Update()
        {
            // Native/manual camera writers and parent movement must never become
            // the origin of the next free-camera movement. Pausing input still owns pose.
            RestorePose();
            if (!IsActive)
            {
                return;
            }

            var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var movementSpeed = fastMode ? FreecamPlugin.CameraFastMoveSpeed.Value : FreecamPlugin.CameraMoveSpeed.Value;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                transform.position += (-transform.right * (movementSpeed * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                transform.position += (transform.right * (movementSpeed * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                transform.position += (transform.forward * (movementSpeed * Time.deltaTime));
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                transform.position += (-transform.forward * (movementSpeed * Time.deltaTime));
            }

            if (FreecamPlugin.CameraHeightMovement.Value)
            {
                if (Input.GetKey(KeyCode.Q))
                {
                    transform.position += (transform.up * (movementSpeed * Time.deltaTime));
                }

                if (Input.GetKey(KeyCode.E))
                {
                    transform.position += (-transform.up * (movementSpeed * Time.deltaTime));
                }

                if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
                {
                    transform.position += (Vector3.up * (movementSpeed * Time.deltaTime));
                }

                if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
                {
                    transform.position += (-Vector3.up * (movementSpeed * Time.deltaTime));
                }
            }

            float newRotationX = transform.eulerAngles.y + Input.GetAxis("Mouse X") * FreecamPlugin.CameraLookSensitivity.Value;
            float newRotationY = transform.eulerAngles.x - Input.GetAxis("Mouse Y") * FreecamPlugin.CameraLookSensitivity.Value;
            transform.eulerAngles = new Vector3(newRotationY, newRotationX, 0f);

            if (FreecamPlugin.CameraMousewheelZoom.Value)
            {
                float axis = Input.GetAxis("Mouse ScrollWheel");
                if (axis != 0)
                {
                    var zoomSensitivity = fastMode ? FreecamPlugin.CameraFastZoomSpeed.Value : FreecamPlugin.CameraZoomSpeed.Value;
                    transform.position += transform.forward * (axis * zoomSensitivity);
                }
            }
            if (_ownsCamera)
            {
                _position = transform.position;
                _rotation = transform.rotation;
            }
        }

    }
}
