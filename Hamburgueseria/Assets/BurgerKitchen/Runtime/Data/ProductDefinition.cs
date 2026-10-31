using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    public enum ProductMatchMode
    {
        /// <summary>El objeto entregado tiene que ser exactamente <see cref="ProductDefinition.item"/>.</summary>
        SingleItem,
        /// <summary>Un contenedor con unos ingredientes concretos (hamburguesa, tarrina con toppings).</summary>
        Assembly,
    }

    public enum AssemblyOrderRule
    {
        /// <summary>Da igual el orden.</summary>
        AnyOrder,
        /// <summary>El primero y el último tienen que coincidir (pan abajo y pan arriba); el medio en cualquier orden.</summary>
        FirstAndLastFixed,
        /// <summary>El orden exacto de abajo arriba.</summary>
        Exact,
    }

    public enum MenuCourse
    {
        Main,
        Side,
        Drink,
        Dessert,
    }

    /// <summary>
    /// Un producto de la carta: lo que piden los clientes y cómo se reconoce al entregarlo.
    /// </summary>
    [CreateAssetMenu(menuName = "Burger Kitchen/Product (menu item)", fileName = "Product_Nuevo")]
    public class ProductDefinition : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        public MenuCourse course = MenuCourse.Main;
        [Min(0)] public int price = 5;
        [Tooltip("Segundos de paciencia extra que da al cliente (productos lentos de preparar).")]
        public float extraPatience = 10f;

        [Header("Reconocimiento")]
        public ProductMatchMode matchMode = ProductMatchMode.SingleItem;
        [Tooltip("SingleItem: el objeto exacto. Assembly: el contenedor base (vacío = cualquiera).")]
        public ItemDefinition item;
        [Tooltip("Assembly: ingredientes de abajo arriba.")]
        public List<ItemDefinition> ingredients = new List<ItemDefinition>();
        public AssemblyOrderRule orderRule = AssemblyOrderRule.FirstAndLastFixed;
        [Tooltip("Acepta ingredientes de más.")]
        public bool allowExtras;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public bool Matches(ItemSnapshot snapshot) => ProductMatcher.Matches(this, snapshot);
    }
}
