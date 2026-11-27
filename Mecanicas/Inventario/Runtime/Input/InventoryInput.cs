using UnityEngine;
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Lectura de teclado y ratón que funciona tanto con el Input System nuevo
    /// como con el Input Manager antiguo (Project Settings > Player > Active Input Handling).
    /// Las teclas se configuran con KeyCode en los componentes y aquí se traducen.
    /// Si quieres usar tus propias acciones (mando, rebind...), sustituye estas funciones.
    /// </summary>
    public static class InventoryInput
    {
        public static bool GetKeyDown(KeyCode key)
        {
            if (key == KeyCode.None) return false;
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = MouseButton(key);
            if (mouse != null) return mouse.wasPressedThisFrame;
            var k = ToKey(key);
            return k != null && k.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool GetKey(KeyCode key)
        {
            if (key == KeyCode.None) return false;
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var mouse = MouseButton(key);
            if (mouse != null) return mouse.isPressed;
            var k = ToKey(key);
            return k != null && k.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        /// <summary>Rueda del ratón: positivo hacia arriba, negativo hacia abajo (aprox. 1 por "clic").</summary>
        public static float ScrollDelta()
        {
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#else
            return 0f;
#endif
        }

        /// <summary>Movimiento del ratón en este frame (en píxeles aproximados).</summary>
        public static Vector2 MouseDelta()
        {
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.1f : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }

        /// <summary>Posición del ratón en pantalla (píxeles).</summary>
        public static Vector2 MousePosition()
        {
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }

        /// <summary>WASD / flechas como vector (-1..1).</summary>
        public static Vector2 MoveAxis()
        {
            float x = 0f, y = 0f;
            if (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow)) x += 1f;
            if (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow)) y -= 1f;
            if (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow)) y += 1f;
            var v = new Vector2(x, y);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        public static bool ShiftHeld => GetKey(KeyCode.LeftShift) || GetKey(KeyCode.RightShift);
        public static bool CtrlHeld => GetKey(KeyCode.LeftControl) || GetKey(KeyCode.RightControl);

        /// <summary>Devuelve 0..8 si se ha pulsado 1..9 este frame, o -1.</summary>
        public static int NumberKeyDown()
        {
            for (int i = 0; i < 9; i++)
                if (GetKeyDown(KeyCode.Alpha1 + i) || GetKeyDown(KeyCode.Keypad1 + i)) return i;
            return -1;
        }

#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        private static UnityEngine.InputSystem.Controls.ButtonControl MouseButton(KeyCode key)
        {
            var m = Mouse.current;
            if (m == null) return null;
            switch (key)
            {
                case KeyCode.Mouse0: return m.leftButton;
                case KeyCode.Mouse1: return m.rightButton;
                case KeyCode.Mouse2: return m.middleButton;
                case KeyCode.Mouse3: return m.backButton;
                case KeyCode.Mouse4: return m.forwardButton;
                default: return null;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<KeyCode, Key> cache =
            new System.Collections.Generic.Dictionary<KeyCode, Key>();

        private static UnityEngine.InputSystem.Controls.KeyControl ToKey(KeyCode code)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            if (!cache.TryGetValue(code, out var key))
            {
                key = Translate(code);
                cache[code] = key;
            }
            return key == Key.None ? null : kb[key];
        }

        private static Key Translate(KeyCode code)
        {
            if (code == KeyCode.Alpha0) return Key.Digit0; // en el Input System Digit0 va después de Digit9
            if (code >= KeyCode.Alpha1 && code <= KeyCode.Alpha9) return Key.Digit1 + (code - KeyCode.Alpha1);
            if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9) return Key.Numpad0 + (code - KeyCode.Keypad0);
            switch (code)
            {
                case KeyCode.Return: return Key.Enter;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.BackQuote: return Key.Backquote;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Minus: return Key.Minus;
                case KeyCode.Plus: return Key.Equals;
            }
            return System.Enum.TryParse(code.ToString(), true, out Key k) ? k : Key.None;
        }
#endif
    }
}
