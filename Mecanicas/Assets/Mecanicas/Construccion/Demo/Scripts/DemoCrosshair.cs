using UnityEngine;

namespace Mecanicas.Construccion.Demo
{
    /// <summary>Punto de mira simple para la demo (apuntar con el centro de la pantalla).</summary>
    public class DemoCrosshair : MonoBehaviour
    {
        [SerializeField] private float size = 6f;
        [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.8f);

        private void OnGUI()
        {
            Color old = GUI.color;
            GUI.color = color;
            float x = Screen.width * 0.5f - size * 0.5f;
            float y = Screen.height * 0.5f - size * 0.5f;
            GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
