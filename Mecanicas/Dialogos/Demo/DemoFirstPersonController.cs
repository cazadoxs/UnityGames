using UnityEngine;
#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Dialogos.Demo
{
    /// <summary>
    /// Controlador en primera persona MUY simple, solo para la demo. En tu proyecto usa el tuyo y
    /// arrástralo a "Disable While Talking" del DialogueManager.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DemoFirstPersonController : MonoBehaviour
    {
        public Transform cameraPivot;
        public float speed = 4f;
        public float mouseSensitivity = 2f;
        public float gravity = -20f;

        CharacterController cc;
        float pitch;
        float verticalVelocity;

        void Awake() => cc = GetComponent<CharacterController>();

        void OnEnable() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

        void Update()
        {
            ReadInput(out Vector2 move, out Vector2 look, out bool unlock, out bool relock);
            if (unlock) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            if (relock) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                transform.Rotate(0f, look.x * mouseSensitivity, 0f);
                pitch = Mathf.Clamp(pitch - look.y * mouseSensitivity, -85f, 85f);
                if (cameraPivot) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector3 dir = transform.right * move.x + transform.forward * move.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            verticalVelocity = cc.isGrounded ? -1f : verticalVelocity + gravity * Time.deltaTime;
            cc.Move((dir * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        static void ReadInput(out Vector2 move, out Vector2 look, out bool unlock, out bool relock)
        {
            move = look = Vector2.zero;
            unlock = relock = false;
#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null)
            {
                move.x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                move.y = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
                unlock = kb.escapeKey.wasPressedThisFrame;
            }
            if (mouse != null)
            {
                look = mouse.delta.ReadValue() * 0.05f;
                relock = mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            unlock = Input.GetKeyDown(KeyCode.Escape);
            relock = Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked;
#endif
        }
    }
}
