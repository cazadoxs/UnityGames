using System;
using UnityEngine;

namespace BurgerKitchen
{
    public enum ContainerLayout
    {
        /// <summary>Apila el contenido hacia arriba (hamburguesas, toppings).</summary>
        Stack,
        /// <summary>Coloca el contenido en una cuadrícula (bandejas).</summary>
        Grid,
    }

    [Serializable]
    public class ContainerSettings
    {
        public bool isContainer;
        [Tooltip("Etiquetas que acepta. Basta con que el objeto tenga una de ellas.")]
        public ItemTags acceptedTags;
        [Min(1)] public int capacity = 8;
        public ContainerLayout layout = ContainerLayout.Stack;
        [Tooltip("Posición local del primer objeto (encima del contenedor).")]
        public Vector3 contentOffset = new Vector3(0f, 0.02f, 0f);
        [Tooltip("Solo para Grid: separación entre huecos.")]
        public float gridSpacing = 0.22f;
        [Min(1)] public int gridColumns = 2;
    }

    /// <summary>
    /// Definición de datos de cualquier objeto que se puede coger: ingredientes, envases,
    /// productos terminados, bandejas... Un objeto cambia de definición al procesarse
    /// (carne cruda → carne hecha → carne quemada).
    /// </summary>
    [CreateAssetMenu(menuName = "Burger Kitchen/Item Definition", fileName = "Item_Nuevo")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identidad")]
        public string displayName;
        public Sprite icon;
        public ItemTags tags;

        [Header("Visual")]
        [Tooltip("Modelo del objeto (sin colisionador ni Rigidbody; el pivote abajo). Si está vacío se usa una primitiva de color.")]
        public GameObject visualPrefab;
        public PrimitiveType placeholderShape = PrimitiveType.Cube;
        public Color placeholderColor = Color.white;
        [Tooltip("Tamaño real en metros de la primitiva provisional.")]
        public Vector3 placeholderSize = new Vector3(0.2f, 0.05f, 0.2f);

        [Header("Apilado")]
        [Tooltip("Altura que ocupa al apilarse dentro de un contenedor.")]
        public float stackHeight = 0.04f;

        [Header("Reglas")]
        [Tooltip("Si cae al suelo queda sucio y no se puede servir.")]
        public bool getsDirtyWhenDropped = true;
        [Tooltip("Estado estropeado (quemado). Nunca cuenta para un pedido.")]
        public bool isFailedState;

        [Header("Contenedor")]
        public ContainerSettings container = new ContainerSettings();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public bool HasAnyTag(ItemTags mask) => (tags & mask) != 0;
        public bool IsContainer => container != null && container.isContainer;
    }
}
