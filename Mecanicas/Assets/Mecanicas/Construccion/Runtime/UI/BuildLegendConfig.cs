using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Todo lo que se puede personalizar de la leyenda que aparece mientras se construye: textos,
    /// colores, posición, tamaño de letra y qué líneas se ven en cada situación. Se crea con
    /// Assets &gt; Create &gt; Mecánicas &gt; Construcción &gt; Configuración de leyenda.
    /// </summary>
    [CreateAssetMenu(fileName = "LeyendaConstruccion", menuName = "Mecánicas/Construcción/Configuración de leyenda", order = 1)]
    public class BuildLegendConfig : ScriptableObject
    {
        public enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        /// <summary>Cuándo se muestra una línea.</summary>
        public enum Condition
        {
            Always,
            Placing,
            GridMode,
            FreeMode,
            Demolishing,
            WhenValid,
            WhenInvalid
        }

        /// <summary>Qué cuadradito de color lleva una línea a la izquierda.</summary>
        public enum SwatchMode
        {
            None,
            /// <summary>Un color fijo (el de <see cref="LegendEntry.swatchColor"/>).</summary>
            Fixed,
            /// <summary>Verde si se puede construir, rojo si no (colores del BuildSystem).</summary>
            PlacementState,
            /// <summary>Siempre el color de "se puede" del BuildSystem.</summary>
            ValidColor,
            /// <summary>Siempre el color de "no se puede" del BuildSystem.</summary>
            InvalidColor
        }

        [Serializable]
        public class LegendEntry
        {
            [Tooltip("Texto de la línea. Admite las etiquetas de la lista de abajo, como {objeto} o {tecla:colocar}.")]
            [TextArea(1, 3)] public string text = "Nueva línea";
            public Condition showWhen = Condition.Always;
            public SwatchMode swatch = SwatchMode.None;
            public Color swatchColor = Color.white;
            [Tooltip("Icono opcional a la izquierda.")]
            public Sprite icon;
        }

        [Header("Título")]
        public string title = "MODO CONSTRUCCIÓN";
        public bool showTitle = true;

        [Header("Líneas")]
        public List<LegendEntry> entries = new List<LegendEntry>();

        [Header("Textos")]
        public string gridModeName = "Cuadrícula";
        public string freeModeName = "Libre";
        public string demolishModeName = "Demoler";
        public string validText = "Se puede construir";
        public string invalidText = "No se puede construir";
        [Tooltip("Formato del coste. {0} es el número.")]
        public string costFormat = "{0} monedas";
        public string freeCostText = "Gratis";
        [Tooltip("Formato de cada objeto del catálogo en {catalogo}. {0} = número, {1} = nombre.")]
        public string catalogItemFormat = "{0} {1}";
        [Tooltip("Formato del objeto elegido en {catalogo}.")]
        public string catalogSelectedFormat = "<b>[{0} {1}]</b>";
        public string catalogSeparator = "  ";
        public string demolishTargetNone = "Apunta a algo construido";

        [Header("Motivos por los que no se puede construir ({motivo})")]
        public string reasonNoSurface = "Apunta al suelo";
        public string reasonOutsideGrid = "Fuera de la cuadrícula";
        public string reasonCellsOccupied = "Casilla ocupada";
        public string reasonOverlapping = "Choca con otro objeto";
        public string reasonTooSteep = "Suelo demasiado inclinado";
        public string reasonTooFar = "Demasiado lejos";
        public string reasonOutsideZone = "Aquí no se puede construir";
        public string reasonCannotAfford = "No tienes suficiente";
        public string reasonModeNotAllowed = "Este objeto no admite este modo";

        [Header("Aspecto")]
        public Corner corner = Corner.TopLeft;
        [Tooltip("Separación desde la esquina, en píxeles (a 1920x1080).")]
        public Vector2 margin = new Vector2(24f, 24f);
        [Min(100f)] public float width = 380f;
        [Tooltip("Fuente opcional. Si está vacía se usa la fuente por defecto de Unity.")]
        public Font font;
        [Min(8)] public int titleFontSize = 22;
        [Min(8)] public int fontSize = 17;
        public Color titleColor = new Color(1f, 0.85f, 0.35f, 1f);
        public Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public Color backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        [Tooltip("Imagen de fondo opcional (por ejemplo, un marco con 9-slice).")]
        public Sprite backgroundSprite;
        [Tooltip("Relleno interior: izquierda, derecha, arriba, abajo.")]
        public Vector4 padding = new Vector4(14f, 14f, 12f, 12f);
        [Min(0f)] public float lineSpacing = 4f;
        [Min(4f)] public float swatchSize = 14f;
        [Tooltip("Orden de dibujo del Canvas que se crea automáticamente.")]
        public int sortingOrder = 100;

        public string GetReasonText(PlacementResult result, string ruleMessage)
        {
            switch (result)
            {
                case PlacementResult.Valid: return string.Empty;
                case PlacementResult.NoSurface: return reasonNoSurface;
                case PlacementResult.OutsideGrid: return reasonOutsideGrid;
                case PlacementResult.CellsOccupied: return reasonCellsOccupied;
                case PlacementResult.Overlapping: return reasonOverlapping;
                case PlacementResult.TooSteep: return reasonTooSteep;
                case PlacementResult.TooFar: return reasonTooFar;
                case PlacementResult.OutsideBuildZone: return reasonOutsideZone;
                case PlacementResult.CannotAfford: return reasonCannotAfford;
                case PlacementResult.ModeNotAllowed: return reasonModeNotAllowed;
                case PlacementResult.CustomRule: return ruleMessage ?? string.Empty;
                default: return result.ToString();
            }
        }

        /// <summary>Rellena la configuración con las líneas de ejemplo.</summary>
        public void ResetToDefaults()
        {
            entries = CreateDefaultEntries();
        }

        private void Reset()
        {
            ResetToDefaults();
        }

        /// <summary>Configuración por defecto en memoria (se usa si la leyenda no tiene ninguna asignada).</summary>
        public static BuildLegendConfig CreateDefault()
        {
            var c = CreateInstance<BuildLegendConfig>();
            c.name = "Leyenda por defecto";
            c.ResetToDefaults();
            return c;
        }

        public static List<LegendEntry> CreateDefaultEntries()
        {
            return new List<LegendEntry>
            {
                new LegendEntry { text = "Objeto: <b>{objeto}</b>  ({coste})", showWhen = Condition.Placing },
                new LegendEntry { text = "{catalogo}", showWhen = Condition.Placing },
                new LegendEntry { text = "Modo: {modo}", showWhen = Condition.Always },
                new LegendEntry { text = "{estado}", showWhen = Condition.WhenValid, swatch = SwatchMode.PlacementState },
                new LegendEntry { text = "{estado}: {motivo}", showWhen = Condition.WhenInvalid, swatch = SwatchMode.PlacementState },
                new LegendEntry { text = "Verde = se puede construir", showWhen = Condition.Placing, swatch = SwatchMode.ValidColor },
                new LegendEntry { text = "Rojo = no se puede construir", showWhen = Condition.Placing, swatch = SwatchMode.InvalidColor },
                new LegendEntry { text = "Demoler: {objetivo}", showWhen = Condition.Demolishing, swatch = SwatchMode.InvalidColor },
                new LegendEntry { text = "[{tecla:colocar}] Colocar   [{tecla:cancelar}] Salir", showWhen = Condition.Placing },
                new LegendEntry { text = "[{tecla:rotarIzq}] / [{tecla:rotarDer}] o rueda: girar ({rotacion}°)", showWhen = Condition.Placing },
                new LegendEntry { text = "[{tecla:cambiarModo}] Cuadrícula / Libre", showWhen = Condition.Placing },
                new LegendEntry { text = "[1-9] o [{tecla:siguiente}] Cambiar objeto", showWhen = Condition.Placing },
                new LegendEntry { text = "[{tecla:demoler}] Modo demoler", showWhen = Condition.Placing },
                new LegendEntry { text = "[{tecla:colocar}] Demoler   [{tecla:demoler}] Volver", showWhen = Condition.Demolishing },
                new LegendEntry { text = "[{tecla:leyenda}] Ocultar leyenda", showWhen = Condition.Always }
            };
        }
    }
}
