using System;
using UnityEngine;

namespace Mecanicas.Inventario
{
    /// <summary>
    /// Recoger objetos mirando a ellos (primera persona). Ponlo en el jugador.
    /// Lanza un rayo desde la cámara; si apunta a un WorldItem muestra el aviso
    /// y lo recoge al pulsar la tecla.
    /// </summary>
    [AddComponentMenu("Mecánicas/Inventario/Recogedor (primera persona)")]
    public class InventoryInteractor : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [Tooltip("Cámara desde la que se apunta. Vacío = cámara principal.")]
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float range = 3f;
        [Tooltip("Radio del rayo (0 = rayo fino). Un poco de radio hace más fácil apuntar a objetos pequeños.")]
        [SerializeField] private float sphereRadius = 0.05f;
        [SerializeField] private LayerMask layers = ~0;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;
        [Tooltip("Mensaje cuando no cabe.")]
        [SerializeField] private string fullMessage = "Inventario lleno";

        public WorldItem CurrentTarget { get; private set; }
        public KeyCode PickupKey => pickupKey;
        /// <summary>Texto listo para mostrar en pantalla ("[E] Recoger Manzana"), vacío si no apunta a nada.</summary>
        public string PromptText => CurrentTarget == null ? string.Empty : "[" + KeyName(pickupKey) + "] " + CurrentTarget.Prompt;

        /// <summary>(objeto, cantidad recogida)</summary>
        public event Action<ItemData, int> PickedUp;
        /// <summary>Mensajes para el jugador ("Inventario lleno").</summary>
        public event Action<string> Message;

        /// <summary>Pon a false para bloquear la recogida (menús abiertos, cinemáticas...).</summary>
        public bool InteractionEnabled { get; set; } = true;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponentInParent<Inventory>();
        }

        private void Update()
        {
            var cam = viewCamera != null ? viewCamera : Camera.main;
            CurrentTarget = null;
            if (!InteractionEnabled || cam == null || inventory == null) return;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            bool hitSomething = sphereRadius > 0f
                ? Physics.SphereCast(ray, sphereRadius, out var hit, range, layers, QueryTriggerInteraction.Collide)
                : Physics.Raycast(ray, out hit, range, layers, QueryTriggerInteraction.Collide);
            if (!hitSomething) return;

            var wi = hit.collider.GetComponentInParent<WorldItem>();
            if (wi == null || wi.Item == null) return;
            CurrentTarget = wi;

            if (InventoryInput.GetKeyDown(pickupKey))
            {
                var item = wi.Item;
                int before = wi.Amount;
                if (wi.TryPickup(inventory))
                {
                    int taken = wi != null ? before - wi.Amount : before;
                    PickedUp?.Invoke(item, taken);
                }
                else Message?.Invoke(fullMessage);
            }
        }

        public static string KeyName(KeyCode k)
        {
            if (k >= KeyCode.Alpha0 && k <= KeyCode.Alpha9) return ((int)(k - KeyCode.Alpha0)).ToString();
            switch (k)
            {
                case KeyCode.Mouse0: return "Clic izq.";
                case KeyCode.Mouse1: return "Clic der.";
                case KeyCode.Mouse2: return "Rueda";
                case KeyCode.Escape: return "Esc";
                case KeyCode.Space: return "Espacio";
                default: return k.ToString();
            }
        }
    }
}
