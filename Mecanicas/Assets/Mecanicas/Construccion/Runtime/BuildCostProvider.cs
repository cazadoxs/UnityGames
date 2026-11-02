using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>
    /// Puente con tu sistema de dinero o recursos. Hereda de esta clase, ponla en la escena y asígnala
    /// al BuildSystem. Si no hay ninguno, construir es gratis.
    /// </summary>
    public abstract class BuildCostProvider : MonoBehaviour
    {
        /// <summary>¿Hay suficiente para pagar <paramref name="cost"/>?</summary>
        public abstract bool CanAfford(BuildableDefinition definition, int cost);

        /// <summary>Cobra el coste. Se llama justo después de colocar el objeto.</summary>
        public abstract void Spend(BuildableDefinition definition, int cost);

        /// <summary>Devuelve dinero al demoler. Por defecto no hace nada.</summary>
        public virtual void Refund(BuildableDefinition definition, int amount)
        {
        }
    }
}
