using UnityEngine;

namespace Mecanicas.Construccion.Demo
{
    /// <summary>
    /// Controlador en primera persona mínimo para la escena de demostración (WASD, ratón, Mayús para
    /// correr, Espacio para saltar). En tu proyecto usa tu propio controlador: el sistema de
    /// construcción solo necesita una cámara.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DemoFirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float runSpeed = 8f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private bool lockCursor = true;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            Vector2 look = InputCompat.MouseDelta() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            _pitch = Mathf.Clamp(_pitch - look.y, -85f, 85f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            float x = (InputCompat.GetKey(KeyCode.D) ? 1f : 0f) - (InputCompat.GetKey(KeyCode.A) ? 1f : 0f);
            float z = (InputCompat.GetKey(KeyCode.W) ? 1f : 0f) - (InputCompat.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 move = transform.right * x + transform.forward * z;
            if (move.sqrMagnitude > 1f) move.Normalize();
            float speed = InputCompat.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -2f;
                if (InputCompat.GetKeyDown(KeyCode.Space)) _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _verticalVelocity += gravity * Time.deltaTime;

            _controller.Move((move * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            if (InputCompat.GetKeyDown(KeyCode.F1))
            {
                bool locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
        }
    }
}
