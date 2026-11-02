using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Eventos.Demo
{
    /// <summary>
    /// Controlador de primera persona mínimo, solo para probar la demo. En tu juego usa el tuyo:
    /// basta con que respete <see cref="BloqueoControlJugador.Bloqueado"/> y, si quieres la opción
    /// "Mantener" de la cámara, que implemente <see cref="IControlCamaraJugador"/> (mira cómo se hace aquí).
    /// Funciona con el Input System nuevo y con el Input Manager antiguo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Mecánicas/Eventos/Demo/Jugador demo primera persona")]
    public class JugadorDemoPrimeraPersona : MonoBehaviour, IControlCamaraJugador
    {
        [SerializeField] private Transform camara;
        [SerializeField] private float velocidad = 4.5f;
        [SerializeField] private float sensibilidad = 0.12f;
        [SerializeField] private float limiteVertical = 85f;
        [SerializeField] private float gravedad = -20f;

        private CharacterController cc;
        private float giroX; // arriba/abajo, en la cámara
        private float giroY; // izquierda/derecha, en el cuerpo
        private float velocidadY;

        private void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
            giroY = transform.eulerAngles.y;
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (TeclaEscape())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (ClicIzquierdo())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Gravedad siempre, para no quedarse flotando durante un evento.
            velocidadY = cc.isGrounded ? -1f : velocidadY + gravedad * Time.deltaTime;

            Vector3 mov = Vector3.zero;
            if (!BloqueoControlJugador.Bloqueado)
            {
                Vector2 raton = Raton() * sensibilidad;
                giroY += raton.x;
                giroX = Mathf.Clamp(giroX - raton.y, -limiteVertical, limiteVertical);
                transform.rotation = Quaternion.Euler(0f, giroY, 0f);
                if (camara != null) camara.localRotation = Quaternion.Euler(giroX, 0f, 0f);

                Vector2 ejes = Movimiento();
                mov = (transform.right * ejes.x + transform.forward * ejes.y) * velocidad;
            }

            mov.y = velocidadY;
            cc.Move(mov * Time.deltaTime);
        }

        /// <summary>La cámara de eventos nos pasa hacia dónde quedó mirando.</summary>
        public void AplicarRotacionMundo(Quaternion rotacionCamara)
        {
            Vector3 e = rotacionCamara.eulerAngles;
            giroY = e.y;
            giroX = Mathf.Clamp(Mathf.DeltaAngle(0f, e.x), -limiteVertical, limiteVertical);
            transform.rotation = Quaternion.Euler(0f, giroY, 0f);
            if (camara != null) camara.localRotation = Quaternion.Euler(giroX, 0f, 0f);
        }

#if ENABLE_INPUT_SYSTEM
        private static Vector2 Raton() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        private static Vector2 Movimiento()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        private static bool TeclaEscape() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        private static bool ClicIzquierdo() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        // El Input Manager antiguo da el ratón ya escalado: lo pasamos a "píxeles" aproximados.
        private static Vector2 Raton() => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
        private static Vector2 Movimiento() =>
            Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        private static bool TeclaEscape() => Input.GetKeyDown(KeyCode.Escape);
        private static bool ClicIzquierdo() => Input.GetMouseButtonDown(0);
#endif
    }
}
