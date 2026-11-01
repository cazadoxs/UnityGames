using UnityEngine;
#if ENABLE_INPUT_SYSTEM && MIRADA_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.MiradaNPC.Demo
{
    /// <summary>
    /// Jugador en primera persona MÍNIMO, solo para probar la demo (WASD, ratón, Shift, Espacio, C para agacharse).
    /// En tu proyecto usa tu propio controlador: la mecánica de mirada no depende de este script.
    /// Funciona con el Input System nuevo (si está instalado) o con el Input Manager clásico.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Mecánicas/Mirada NPC/Demo/Jugador Demo (primera persona)")]
    public class JugadorDemoMirada : MonoBehaviour
    {
        public Transform camara;
        public float velocidad = 3.5f;
        public float velocidadCorrer = 6f;
        public float sensibilidad = 0.12f;
        public float salto = 5f;
        public float gravedad = -20f;
        public float alturaOjos = 1.6f;
        public float alturaOjosAgachado = 0.9f;

        CharacterController cc;
        float pitch, velVertical;
        bool cursorBloqueado = true;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (camara == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) camara = cam.transform;
            }
            BloquearCursor(true);
        }

        void Update()
        {
            LeerEntrada(out Vector2 mover, out Vector2 mirar, out bool correr, out bool saltar, out bool agachar, out bool escape, out bool clic);

            if (escape) BloquearCursor(false);
            else if (clic && !cursorBloqueado) BloquearCursor(true);

            if (cursorBloqueado)
            {
                transform.Rotate(0f, mirar.x * sensibilidad, 0f);
                pitch = Mathf.Clamp(pitch - mirar.y * sensibilidad, -85f, 85f);
            }

            float dt = Time.deltaTime;
            if (camara != null)
            {
                camara.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                Vector3 lp = camara.localPosition;
                lp.y = Mathf.MoveTowards(lp.y, agachar ? alturaOjosAgachado : alturaOjos, 4f * dt);
                camara.localPosition = lp;
            }

            Vector3 plano = (transform.right * mover.x + transform.forward * mover.y) * (correr ? velocidadCorrer : velocidad);
            if (cc.isGrounded)
            {
                velVertical = -1f;
                if (saltar) velVertical = salto;
            }
            velVertical += gravedad * dt;
            cc.Move((plano + Vector3.up * velVertical) * dt);
        }

        void BloquearCursor(bool bloquear)
        {
            cursorBloqueado = bloquear;
            Cursor.lockState = bloquear ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !bloquear;
        }

        static void LeerEntrada(out Vector2 mover, out Vector2 mirar, out bool correr, out bool saltar, out bool agachar, out bool escape, out bool clic)
        {
#if ENABLE_INPUT_SYSTEM && MIRADA_INPUT_SYSTEM
            var k = Keyboard.current;
            var m = Mouse.current;
            mover = Vector2.zero;
            mirar = Vector2.zero;
            correr = saltar = agachar = escape = clic = false;
            if (k != null)
            {
                mover = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                correr = k.leftShiftKey.isPressed;
                saltar = k.spaceKey.wasPressedThisFrame;
                agachar = k.cKey.isPressed || k.leftCtrlKey.isPressed;
                escape = k.escapeKey.wasPressedThisFrame;
            }
            if (m != null)
            {
                mirar = m.delta.ReadValue();
                clic = m.leftButton.wasPressedThisFrame;
            }
            if (mover.sqrMagnitude > 1f) mover.Normalize();
#else
            mover = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            // El Input Manager clásico da el ratón en otra escala; lo igualamos aprox. al delta en píxeles.
            mirar = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 10f;
            correr = Input.GetKey(KeyCode.LeftShift);
            saltar = Input.GetKeyDown(KeyCode.Space);
            agachar = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
            escape = Input.GetKeyDown(KeyCode.Escape);
            clic = Input.GetMouseButtonDown(0);
#endif
        }

        void OnGUI()
        {
            GUI.Label(new Rect(12, 10, 700, 90),
                "DEMO MIRADA NPC — WASD moverse · Ratón mirar · Shift correr · Espacio saltar · C agacharse · Esc soltar ratón\n" +
                "Rodea a cada NPC: cada uno tiene límites y comportamientos distintos (lee su cartel). Sube a la plataforma para probar los límites verticales.");
        }
    }
}
