using UnityEngine;
#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Lectura de teclado y ratón que funciona con el Input System nuevo y con el Input Manager antiguo.
    /// Si el proyecto tiene activo el nuevo, se usa ese; si no, el antiguo.
    /// Las teclas se configuran con <see cref="KeyCode"/> (como en el Input Manager antiguo).
    /// </summary>
    public static class DialogueInput
    {
        public static bool GetKeyDown(KeyCode key)
        {
            if (key == KeyCode.None) return false;
#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6)
            {
                var mouse = Mouse.current;
                if (mouse == null) return false;
                switch (key)
                {
                    case KeyCode.Mouse0: return mouse.leftButton.wasPressedThisFrame;
                    case KeyCode.Mouse1: return mouse.rightButton.wasPressedThisFrame;
                    case KeyCode.Mouse2: return mouse.middleButton.wasPressedThisFrame;
                    default: return false;
                }
            }
            var kb = Keyboard.current;
            if (kb == null) return false;
            var k = ToKey(key);
            return k != Key.None && kb[k].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool AnyDown(KeyCode[] keys)
        {
            if (keys == null) return false;
            foreach (var k in keys) if (GetKeyDown(k)) return true;
            return false;
        }

        /// <summary>Devuelve 0-8 si se ha pulsado 1-9 (fila superior o teclado numérico) este frame; -1 si no.</summary>
        public static int NumberKeyDown()
        {
            for (int i = 0; i < 9; i++)
                if (GetKeyDown(KeyCode.Alpha1 + i) || GetKeyDown(KeyCode.Keypad1 + i)) return i;
            return -1;
        }

#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        static Key ToKey(KeyCode code)
        {
            if (code >= KeyCode.A && code <= KeyCode.Z) return Key.A + (code - KeyCode.A);
            if (code >= KeyCode.Alpha0 && code <= KeyCode.Alpha9)
                return code == KeyCode.Alpha0 ? Key.Digit0 : Key.Digit1 + (code - KeyCode.Alpha1);
            if (code >= KeyCode.Keypad0 && code <= KeyCode.Keypad9)
                return code == KeyCode.Keypad0 ? Key.Numpad0 : Key.Numpad1 + (code - KeyCode.Keypad1);
            if (code >= KeyCode.F1 && code <= KeyCode.F12) return Key.F1 + (code - KeyCode.F1);
            switch (code)
            {
                case KeyCode.Space: return Key.Space;
                case KeyCode.Return: return Key.Enter;
                case KeyCode.KeypadEnter: return Key.NumpadEnter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.LeftShift: return Key.LeftShift;
                case KeyCode.RightShift: return Key.RightShift;
                case KeyCode.LeftControl: return Key.LeftCtrl;
                case KeyCode.RightControl: return Key.RightCtrl;
                case KeyCode.LeftAlt: return Key.LeftAlt;
                case KeyCode.RightAlt: return Key.RightAlt;
                case KeyCode.UpArrow: return Key.UpArrow;
                case KeyCode.DownArrow: return Key.DownArrow;
                case KeyCode.LeftArrow: return Key.LeftArrow;
                case KeyCode.RightArrow: return Key.RightArrow;
                default: return Key.None;
            }
        }
#endif
    }
}
