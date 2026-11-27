using UnityEngine;
#if MECANICAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Punto único de lectura de teclado y ratón para todo el asset.
    /// Funciona con el Input System nuevo (si el paquete está instalado y activo)
    /// y con el Input Manager clásico. Para cambiar las teclas, edita solo este archivo.
    /// </summary>
    public static class Entrada
    {
        // Factor para que el delta del ratón del Input System (píxeles) se parezca al eje clásico "Mouse X".
        const float EscalaRatonInputSystem = 0.1f;

#if MECANICAS_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        static Keyboard K => Keyboard.current;
        static Mouse M => Mouse.current;

        /// <summary>Se pulsó este frame la acción de interactuar (E o clic izquierdo).</summary>
        public static bool InteractuarPulsado =>
            (K != null && K.eKey.wasPressedThisFrame) || (M != null && M.leftButton.wasPressedThisFrame);

        /// <summary>La acción de interactuar se mantiene pulsada.</summary>
        public static bool InteractuarMantenido =>
            (K != null && K.eKey.isPressed) || (M != null && M.leftButton.isPressed);

        /// <summary>Movimiento del ratón este frame, en las unidades del eje clásico "Mouse X/Y".</summary>
        public static Vector2 DeltaRaton => M != null ? M.delta.ReadValue() * EscalaRatonInputSystem : Vector2.zero;

        /// <summary>Rueda del ratón: positivo hacia arriba.</summary>
        public static float Rueda => M != null ? Mathf.Sign(M.scroll.ReadValue().y) * (Mathf.Abs(M.scroll.ReadValue().y) > 0.01f ? 1f : 0f) : 0f;

        /// <summary>WASD / flechas. x = lateral, y = adelante.</summary>
        public static Vector2 Movimiento
        {
            get
            {
                if (K == null) return Vector2.zero;
                float x = (K.dKey.isPressed || K.rightArrowKey.isPressed ? 1f : 0f) - (K.aKey.isPressed || K.leftArrowKey.isPressed ? 1f : 0f);
                float y = (K.wKey.isPressed || K.upArrowKey.isPressed ? 1f : 0f) - (K.sKey.isPressed || K.downArrowKey.isPressed ? 1f : 0f);
                return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
            }
        }

        public static bool Saltar => K != null && K.spaceKey.wasPressedThisFrame;
        public static bool Correr => K != null && K.leftShiftKey.isPressed;
        public static bool Escape => K != null && K.escapeKey.wasPressedThisFrame;
        public static bool ClicIzquierdoPulsado => M != null && M.leftButton.wasPressedThisFrame;
#else
        public static bool InteractuarPulsado => Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
        public static bool InteractuarMantenido => Input.GetKey(KeyCode.E) || Input.GetMouseButton(0);
        public static Vector2 DeltaRaton => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        public static float Rueda
        {
            get
            {
                float y = Input.mouseScrollDelta.y;
                return Mathf.Abs(y) > 0.01f ? Mathf.Sign(y) : 0f;
            }
        }
        public static Vector2 Movimiento => Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
        public static bool Saltar => Input.GetKeyDown(KeyCode.Space);
        public static bool Correr => Input.GetKey(KeyCode.LeftShift);
        public static bool Escape => Input.GetKeyDown(KeyCode.Escape);
        public static bool ClicIzquierdoPulsado => Input.GetMouseButtonDown(0);
#endif

        /// <summary>Texto de la tecla de interacción que se enseña en pantalla.</summary>
        public static string NombreTeclaInteractuar = "E";
    }
}
