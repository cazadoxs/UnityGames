using System;
using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    [Serializable]
    public class MenuEntry
    {
        public ProductDefinition product;
        [Min(0f)] public float weight = 1f;
        [Tooltip("Día a partir del cual aparece en los pedidos.")]
        [Min(1)] public int unlockDay = 1;
    }

    /// <summary>La carta: qué se puede pedir, con qué frecuencia y desde qué día.</summary>
    [CreateAssetMenu(menuName = "Burger Kitchen/Menu", fileName = "Menu_Principal")]
    public class MenuDefinition : ScriptableObject
    {
        public List<MenuEntry> entries = new List<MenuEntry>();

        [Header("Composición de los pedidos")]
        [Range(0f, 1f)] public float mainChance = 0.85f;
        [Range(0f, 1f)] public float secondMainChance = 0.15f;
        [Range(0f, 1f)] public float sideChance = 0.55f;
        [Range(0f, 1f)] public float drinkChance = 0.55f;
        [Range(0f, 1f)] public float dessertChance = 0.25f;
        [Min(1)] public int maxItemsPerOrder = 4;
        [Tooltip("Productos extra permitidos por pedido por cada día que pasa (dificultad).")]
        public float extraItemsPerDay = 0.25f;
    }
}
