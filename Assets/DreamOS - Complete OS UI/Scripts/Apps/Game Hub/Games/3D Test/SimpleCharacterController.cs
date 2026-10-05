using UnityEngine;
using UnityEngine.InputSystem;

namespace Michsky.DreamOS.Examples
{
    public class SimpleCharacterController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float moveSpeed = 5f;
        public float rotationSpeed = 200f;
        public float cameraSensitivity = 2f;

        Rigidbody rb;
        float cameraRotationX = 0f;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            // Get input axes using the new input system
            float moveHorizontal = Keyboard.current[Key.D].ReadValue() - Keyboard.current[Key.A].ReadValue();
            float moveVertical = -(Keyboard.current[Key.S].ReadValue() - Keyboard.current[Key.W].ReadValue());

            // Calculate movement
            Vector3 movement = (transform.forward * moveVertical + transform.right * moveHorizontal) * moveSpeed * Time.deltaTime;

            // Apply movement to the rigidbody
            rb.MovePosition(rb.position + movement);

            // Mouse input for camera rotation (inverted)
            float mouseDeltaX = Mouse.current.delta.x.ReadValue() * cameraSensitivity * Time.deltaTime;
            float mouseDeltaY = -Mouse.current.delta.y.ReadValue() * cameraSensitivity * Time.deltaTime;

            cameraRotationX += mouseDeltaY;
            cameraRotationX = Mathf.Clamp(cameraRotationX, -90f, 90f);

            cameraTransform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
            transform.Rotate(Vector3.up * mouseDeltaX);
        }
    }
}