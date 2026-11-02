using UnityEngine;

namespace Mecanicas.Cintas.Demo
{
    /// <summary>
    /// Jugador en primera persona mínimo (CharacterController) SOLO para probar la demo.
    /// Sirve para comprobar que la cinta también arrastra al jugador. En tu juego usa tu
    /// propio controlador: si usa CharacterController o Rigidbody, la cinta lo moverá igual.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DemoJugadorPrimeraPersona : MonoBehaviour
    {
        public float velocidad = 4f;
        public float velocidadCorrer = 7f;
        public float salto = 1.2f;
        public float gravedad = -20f;
        public float sensibilidad = 2f;
        public Transform camara;

        CharacterController cc;
        float pitch;
        float velocidadVertical;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (camara == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) camara = cam.transform;
            }
        }

        void Start() => Bloquear(true);

        void Update()
        {
            if (DemoEntrada.Pulsada(KeyCode.Escape)) Bloquear(false);
            if (DemoEntrada.ClicIzquierdo()) Bloquear(true);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 raton = DemoEntrada.Raton() * sensibilidad;
                transform.Rotate(0f, raton.x, 0f, Space.Self);
                pitch = Mathf.Clamp(pitch - raton.y, -85f, 85f);
                if (camara != null) camara.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector3 entrada = Vector3.zero;
            if (DemoEntrada.Mantenida(KeyCode.W)) entrada.z += 1f;
            if (DemoEntrada.Mantenida(KeyCode.S)) entrada.z -= 1f;
            if (DemoEntrada.Mantenida(KeyCode.D)) entrada.x += 1f;
            if (DemoEntrada.Mantenida(KeyCode.A)) entrada.x -= 1f;
            entrada = Vector3.ClampMagnitude(entrada, 1f);
            float v = DemoEntrada.Mantenida(KeyCode.LeftShift) ? velocidadCorrer : velocidad;

            if (cc.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f;
            if (cc.isGrounded && DemoEntrada.Pulsada(KeyCode.Space)) velocidadVertical = Mathf.Sqrt(salto * -2f * gravedad);
            velocidadVertical += gravedad * Time.deltaTime;

            Vector3 mov = transform.TransformDirection(entrada) * v + Vector3.up * velocidadVertical;
            cc.Move(mov * Time.deltaTime);
        }

        static void Bloquear(bool si)
        {
            Cursor.lockState = si ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !si;
        }
    }
}
