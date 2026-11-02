using UnityEngine;

namespace Mecanicas.Tienda.Demo
{
    /// <summary>Jugador en primera persona mínimo, solo para la demo. En tu juego usa tu propio controlador.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class JugadorDemo : MonoBehaviour
    {
        public Transform camara;
        public float velocidad = 4.5f;
        public float sensibilidad = 2f;
        public float gravedad = -20f;

        CharacterController cc;
        float pitch;
        float velocidadVertical;

        void Awake() { cc = GetComponent<CharacterController>(); }

        void Start() { Bloquear(true); }

        void Update()
        {
            if (TiendaMenu.AlgunaAbierta) return;

            if (Cursor.lockState != CursorLockMode.Locked && EntradaTienda.ClicIzquierdo()) Bloquear(true);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 r = EntradaTienda.Raton() * sensibilidad;
                transform.Rotate(0, r.x, 0);
                pitch = Mathf.Clamp(pitch - r.y, -85f, 85f);
                if (camara != null) camara.localRotation = Quaternion.Euler(pitch, 0, 0);
            }

            Vector2 m = EntradaTienda.Movimiento();
            Vector3 mov = (transform.right * m.x + transform.forward * m.y) * velocidad;
            if (cc.isGrounded && velocidadVertical < 0) velocidadVertical = -1f;
            velocidadVertical += gravedad * Time.deltaTime;
            mov.y = velocidadVertical;
            cc.Move(mov * Time.deltaTime);
        }

        static void Bloquear(bool si)
        {
            Cursor.lockState = si ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !si;
        }
    }
}
