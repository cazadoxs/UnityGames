using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>Datos de una posición candidata, para las reglas personalizadas.</summary>
    public struct PlacementCandidate
    {
        public BuildableDefinition Definition;
        public PlacementMode Mode;
        public Vector3 Position;
        public Quaternion Rotation;
        /// <summary>Solo en modo cuadrícula.</summary>
        public BuildGrid Grid;
        /// <summary>Solo en modo cuadrícula.</summary>
        public CellArea Area;
        /// <summary>Colisionador del suelo apuntado (solo en modo libre).</summary>
        public Collider Surface;
    }

    /// <summary>
    /// Regla extra de colocación. Hereda de esta clase, ponla en el mismo objeto que el BuildSystem
    /// (o en sus hijos) y se comprobará después de las reglas normales. Ejemplos: "solo junto a una
    /// carretera", "máximo 3 torres", "solo de día".
    /// </summary>
    public abstract class BuildRule : MonoBehaviour
    {
        [Tooltip("Texto que se muestra en la leyenda cuando esta regla impide construir.")]
        [SerializeField] private string failMessage = "No se cumple una condición";

        public string FailMessage => failMessage;

        /// <summary>Devuelve true si se puede construir.</summary>
        public abstract bool IsAllowed(in PlacementCandidate candidate);
    }
}
