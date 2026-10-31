using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>Cubo de basura. Los contenedores se vacían (conservas el envase/bandeja); lo demás se tira.</summary>
    public class TrashStation : MonoBehaviour, IInteractable
    {
        public string stationName = "Basura";
        [Tooltip("Las bandejas y envases se vacían en vez de tirarse.")]
        public bool keepContainers = true;
        public UnityEvent onTrashed;

        public bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            prompt.Title = stationName;
            var held = ctx.Held;
            if (held == null) return true;
            prompt.Primary = keepContainers && held.Container != null && held.Container.Contents.Count > 0
                ? "Vaciar " + held.Definition.DisplayName
                : "Tirar " + held.Definition.DisplayName;
            return true;
        }

        public void Interact(InteractionContext ctx)
        {
            var held = ctx.Held;
            if (held == null) return;
            if (keepContainers && held.Container != null && held.Container.Contents.Count > 0 && !held.IsDirty)
                held.Container.Clear();
            else
                held.Consume();
            onTrashed?.Invoke();
        }

        public void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime) { }
        public void Cycle(InteractionContext ctx, int direction) { }
    }
}
