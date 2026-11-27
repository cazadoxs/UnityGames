using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Tienda
{
    /// <summary>Teclas que puede usar el sistema (funciona con el Input System nuevo y con el Input Manager clásico).</summary>
    public enum TeclaTienda { Ninguna, E, F, Q, R, B, I, M, U, Tab, Escape, Enter, Espacio }

    /// <summary>
    /// Capa fina de entrada para que el asset funcione tanto con el Input System nuevo como con el clásico,
    /// sin que tengas que configurar nada. Si tu juego tiene su propio sistema de entrada,
    /// desactiva las teclas en los componentes y llama tú a Interactuar()/Abrir().
    /// </summary>
    public static class EntradaTienda
    {
        public static bool Pulsada(TeclaTienda tecla)
        {
            if (tecla == TeclaTienda.Ninguna) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            var control = k[AKey(tecla)];
            return control != null && control.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(AKeyCode(tecla));
#else
            return false;
#endif
        }

        public static bool Mantenida(TeclaTienda tecla)
        {
            if (tecla == TeclaTienda.Ninguna) return false;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            var control = k[AKey(tecla)];
            return control != null && control.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(AKeyCode(tecla));
#else
            return false;
#endif
        }

        public static bool ClicIzquierdo()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        /// <summary>Movimiento del ratón, más o menos en la misma escala en los dos sistemas.</summary>
        public static Vector2 Raton()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }

        /// <summary>WASD / flechas como vector (x = lateral, y = adelante).</summary>
        public static Vector2 Movimiento()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
            return Vector2.zero;
#endif
        }

        public static string Nombre(TeclaTienda tecla)
        {
            switch (tecla)
            {
                case TeclaTienda.Escape: return "Esc";
                case TeclaTienda.Espacio: return "Espacio";
                case TeclaTienda.Ninguna: return "";
                default: return tecla.ToString();
            }
        }

#if ENABLE_INPUT_SYSTEM
        static Key AKey(TeclaTienda t)
        {
            switch (t)
            {
                case TeclaTienda.E: return Key.E;
                case TeclaTienda.F: return Key.F;
                case TeclaTienda.Q: return Key.Q;
                case TeclaTienda.R: return Key.R;
                case TeclaTienda.B: return Key.B;
                case TeclaTienda.I: return Key.I;
                case TeclaTienda.M: return Key.M;
                case TeclaTienda.U: return Key.U;
                case TeclaTienda.Tab: return Key.Tab;
                case TeclaTienda.Escape: return Key.Escape;
                case TeclaTienda.Enter: return Key.Enter;
                case TeclaTienda.Espacio: return Key.Space;
                default: return Key.None;
            }
        }
#endif

        static KeyCode AKeyCode(TeclaTienda t)
        {
            switch (t)
            {
                case TeclaTienda.E: return KeyCode.E;
                case TeclaTienda.F: return KeyCode.F;
                case TeclaTienda.Q: return KeyCode.Q;
                case TeclaTienda.R: return KeyCode.R;
                case TeclaTienda.B: return KeyCode.B;
                case TeclaTienda.I: return KeyCode.I;
                case TeclaTienda.M: return KeyCode.M;
                case TeclaTienda.U: return KeyCode.U;
                case TeclaTienda.Tab: return KeyCode.Tab;
                case TeclaTienda.Escape: return KeyCode.Escape;
                case TeclaTienda.Enter: return KeyCode.Return;
                case TeclaTienda.Espacio: return KeyCode.Space;
                default: return KeyCode.None;
            }
        }
    }
}
