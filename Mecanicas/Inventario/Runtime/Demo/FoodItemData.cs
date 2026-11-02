using UnityEngine;

namespace Mecanicas.Inventario.Demo
{
    /// <summary>
    /// EJEMPLO de objeto con comportamiento propio: hereda de ItemData y sobrescribe Use().
    /// Copia este patrón para pociones, munición, planos, etc.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Inventario/Ejemplos/Comida", fileName = "NuevaComida", order = 20)]
    public class FoodItemData : ItemData
    {
        [Header("Comida")]
        [Tooltip("Cuánta hambre quita al comerlo.")]
        [SerializeField] private float nutrition = 25f;

        public float Nutrition => nutrition;

        public override bool CanUse(Inventory owner)
        {
            // Solo se puede comer si hay hambre.
            var hunger = owner.GetComponent<DemoHunger>();
            return base.CanUse(owner) && (hunger == null || hunger.Hunger > 0.5f);
        }

        public override bool Use(Inventory owner, int slotIndex)
        {
            var hunger = owner.GetComponent<DemoHunger>();
            if (hunger != null) hunger.Eat(nutrition);
            return true; // true = usado; como ConsumeOnUse está activo se gasta una unidad
        }

        public override string GetTooltipExtra() => "Quita " + nutrition.ToString("0") + " de hambre";

#if UNITY_EDITOR
        public void EditorSetNutrition(float value) => nutrition = value;
#endif
    }
}
