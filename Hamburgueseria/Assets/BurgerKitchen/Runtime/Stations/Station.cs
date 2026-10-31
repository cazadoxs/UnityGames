using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Base de toda estación con huecos donde dejar objetos (encimeras, plancha, freidora, máquinas).
    /// Necesita un Collider (en este objeto o en un hijo) para que el jugador la pueda apuntar.
    /// Los huecos son Transforms hijos (sin escala); si no se asigna ninguno se crea uno encima.
    /// </summary>
    public abstract class Station : MonoBehaviour, IInteractable, IItemHolder, IWorldStatusSource
    {
        public class Slot
        {
            public int Index;
            public Transform Anchor;
            public Item Item;
            public bool IsEmpty => Item == null;
        }

        [Header("Estación")]
        public string stationName = "Estación";
        [Tooltip("Puntos donde se colocan los objetos. Vacío = uno solo en 'defaultSlotOffset'.")]
        public List<Transform> slotAnchors = new List<Transform>();
        public Vector3 defaultSlotOffset = new Vector3(0f, 1f, 0f);
        [Tooltip("Altura de las etiquetas flotantes sobre cada hueco.")]
        public float statusHeight = 0.35f;

        protected Slot[] slots;
        public IReadOnlyList<Slot> Slots => slots;

        protected virtual void Awake()
        {
            var anchors = new List<Transform>();
            foreach (var a in slotAnchors) if (a != null) anchors.Add(a);
            if (anchors.Count == 0)
            {
                var a = new GameObject("Slot").transform;
                a.SetParent(transform, false);
                a.localPosition = defaultSlotOffset;
                anchors.Add(a);
            }
            slots = new Slot[anchors.Count];
            for (int i = 0; i < anchors.Count; i++) slots[i] = new Slot { Index = i, Anchor = anchors[i] };
        }

        protected virtual void OnEnable() => WorldStatusRegistry.Register(this);
        protected virtual void OnDisable() => WorldStatusRegistry.Unregister(this);

        // ---------- Huecos ----------

        protected Slot NearestSlot(Vector3 point, bool occupied)
        {
            Slot best = null;
            float bestDist = float.MaxValue;
            foreach (var s in slots)
            {
                if (s.IsEmpty == occupied) continue;
                float d = (s.Anchor.position - point).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = s; }
            }
            return best;
        }

        protected Slot SlotOf(Item item)
        {
            foreach (var s in slots) if (s.Item == item) return s;
            return null;
        }

        /// <summary>¿Se puede dejar este objeto aquí? Las estaciones especializadas filtran.</summary>
        public virtual bool CanPlace(Item item) => item != null;

        public bool TryPlace(Item item, Vector3 nearPoint)
        {
            if (!CanPlace(item)) return false;
            var slot = NearestSlot(nearPoint, false);
            if (slot == null) return false;
            slot.Item = item;
            item.AttachTo(slot.Anchor, this, Vector3.zero, Quaternion.identity);
            OnItemPlaced(slot);
            return true;
        }

        public void OnItemRemoved(Item item)
        {
            var slot = SlotOf(item);
            if (slot == null) return;
            slot.Item = null;
            OnItemTaken(slot, item);
        }

        protected virtual void OnItemPlaced(Slot slot) { }
        protected virtual void OnItemTaken(Slot slot, Item item) { }

        // ---------- Comportamiento por defecto: superficie ----------
        // Con las manos vacías coge; con algo en la mano lo deja o lo combina con lo que haya.

        protected bool SurfacePrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            var held = ctx.Held;
            var target = NearestSlot(ctx.Point, true);
            if (held == null)
            {
                if (target == null) return false;
                prompt.Primary = "Coger " + target.Item.Describe();
                return true;
            }
            if (target != null && ItemTransfer.CanCombine(held, target.Item))
            {
                prompt.Primary = ItemTransfer.DescribeCombine(held, target.Item);
                return true;
            }
            if (CanPlace(held) && NearestSlot(ctx.Point, false) != null)
            {
                prompt.Primary = "Dejar " + held.Definition.DisplayName;
                return true;
            }
            return false;
        }

        protected bool SurfaceInteract(InteractionContext ctx)
        {
            var held = ctx.Held;
            var target = NearestSlot(ctx.Point, true);
            if (held == null)
                return target != null && ctx.Interactor.TryPickUp(target.Item);
            if (target != null && ItemTransfer.TryCombine(held, target.Item))
                return true;
            return TryPlace(held, ctx.Point);
        }

        // ---------- IInteractable ----------

        public virtual bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            prompt.Title = stationName;
            return SurfacePrompt(ctx, ref prompt);
        }

        public virtual void Interact(InteractionContext ctx) => SurfaceInteract(ctx);
        public virtual void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime) { }
        public virtual void Cycle(InteractionContext ctx, int direction) { }

        public virtual void CollectStatus(List<WorldStatus> buffer) { }

        protected Vector3 StatusPosition(Slot s) => s.Anchor.position + Vector3.up * statusHeight;
    }
}
