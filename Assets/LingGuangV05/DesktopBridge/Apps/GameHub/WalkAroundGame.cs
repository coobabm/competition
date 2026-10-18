using UnityEngine;
using UnityEngine.InputSystem;

namespace LingGuangV05.Desktop.Apps
{
    /// <summary>
    /// The game center's little 3D walk-around (replaces DreamOS's SimpleCharacterController sample; same GUID and
    /// fields): WASD moves the rigidbody, the mouse turns the body and tilts the camera (inverted).
    /// </summary>
    public class WalkAroundGame : MonoBehaviour
    {
        public Transform cameraTransform;
        public float moveSpeed = 5f;
        public float rotationSpeed = 200f;
        public float cameraSensitivity = 2f;

        Rigidbody body;
        float pitch;

        void Start() => body = GetComponent<Rigidbody>();

        void Update()
        {
            var keys = Keyboard.current;
            float strafe = keys[Key.D].ReadValue() - keys[Key.A].ReadValue();
            float forward = keys[Key.W].ReadValue() - keys[Key.S].ReadValue();
            Vector3 move = (transform.forward * forward + transform.right * strafe) * moveSpeed * Time.deltaTime;
            body.MovePosition(body.position + move);

            Vector2 delta = Mouse.current.delta.ReadValue() * cameraSensitivity * Time.deltaTime;
            pitch = Mathf.Clamp(pitch - delta.y, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(Vector3.up * delta.x);
        }
    }
}
