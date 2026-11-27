using UnityEngine;

namespace Mecanicas.Inventario.Demo
{
    /// <summary>Muestra los controles de la demo en pantalla.</summary>
    [AddComponentMenu("Mecánicas/Inventario/Demo/Ayuda de controles (demo)")]
    public class DemoHelp : MonoBehaviour
    {
        [TextArea(4, 12)]
        [SerializeField] private string text =
            "WASD mover · Ratón mirar · Espacio saltar · Mayús correr\n" +
            "E recoger · Tab / I abrir inventario · 1-6 o rueda: barra rápida\n" +
            "Clic izq. usar lo de la mano · G tirar 1 (Mayús+G toda la pila)\n" +
            "F5 guardar · F9 cargar · H ocultar esta ayuda";
        [SerializeField] private KeyCode hideKey = KeyCode.H;
        private bool visible = true;

        private void Update()
        {
            if (InventoryInput.GetKeyDown(hideKey)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible) return;
            var style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, wordWrap = true };
            style.padding = new RectOffset(10, 10, 8, 8);
            GUI.Box(new Rect(Screen.width - 470, 20, 450, 96), text, style);
        }
    }
}
