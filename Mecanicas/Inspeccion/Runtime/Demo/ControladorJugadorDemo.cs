using UnityEngine;
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Inspeccion.Demo
{
    /// <summary>
    /// Controlador en primera persona MUY sencillo, solo para la escena de demostración.
    /// En tu juego usa tu propio controlador y ponlo en "Componentes A Bloquear" del agarrador.
    ///
    /// WASD mover · ratón mirar · Shift correr · Espacio saltar · Esc liberar el ratón · clic para recapturarlo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Mecánicas/Inspección/Demo/Controlador Jugador Demo")]
    public class ControladorJugadorDemo : MonoBehaviour
    {
        public Transform camara;
        public float velocidad = 3.5f;
        public float velocidadCorrer = 6f;
        public float fuerzaSalto = 4.5f;
        public float gravedad = -18f;
        [Tooltip("Grados por píxel de ratón.")]
        public float sensibilidad = 0.12f;
        public float limiteVertical = 85f;

        CharacterController _cc;
        float _cabeceo, _velocidadVertical;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
        }

        void OnEnable() => BloquearCursor(true);

        static void BloquearCursor(bool bloquear)
        {
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }

        void Update()
        {
            Vector2 mover = Vector2.zero, mirar = Vector2.zero;
            bool correr = false, saltar = false, escape = false, clic = false;

#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k != null)
            {
                mover = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                correr = k.leftShiftKey.isPressed;
                saltar = k.spaceKey.wasPressedThisFrame;
                escape = k.escapeKey.wasPressedThisFrame;
            }
            if (m != null)
            {
                mirar = m.delta.ReadValue() * sensibilidad;
                clic = m.leftButton.wasPressedThisFrame;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                mover += g.leftStick.ReadValue();
                mirar += g.rightStick.ReadValue() * (150f * Time.deltaTime);
                correr |= g.leftStickButton.isPressed;
                saltar |= g.buttonSouth.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            mover = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            mirar = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * (sensibilidad * 10f);
            correr = Input.GetKey(KeyCode.LeftShift);
            saltar = Input.GetKeyDown(KeyCode.Space);
            escape = Input.GetKeyDown(KeyCode.Escape);
            clic = Input.GetMouseButtonDown(0);
#endif

            if (escape) BloquearCursor(false);
            else if (clic && Cursor.lockState != CursorLockMode.Locked) BloquearCursor(true);
            if (Cursor.lockState != CursorLockMode.Locked) mirar = Vector2.zero;

            transform.Rotate(0f, mirar.x, 0f);
            _cabeceo = Mathf.Clamp(_cabeceo - mirar.y, -limiteVertical, limiteVertical);
            if (camara != null) camara.localRotation = Quaternion.Euler(_cabeceo, 0f, 0f);

            Vector3 direccion = transform.right * mover.x + transform.forward * mover.y;
            direccion = Vector3.ClampMagnitude(direccion, 1f) * (correr ? velocidadCorrer : velocidad);

            if (_cc.isGrounded)
            {
                _velocidadVertical = -1f;
                if (saltar) _velocidadVertical = fuerzaSalto;
            }
            _velocidadVertical += gravedad * Time.deltaTime;
            direccion.y = _velocidadVertical;
            _cc.Move(direccion * Time.deltaTime);
        }
    }
}
