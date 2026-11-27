using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Un objeto tirado en el mundo que se puede recoger.
    /// Ponlo en cualquier GameObject con Collider. El InventoryInteractor del
    /// jugador lo detecta al mirarlo; también se puede recoger al tocarlo.
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/Objeto en el mundo (recogible)")]
    public class WorldItem : MonoBehaviour
    {
        [SerializeField] private ItemData item;
        [Min(1)] [SerializeField] private int amount = 1;
        [Tooltip("Recoger automáticamente al acercarse (pasar por encima). Busca un Inventory en un radio.")]
        [SerializeField] private bool pickupOnTouch = false;
        [Tooltip("Radio (en metros) de la recogida automática.")]
        [SerializeField] private float touchRadius = 0.7f;
        [Tooltip("Texto del aviso. {0} = nombre, {1} = cantidad.")]
        [SerializeField] private string promptFormat = "Recoger {0}";
        [Tooltip("Sonido al recogerlo (opcional).")]
        [SerializeField] private AudioClip pickupSound;

        public ItemData Item => item;
        public bool PickupOnTouch { get => pickupOnTouch; set => pickupOnTouch = value; }
        public int Amount => amount;

        public string Prompt
        {
            get
            {
                if (item == null) return string.Empty;
                string n = amount > 1 ? item.DisplayName + " x" + amount : item.DisplayName;
                return string.Format(promptFormat, n, amount);
            }
        }

        public void Setup(ItemData newItem, int newAmount)
        {
            item = newItem;
            amount = Mathf.Max(1, newAmount);
        }

        /// <summary>Intenta meter el objeto en el inventario. Lo que no quepa se queda en el suelo.</summary>
        public bool TryPickup(Inventory inventory)
        {
            if (inventory == null || item == null || amount <= 0) return false;
            int left = inventory.Add(item, amount);
            int taken = amount - left;
            if (taken <= 0) return false;
            if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            amount = left;
            if (amount <= 0) Destroy(gameObject);
            return true;
        }

        private static readonly Collider[] overlap = new Collider[16];
        private float nextTouchCheck;

        private void FixedUpdate()
        {
            if (!pickupOnTouch || Time.time < nextTouchCheck) return;
            nextTouchCheck = Time.time + 0.1f;
            // OverlapSphere funciona también con CharacterController (que no lanza OnCollisionEnter).
            int n = Physics.OverlapSphereNonAlloc(transform.position, touchRadius, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n && this != null && amount > 0; i++) TouchPickup(overlap[i]);
        }

        private void OnTriggerEnter(Collider other) => TouchPickup(other);

        private void TouchPickup(Collider other)
        {
            if (!pickupOnTouch || other == null || amount <= 0) return;
            var inv = other.GetComponentInParent<Inventory>();
            if (inv != null) TryPickup(inv);
        }

        /// <summary>
        /// Crea un objeto en el mundo. Usa el Prefab del ItemData; si no tiene,
        /// crea un cubo del color del icono. Garantiza Collider, Rigidbody y WorldItem.
        /// </summary>
        public static WorldItem Spawn(ItemData item, int amount, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            if (item == null || amount <= 0) return null;
            GameObject go;
            if (item.WorldPrefab != null)
            {
                go = Instantiate(item.WorldPrefab, position, rotation);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(position, rotation);
                go.transform.localScale = Vector3.one * 0.3f;
                var r = go.GetComponent<Renderer>();
                if (r != null) r.material.color = item.IconColor;
            }
            go.name = item.DisplayName + (amount > 1 ? " x" + amount : "");

            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.AddForce(velocity, ForceMode.VelocityChange);

            var wi = go.GetComponent<WorldItem>();
            if (wi == null) wi = go.AddComponent<WorldItem>();
            wi.Setup(item, amount);
            return wi;
        }
    }
}
