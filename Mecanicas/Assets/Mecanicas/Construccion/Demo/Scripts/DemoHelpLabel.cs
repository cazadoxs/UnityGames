using UnityEngine;

namespace Mecanicas.Construccion.Demo
{
    /// <summary>Texto de ayuda de la demo cuando no se está construyendo.</summary>
    public class DemoHelpLabel : MonoBehaviour
    {
        [SerializeField] private string text = "WASD moverse · Ratón mirar · Mayús correr · Espacio saltar · F1 soltar el ratón\n<b>B</b> entrar en el modo construcción";

        private GUIStyle _style;

        private void OnGUI()
        {
            BuildSystem sys = BuildSystem.Current;
            if (sys != null && sys.IsActive) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true, alignment = TextAnchor.LowerCenter };
                _style.normal.textColor = Color.white;
            }
            GUI.Label(new Rect(0f, Screen.height - 90f, Screen.width, 70f), text, _style);
        }
    }
}
