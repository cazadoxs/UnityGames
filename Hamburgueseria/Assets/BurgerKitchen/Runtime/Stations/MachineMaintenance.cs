using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>
    /// Mantenimiento de una máquina: tras unos usos hay que limpiarla (o cambiar el aceite)
    /// y a veces se avería. Mientras tanto la estación no funciona.
    /// Se repara manteniendo la acción secundaria sobre la estación.
    /// </summary>
    public class MachineMaintenance : MonoBehaviour, IWorldStatusSource
    {
        [Tooltip("Usos antes de necesitar limpieza (0 = nunca).")]
        [Min(0)] public int usesBeforeService = 10;
        [Range(0f, 1f)] public float breakdownChancePerUse = 0f;
        [Min(0.1f)] public float serviceDuration = 4f;
        public string serviceLabel = "Limpiar";
        public string repairLabel = "Reparar";
        public Vector3 statusOffset = new Vector3(0f, 1.6f, 0f);

        public UnityEvent onNeedsService;
        public UnityEvent onBrokeDown;
        public UnityEvent onServiced;

        public int Uses { get; private set; }
        public bool IsBroken { get; private set; }
        public float ServiceProgress { get; private set; }
        public bool NeedsService => IsBroken || (usesBeforeService > 0 && Uses >= usesBeforeService);
        public bool IsOperational => !NeedsService;
        public string ActionLabel => IsBroken ? repairLabel : serviceLabel;

        void OnEnable() => WorldStatusRegistry.Register(this);
        void OnDisable() => WorldStatusRegistry.Unregister(this);

        public void RegisterUse()
        {
            bool wasOk = IsOperational;
            Uses++;
            if (breakdownChancePerUse > 0f && Random.value < breakdownChancePerUse)
            {
                IsBroken = true;
                onBrokeDown?.Invoke();
                KitchenEvents.Post($"¡{name} se ha averiado!", MessageKind.Bad);
            }
            if (wasOk && NeedsService)
            {
                onNeedsService?.Invoke();
                if (!IsBroken) KitchenEvents.Post($"{name}: necesita {serviceLabel.ToLowerInvariant()}", MessageKind.Info);
            }
        }

        /// <summary>Avanza la limpieza/reparación. Devuelve true al terminar.</summary>
        public bool Service(float deltaTime)
        {
            if (!NeedsService) return false;
            ServiceProgress += deltaTime / serviceDuration;
            if (ServiceProgress < 1f) return false;
            ServiceProgress = 0f;
            Uses = 0;
            IsBroken = false;
            onServiced?.Invoke();
            return true;
        }

        public void CollectStatus(List<WorldStatus> buffer)
        {
            if (!NeedsService) return;
            var label = IsBroken ? "¡AVERIADA!" : serviceLabel.ToUpperInvariant() + "!";
            buffer.Add(new WorldStatus(transform.TransformPoint(statusOffset), label,
                ServiceProgress > 0f ? ServiceProgress : -1f, StatusKind.Danger));
        }
    }
}
