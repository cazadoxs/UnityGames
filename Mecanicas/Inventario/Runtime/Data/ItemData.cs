using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Definición de un tipo de objeto (manzana, llave, linterna...).
    /// Es un ScriptableObject: se crea como archivo en el proyecto con
    /// clic derecho > Create > Mecánicas > Inventario > Objeto.
    /// Hereda de esta clase para crear objetos con comportamiento propio al usarlos.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Inventario/Objeto", fileName = "NuevoObjeto", order = 0)]
    public class ItemData : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Identificador único y estable. Se usa para guardar/cargar. Si se deja vacío se usa el nombre del archivo.")]
        [SerializeField] private string id = "";
        [Tooltip("Nombre que ve el jugador.")]
        [SerializeField] private string displayName = "Objeto";
        [TextArea(2, 5)]
        [SerializeField] private string description = "";
        [Tooltip("Categoría libre (Comida, Llaves, Herramientas...). Se usa al ordenar y en el tooltip.")]
        [SerializeField] private string category = "General";

        [Header("Aspecto en la interfaz")]
        [Tooltip("Icono del objeto. Si no hay icono se pinta un cuadrado del color de abajo con las iniciales.")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color iconColor = Color.white;

        [Header("Pilas y peso")]
        [Tooltip("Cuántas unidades caben en una casilla. 1 = no apilable.")]
        [Min(1)] [SerializeField] private int maxStack = 1;
        [Tooltip("Peso de una unidad. Solo importa si el inventario tiene un peso máximo.")]
        [Min(0f)] [SerializeField] private float weight = 0f;

        [Header("Mundo 3D")]
        [Tooltip("Prefab que aparece en el mundo al soltar el objeto (debería llevar WorldItem, Collider y Rigidbody; si no, se añaden solos). Si está vacío se crea un cubo.")]
        [SerializeField] private GameObject worldPrefab;
        [Tooltip("Modelo que se ve en la mano al seleccionarlo en la barra rápida. Si está vacío se usa el Prefab del mundo.")]
        [SerializeField] private GameObject heldPrefab;
        [Tooltip("Escala aplicada al modelo en la mano.")]
        [SerializeField] private float heldScale = 1f;

        [Header("Uso")]
        [Tooltip("Si se puede usar (clic derecho en la casilla o tecla de usar con el objeto en la mano).")]
        [SerializeField] private bool usable = false;
        [Tooltip("Si al usarlo se gasta una unidad.")]
        [SerializeField] private bool consumeOnUse = false;
        [Tooltip("Si se puede tirar al mundo.")]
        [SerializeField] private bool canDrop = true;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;
        public string Category => category;
        public Sprite Icon => icon;
        public Color IconColor => iconColor;
        public int MaxStack => Mathf.Max(1, maxStack);
        public float Weight => weight;
        public bool IsStackable => MaxStack > 1;
        public GameObject WorldPrefab => worldPrefab;
        public GameObject HeldPrefab => heldPrefab != null ? heldPrefab : worldPrefab;
        public float HeldScale => heldScale;
        public bool Usable => usable;
        public bool ConsumeOnUse => consumeOnUse;
        public bool CanDrop => canDrop;

        /// <summary>¿Se puede usar ahora mismo? Sobrescríbelo para añadir condiciones.</summary>
        public virtual bool CanUse(Inventory owner) => usable;

        /// <summary>
        /// Lógica al usar el objeto. Devuelve true si se ha usado (y entonces,
        /// si ConsumeOnUse está activo, se gasta una unidad).
        /// La versión base no hace nada más: escucha el evento Inventory.ItemUsed
        /// o pon un ItemUseListener en la escena para reaccionar.
        /// </summary>
        public virtual bool Use(Inventory owner, int slotIndex) => usable;

        /// <summary>Texto extra para el tooltip. Sobrescríbelo para mostrar estadísticas propias.</summary>
        public virtual string GetTooltipExtra() => string.Empty;

#if UNITY_EDITOR
        /// <summary>Solo para herramientas de editor (generador de la demo).</summary>
        public void EditorSetup(string newId, string newName, string newDescription, string newCategory, Color color,
            int newMaxStack, float newWeight, bool isUsable, bool consume, bool droppable = true)
        {
            id = newId; displayName = newName; description = newDescription; category = newCategory;
            iconColor = color; maxStack = Mathf.Max(1, newMaxStack); weight = newWeight;
            usable = isUsable; consumeOnUse = consume; canDrop = droppable;
        }

        public void EditorSetPrefabs(GameObject world, GameObject held, float scaleInHand)
        {
            worldPrefab = world; heldPrefab = held; heldScale = scaleInHand;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id)) id = name;
            if (maxStack < 1) maxStack = 1;
        }
#endif
    }
}
