using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Controlador en primera persona mínimo para probar el asset (WASD, ratón, Shift, Espacio, Esc).
    /// En tu juego puedes usar tu propio controlador: solo respeta <see cref="Interactor.CamaraBloqueada"/>
    /// para no girar la cámara mientras el jugador arrastra una puerta, palanca o pestillo.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Demo/Jugador primera persona (demo)")]
    [RequireComponent(typeof(CharacterController))]
    public class JugadorPrimeraPersonaDemo : MonoBehaviour
    {
        [SerializeField] Transform camara;
        [SerializeField] float velocidad = 3.5f;
        [SerializeField] float velocidadCorrer = 6f;
        [SerializeField] float sensibilidad = 2f;
        [SerializeField] float salto = 1.1f;
        [SerializeField] float gravedad = -18f;

        CharacterController cc;
        Interactor interactor;
        float pitch, velY;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (camara == null && Camera.main != null) camara = Camera.main.transform;
            interactor = GetComponentInChildren<Interactor>();
        }

        void OnEnable() => BloquearCursor(true);

        void BloquearCursor(bool b)
        {
            Cursor.lockState = b ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !b;
        }

        void Update()
        {
            if (Entrada.Escape) BloquearCursor(Cursor.lockState != CursorLockMode.Locked);
            if (Cursor.lockState != CursorLockMode.Locked && Entrada.ClicIzquierdoPulsado) BloquearCursor(true);

            bool bloqueada = interactor != null && interactor.CamaraBloqueada;
            if (!bloqueada && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = Entrada.DeltaRaton * sensibilidad;
                transform.Rotate(0f, d.x, 0f);
                pitch = Mathf.Clamp(pitch - d.y, -85f, 85f);
                if (camara != null) camara.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            Vector2 m = Entrada.Movimiento;
            Vector3 mov = (transform.right * m.x + transform.forward * m.y) * (Entrada.Correr ? velocidadCorrer : velocidad);
            if (cc.isGrounded)
            {
                velY = -1f;
                if (Entrada.Saltar) velY = Mathf.Sqrt(salto * -2f * gravedad);
            }
            velY += gravedad * Time.deltaTime;
            mov.y = velY;
            cc.Move(mov * Time.deltaTime);
        }
    }
}
