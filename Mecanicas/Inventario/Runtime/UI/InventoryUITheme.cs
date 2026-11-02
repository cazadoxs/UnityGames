using System;
using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>Colores, tamaños y textos de la interfaz. Cámbialos en el Inspector del InventoryUI.</summary>
    [Serializable]
    public class InventoryUITheme
    {
        [Header("Rejilla")]
        [Min(1)] public int columns = 6;
        [Min(16)] public float slotSize = 72f;
        [Min(0)] public float slotSpacing = 6f;
        [Min(0)] public float panelPadding = 16f;

        [Header("Colores")]
        public Color panelColor = new Color(0.08f, 0.09f, 0.11f, 0.92f);
        public Color slotColor = new Color(1f, 1f, 1f, 0.08f);
        public Color hotbarSlotColor = new Color(1f, 0.85f, 0.4f, 0.12f);
        public Color slotHoverColor = new Color(1f, 1f, 1f, 0.22f);
        public Color selectedFrameColor = new Color(1f, 0.8f, 0.25f, 1f);
        public Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public Color secondaryTextColor = new Color(0.7f, 0.72f, 0.76f, 1f);
        public Color tooltipColor = new Color(0.03f, 0.03f, 0.04f, 0.96f);
        public Color buttonColor = new Color(1f, 1f, 1f, 0.12f);
        public Color warningColor = new Color(1f, 0.45f, 0.35f, 1f);
        public Color crosshairColor = new Color(1f, 1f, 1f, 0.8f);

        [Header("Texto")]
        [Tooltip("Fuente. Vacío = fuente integrada de Unity.")]
        public Font font;
        [Min(8)] public int fontSize = 18;
        public string title = "Inventario";
        public string sortButtonText = "Ordenar";
        public string weightFormat = "Peso: {0:0.#} / {1:0.#}";
        public string helpText = "Arrastra para mover · Ctrl+arrastrar: 1 unidad · Clic der.: usar · Mayús+clic: mover rápido · Rueda: dividir · Suelta fuera para tirar";

        [Header("Fondos opcionales (sprites 9-slice)")]
        public Sprite panelSprite;
        public Sprite slotSprite;
    }
}
