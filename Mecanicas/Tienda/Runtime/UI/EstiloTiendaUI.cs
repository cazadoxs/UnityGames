using System;
using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>Colores y tamaños de la interfaz que se genera sola. Cámbialos en el inspector de TiendaMenu.</summary>
    [Serializable]
    public class EstiloTiendaUI
    {
        [Tooltip("Vacío = fuente integrada de Unity (LegacyRuntime).")]
        public Font fuente;
        public Vector2 tamanoPanel = new Vector2(920, 640);
        [Min(8)] public int tamanoTexto = 22;
        [Min(40)] public float altoFila = 96;
        public Color colorFondo = new Color(0, 0, 0, 0.55f);
        public Color colorPanel = new Color(0.11f, 0.12f, 0.15f, 0.97f);
        public Color colorFila = new Color(0.18f, 0.19f, 0.23f, 1f);
        public Color colorBoton = new Color(0.22f, 0.62f, 0.33f, 1f);
        public Color colorPestana = new Color(0.25f, 0.27f, 0.32f, 1f);
        public Color colorPestanaActiva = new Color(0.36f, 0.45f, 0.75f, 1f);
        public Color colorTexto = new Color(0.94f, 0.94f, 0.96f, 1f);
        public Color colorTextoSecundario = new Color(0.7f, 0.72f, 0.78f, 1f);
        public Color colorAcento = new Color(1f, 0.82f, 0.3f, 1f);
        public Color colorError = new Color(1f, 0.4f, 0.35f, 1f);
        [Tooltip("Orden del Canvas generado (más alto = encima de tu HUD).")]
        public int ordenCanvas = 100;

        public Font Fuente()
        {
            if (fuente != null) return fuente;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
