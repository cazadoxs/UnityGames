using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Reacciona cuando el jugador usa un objeto concreto, sin programar:
    /// arrastra el ItemData, y en "On Used" conecta lo que quieras
    /// (encender una luz, abrir una puerta, curar, reproducir un sonido...).
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/Al usar objeto (evento)")]
    public class ItemUseListener : MonoBehaviour
    {
        [Tooltip("Objeto a escuchar. Vacío = cualquier objeto.")]
        [SerializeField] private ItemData item;
        [Tooltip("Inventario a escuchar. Vacío = el primero de la escena.")]
        [SerializeField] private Inventory inventory;
        public UnityEvent onUsed = new UnityEvent();
        public ItemEvent onUsedItem = new ItemEvent();

        private void OnEnable()
        {
            if (inventory == null)
#if UNITY_2023_1_OR_NEWER
                inventory = FindFirstObjectByType<Inventory>();
#else
                inventory = FindObjectOfType<Inventory>();
#endif
            if (inventory != null) inventory.ItemUsed += HandleUsed;
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.ItemUsed -= HandleUsed;
        }

        private void HandleUsed(ItemData used, int slot)
        {
            if (item != null && used != item) return;
            onUsed.Invoke();
            onUsedItem.Invoke(used);
        }
    }
}
