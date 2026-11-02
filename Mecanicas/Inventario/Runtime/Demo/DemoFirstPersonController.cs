using UnityEngine;

namespace Mecanicas.Inventario.Demo
{
    /// <summary>
    /// Controlador de primera persona MUY simple, solo para probar la demo.
    /// En tu juego usa tu propio controlador: lo único que necesita el inventario
    /// es que no gires la cámara mientras InventoryUI.IsAnyOpen sea true.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Mecánicas/Inventario/Demo/Controlador primera persona (demo)")]
    public class DemoFirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxPitch = 85f;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
        }

        private void Update()
        {
            bool menuOpen = InventoryUI.IsAnyOpen;

            if (!menuOpen && cameraPivot != null)
            {
                Vector2 look = InventoryInput.MouseDelta() * mouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -maxPitch, maxPitch);
                cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }

            Vector2 move = menuOpen ? Vector2.zero : InventoryInput.MoveAxis();
            float speed = InventoryInput.ShiftHeld ? runSpeed : walkSpeed;
            Vector3 dir = (transform.right * move.x + transform.forward * move.y) * speed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (!menuOpen && InventoryInput.GetKeyDown(KeyCode.Space))
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;
            dir.y = verticalVelocity;
            controller.Move(dir * Time.deltaTime);
        }
    }
}
