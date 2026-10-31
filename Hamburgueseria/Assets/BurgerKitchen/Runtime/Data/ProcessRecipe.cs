using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Transformación de un objeto en una estación: entrada → salida en un tiempo.
    /// Se encadenan solas: carne cruda → hecha (receta 1) → quemada (receta 2, isOvercook).
    /// Varias recetas con la misma entrada en una estación = el jugador elige (sabores de batido).
    /// </summary>
    [CreateAssetMenu(menuName = "Burger Kitchen/Process Recipe", fileName = "Recipe_Nueva")]
    public class ProcessRecipe : ScriptableObject
    {
        [Tooltip("Verbo que se muestra al jugador: Cocinar, Freír, Cortar, Batir, Servir...")]
        public string actionLabel = "Cocinar";
        public ItemDefinition input;
        public ItemDefinition output;
        [Min(0.05f)] public float duration = 5f;
        [Tooltip("El resultado es un estado estropeado (se muestra en rojo como aviso).")]
        public bool isOvercook;
        [Tooltip("Si es mayor que 0, el proceso se para en ese punto hasta que el jugador le dé la vuelta.")]
        [Range(0f, 0.95f)] public float flipAt;
        public string flipLabel = "Dar la vuelta";

        public string OptionLabel => output != null ? output.DisplayName : name;
    }
}
