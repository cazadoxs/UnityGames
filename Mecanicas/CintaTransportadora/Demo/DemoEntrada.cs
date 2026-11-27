using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Cintas.Demo
{
    /// <summary>
    /// Lectura de teclado y ratón SOLO para la demo. Funciona tanto con el Input System
    /// nuevo como con el Input Manager antiguo. En tu proyecto usa tu propio sistema de
    /// controles: la cinta no depende de esto.
    /// </summary>
    public static class DemoEntrada
    {
        public static bool Mantenida(KeyCode k)
        {
#if ENABLE_INPUT_SYSTEM
            var t = Keyboard.current;
            if (t == null) return false;
            var c = Control(t, k);
            return c != null && c.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(k);
#else
            return false;
#endif
        }

        public static bool Pulsada(KeyCode k)
        {
#if ENABLE_INPUT_SYSTEM
            var t = Keyboard.current;
            if (t == null) return false;
            var c = Control(t, k);
            return c != null && c.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(k);
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

        /// <summary>Movimiento del ratón en este frame (escala parecida a la del Input Manager antiguo).</summary>
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

#if ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl Control(Keyboard t, KeyCode k)
        {
            switch (k)
            {
                case KeyCode.W: return t.wKey;
                case KeyCode.A: return t.aKey;
                case KeyCode.S: return t.sKey;
                case KeyCode.D: return t.dKey;
                case KeyCode.F: return t.fKey;
                case KeyCode.R: return t.rKey;
                case KeyCode.Z: return t.zKey;
                case KeyCode.X: return t.xKey;
                case KeyCode.G: return t.gKey;
                case KeyCode.LeftShift: return t.leftShiftKey;
                case KeyCode.Space: return t.spaceKey;
                case KeyCode.Escape: return t.escapeKey;
                default: return null;
            }
        }
#endif
    }
}
