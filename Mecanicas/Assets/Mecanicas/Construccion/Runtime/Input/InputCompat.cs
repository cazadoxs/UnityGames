using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Lectura de teclado y ratón que funciona igual con el Input System nuevo y con el Input Manager
    /// antiguo. Las teclas se configuran con <see cref="KeyCode"/> (el desplegable de siempre) y, si el
    /// proyecto usa el Input System, se traducen a su equivalente.
    /// </summary>
    public static class InputCompat
    {
#if ENABLE_INPUT_SYSTEM
        private static readonly Dictionary<KeyCode, Key> KeyMap = new Dictionary<KeyCode, Key>();
        private static readonly HashSet<KeyCode> Unmapped = new HashSet<KeyCode>();
#endif

        public static bool GetKeyDown(KeyCode key)
        {
            if (key == KeyCode.None) return false;
#if ENABLE_INPUT_SYSTEM
            ButtonControl control = GetControl(key);
            return control != null && control.wasPressedThisFrame;
#else
            return Input.GetKeyDown(key);
#endif
        }

        public static bool GetKey(KeyCode key)
        {
            if (key == KeyCode.None) return false;
#if ENABLE_INPUT_SYSTEM
            ButtonControl control = GetControl(key);
            return control != null && control.isPressed;
#else
            return Input.GetKey(key);
#endif
        }

        /// <summary>Giro de la rueda del ratón: positivo hacia arriba, negativo hacia abajo, 0 si nada.</summary>
        public static float ScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse == null) return 0f;
            float y = mouse.scroll.ReadValue().y;
            return Mathf.Abs(y) < 0.01f ? 0f : Mathf.Sign(y);
#else
            float y = Input.mouseScrollDelta.y;
            return Mathf.Abs(y) < 0.01f ? 0f : Mathf.Sign(y);
#endif
        }

        /// <summary>Posición del ratón en píxeles de pantalla.</summary>
        public static Vector2 MousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        /// <summary>Movimiento del ratón en este fotograma (en píxeles aproximados).</summary>
        public static Vector2 MouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null ? mouse.delta.ReadValue() * 0.1f : Vector2.zero;
#else
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif
        }

        /// <summary>Nombre corto y legible de una tecla para la leyenda.</summary>
        public static string KeyDisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.None: return "-";
                case KeyCode.Mouse0: return "Clic izq.";
                case KeyCode.Mouse1: return "Clic der.";
                case KeyCode.Mouse2: return "Clic central";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Return: return "Intro";
                case KeyCode.Space: return "Espacio";
                case KeyCode.Tab: return "Tab";
                case KeyCode.Backspace: return "Retroceso";
                case KeyCode.Delete: return "Supr";
                case KeyCode.LeftShift:
                case KeyCode.RightShift: return "Mayús";
                case KeyCode.LeftControl:
                case KeyCode.RightControl: return "Ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt: return "Alt";
                case KeyCode.UpArrow: return "↑";
                case KeyCode.DownArrow: return "↓";
                case KeyCode.LeftArrow: return "←";
                case KeyCode.RightArrow: return "→";
            }

            string n = key.ToString();
            if (n.StartsWith("Alpha")) return n.Substring(5);
            if (n.StartsWith("Keypad")) return "Num " + n.Substring(6);
            return n;
        }

#if ENABLE_INPUT_SYSTEM
        private static ButtonControl GetControl(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0: return Mouse.current?.leftButton;
                case KeyCode.Mouse1: return Mouse.current?.rightButton;
                case KeyCode.Mouse2: return Mouse.current?.middleButton;
                case KeyCode.Mouse3: return Mouse.current?.backButton;
                case KeyCode.Mouse4: return Mouse.current?.forwardButton;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return null;
            if (!TryMap(key, out Key mapped)) return null;
            return keyboard[mapped];
        }

        private static bool TryMap(KeyCode keyCode, out Key key)
        {
            if (KeyMap.TryGetValue(keyCode, out key)) return true;
            if (Unmapped.Contains(keyCode)) return false;

            string n = keyCode.ToString();
            if (n.StartsWith("Alpha")) n = "Digit" + n.Substring(5);
            else if (n.StartsWith("Keypad"))
            {
                string rest = n.Substring(6);
                switch (rest)
                {
                    case "Period": n = "NumpadPeriod"; break;
                    case "Divide": n = "NumpadDivide"; break;
                    case "Multiply": n = "NumpadMultiply"; break;
                    case "Minus": n = "NumpadMinus"; break;
                    case "Plus": n = "NumpadPlus"; break;
                    case "Enter": n = "NumpadEnter"; break;
                    case "Equals": n = "NumpadEquals"; break;
                    default: n = "Numpad" + rest; break;
                }
            }
            else
            {
                switch (keyCode)
                {
                    case KeyCode.Return: n = "Enter"; break;
                    case KeyCode.LeftControl: n = "LeftCtrl"; break;
                    case KeyCode.RightControl: n = "RightCtrl"; break;
                    case KeyCode.LeftCommand: n = "LeftMeta"; break;
                    case KeyCode.RightCommand: n = "RightMeta"; break;
                    case KeyCode.BackQuote: n = "Backquote"; break;
                    case KeyCode.UpArrow: n = "UpArrow"; break;
                    case KeyCode.DownArrow: n = "DownArrow"; break;
                    case KeyCode.LeftArrow: n = "LeftArrow"; break;
                    case KeyCode.RightArrow: n = "RightArrow"; break;
                }
            }

            if (System.Enum.TryParse(n, out key) && key != Key.None)
            {
                KeyMap[keyCode] = key;
                return true;
            }

            Unmapped.Add(keyCode);
            return false;
        }
#endif
    }
}
