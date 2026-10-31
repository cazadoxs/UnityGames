using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Detecta con un rayo desde el centro de la cámara lo que mira el jugador, muestra la ayuda
    /// y reparte las acciones. También gestiona el objeto que lleva en la mano.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour, IItemHolder
    {
        public PlayerInputReader input;
        public Camera viewCamera;
        [Tooltip("Punto donde se lleva el objeto (hijo de la cámara, abajo a la derecha).")]
        public Transform holdPoint;

        [Header("Detección")]
        public float reach = 2.6f;
        [Tooltip("Capas con las que se puede interactuar. Excluye la del propio jugador.")]
        public LayerMask interactionMask = ~0;
        [Tooltip("Radio del rayo: más alto = más fácil apuntar.")]
        public float rayRadius = 0.05f;

        [Header("Soltar")]
        public float dropForwardSpeed = 2.5f;

        public Item HeldItem { get; private set; }
        public IInteractable CurrentTarget { get; private set; }
        public Vector3 CurrentPoint { get; private set; }
        public bool HasPrompt { get; private set; }
        public InteractionPrompt CurrentPrompt => prompt;

        InteractionPrompt prompt;
        IInteractable altTarget;
        Highlightable highlighted;
        readonly RaycastHit[] hits = new RaycastHit[16];

        void Awake()
        {
            if (input == null) input = GetComponentInParent<PlayerInputReader>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (viewCamera == null) viewCamera = Camera.main;
            if (holdPoint == null && viewCamera != null)
            {
                holdPoint = new GameObject("HoldPoint").transform;
                holdPoint.SetParent(viewCamera.transform, false);
                holdPoint.localPosition = new Vector3(0.28f, -0.32f, 0.6f);
            }
        }

        void Update()
        {
            if (input == null || viewCamera == null) return;

            // Mientras se mantiene la acción secundaria, el objetivo se queda fijo.
            if (!(input.AltHeld && altTarget != null))
            {
                altTarget = null;
                FindTarget();
            }

            var ctx = new InteractionContext(this, CurrentPoint);
            prompt = InteractionPrompt.Create(null);
            HasPrompt = CurrentTarget != null && CurrentTarget.GetPrompt(ctx, ref prompt);

            if (input.InteractPressed)
            {
                if (CurrentTarget != null) CurrentTarget.Interact(ctx);
            }
            if (input.AltHeld && CurrentTarget != null)
            {
                altTarget = CurrentTarget;
                CurrentTarget.InteractAlt(ctx, input.AltPressed, Time.deltaTime);
            }
            if (input.CycleDirection != 0 && CurrentTarget != null)
                CurrentTarget.Cycle(ctx, input.CycleDirection);

            if (input.DropPressed && HeldItem != null)
                DropHeld();
        }

        void FindTarget()
        {
            IInteractable found = null;
            Vector3 point = Vector3.zero;
            var ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.SphereCastNonAlloc(ray, rayRadius, hits, reach, interactionMask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.collider == null || h.distance >= best) continue;
                if (h.collider.transform.IsChildOf(transform)) continue; // el propio jugador
                if (HeldItem != null && h.collider.transform.IsChildOf(HeldItem.transform)) continue;
                var interactable = h.collider.GetComponentInParent<IInteractable>();
                best = h.distance;
                // Lo más cercano tapa lo de detrás aunque no sea interactuable (paredes).
                found = interactable;
                point = h.distance > 0f ? h.point : ray.GetPoint(0.1f);
            }

            CurrentTarget = found;
            CurrentPoint = point;

            Highlightable newHighlight = null;
            if (found is Component comp) newHighlight = comp.GetComponentInParent<Highlightable>();
            if (newHighlight != highlighted)
            {
                if (highlighted != null) highlighted.SetHighlighted(false);
                highlighted = newHighlight;
                if (highlighted != null) highlighted.SetHighlighted(true);
            }
        }

        public bool TryPickUp(Item item)
        {
            if (item == null || HeldItem != null || holdPoint == null) return false;
            item.AttachTo(holdPoint, this, Vector3.zero, Quaternion.identity);
            HeldItem = item;
            return true;
        }

        public void DropHeld()
        {
            if (HeldItem == null) return;
            var item = HeldItem;
            var forward = viewCamera != null ? viewCamera.transform.forward : transform.forward;
            item.Drop(forward * dropForwardSpeed);
        }

        public void OnItemRemoved(Item item)
        {
            if (HeldItem == item) HeldItem = null;
        }
    }
}
