using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Ficha de un objeto que se puede construir: qué prefab se instancia, cuántas celdas ocupa,
    /// en qué modos se puede colocar y cuánto cuesta. Se crea con
    /// Assets &gt; Create &gt; Mecánicas &gt; Construcción &gt; Objeto construible.
    /// </summary>
    [CreateAssetMenu(fileName = "NuevoConstruible", menuName = "Mecánicas/Construcción/Objeto construible", order = 0)]
    public class BuildableDefinition : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Nombre que se muestra en la leyenda ({objeto}).")]
        public string displayName = "Objeto";

        [TextArea(2, 4)]
        [Tooltip("Descripción opcional ({descripcion} en la leyenda).")]
        public string description;

        [Tooltip("Icono opcional para la leyenda o para tu propia interfaz.")]
        public Sprite icon;

        [Tooltip("Prefab que se instancia al construir. El pivote debería estar en la base del objeto.")]
        public GameObject prefab;

        [Header("Colocación")]
        [Tooltip("En qué modos se puede colocar este objeto.")]
        public AllowedPlacementModes allowedModes = AllowedPlacementModes.Both;

        [Tooltip("Celdas que ocupa en la cuadrícula (X = ancho, Y = fondo), sin girar.")]
        public Vector2Int gridSize = Vector2Int.one;

        [Tooltip("Desplazamiento local aplicado al colocar (por ejemplo, para subirlo un poco).")]
        public Vector3 positionOffset = Vector3.zero;

        [Tooltip("Grados que gira cada pulsación en modo libre. En cuadrícula siempre gira 90°.")]
        [Range(1f, 180f)]
        public float freeRotationStep = 15f;

        [Tooltip("En modo libre, alinea el objeto con la inclinación del suelo.")]
        public bool alignToSurfaceNormal = false;

        [Tooltip("Inclinación máxima del suelo en modo libre, en grados.")]
        [Range(0f, 90f)]
        public float maxSlope = 30f;

        [Header("Colisión")]
        [Tooltip("Si está activo, se usa la caja de abajo para comprobar superposiciones en lugar de calcularla del modelo.")]
        public bool overrideBounds = false;

        [Tooltip("Caja local (centro y tamaño) para comprobar superposiciones.")]
        public Bounds customBounds = new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);

        [Header("Coste")]
        [Tooltip("Coste al construir. Solo se aplica si el BuildSystem tiene un BuildCostProvider.")]
        [Min(0)]
        public int cost = 0;

        [Tooltip("Porcentaje del coste que se devuelve al demoler (0 = nada, 1 = todo).")]
        [Range(0f, 1f)]
        public float refundOnDemolish = 0.5f;

        public bool Allows(PlacementMode mode)
        {
            AllowedPlacementModes flag = mode == PlacementMode.Grid ? AllowedPlacementModes.Grid : AllowedPlacementModes.Free;
            return (allowedModes & flag) != 0;
        }

        public string DisplayNameOrAssetName => string.IsNullOrEmpty(displayName) ? name : displayName;

        private void OnValidate()
        {
            gridSize.x = Mathf.Max(1, gridSize.x);
            gridSize.y = Mathf.Max(1, gridSize.y);
            if (allowedModes == 0) allowedModes = AllowedPlacementModes.Both;
        }
    }
}
