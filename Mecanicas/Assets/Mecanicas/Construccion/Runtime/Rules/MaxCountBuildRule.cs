using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Regla de ejemplo: limita cuántos objetos de un tipo puede haber construidos a la vez.
    /// Ponla en el mismo objeto que el BuildSystem. Sirve de plantilla para tus propias reglas.
    /// </summary>
    [AddComponentMenu("Mecánicas/Construcción/Reglas/Límite de cantidad")]
    public class MaxCountBuildRule : BuildRule
    {
        [Tooltip("Objeto limitado. Si está vacío, el límite cuenta todos los objetos construidos.")]
        [SerializeField] private BuildableDefinition definition;
        [Min(0)] [SerializeField] private int maxCount = 3;

        public override bool IsAllowed(in PlacementCandidate candidate)
        {
            if (definition != null && candidate.Definition != definition) return true;
            int count = PlacedBuildable.Count(definition);
            return count < maxCount;
        }
    }
}
