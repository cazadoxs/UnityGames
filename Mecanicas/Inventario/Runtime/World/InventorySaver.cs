using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Guardado rápido del inventario en PlayerPrefs (para pruebas o juegos sencillos).
    /// Para tu propio sistema de partidas usa Inventory.ToJson() / LoadFromJson().
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/Guardado rápido")]
    public class InventorySaver : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [SerializeField] private string saveKey = "inventario_jugador";
        [SerializeField] private KeyCode saveHotkey = KeyCode.F5;
        [SerializeField] private KeyCode loadHotkey = KeyCode.F9;
        [SerializeField] private bool loadOnStart = false;
        [SerializeField] private bool saveOnQuit = false;

        public event System.Action<string> Message;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<Inventory>();
        }

        private void Start()
        {
            if (loadOnStart) Load();
        }

        private void Update()
        {
            if (InventoryInput.GetKeyDown(saveHotkey)) Save();
            if (InventoryInput.GetKeyDown(loadHotkey)) Load();
        }

        public void Save()
        {
            if (inventory == null) return;
            inventory.SaveToPlayerPrefs(saveKey);
            Message?.Invoke("Inventario guardado");
        }

        public void Load()
        {
            if (inventory == null) return;
            Message?.Invoke(inventory.LoadFromPlayerPrefs(saveKey) ? "Inventario cargado" : "No hay inventario guardado");
        }

        private void OnApplicationQuit()
        {
            if (saveOnQuit) Save();
        }
    }
}
