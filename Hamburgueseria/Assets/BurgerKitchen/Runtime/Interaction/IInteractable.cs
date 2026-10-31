using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>Datos de una interacción: quién interactúa y en qué punto mira.</summary>
    public readonly struct InteractionContext
    {
        public readonly PlayerInteractor Interactor;
        public readonly Vector3 Point;

        public InteractionContext(PlayerInteractor interactor, Vector3 point)
        {
            Interactor = interactor;
            Point = point;
        }

        public Item Held => Interactor != null ? Interactor.HeldItem : null;
    }

    /// <summary>Textos que se muestran al jugador. Un campo vacío = esa acción no está disponible.</summary>
    public struct InteractionPrompt
    {
        public string Title;
        public string Primary;
        public string Secondary;
        public string Cycle;
        public string Info;
        /// <summary>0-1, o negativo si no hay barra.</summary>
        public float Progress;

        public static InteractionPrompt Create(string title)
            => new InteractionPrompt { Title = title, Progress = -1f };
    }

    /// <summary>
    /// Todo aquello con lo que el jugador puede interactuar (estaciones, objetos sueltos, caja...).
    /// Necesita un Collider en el mismo GameObject o en un hijo.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Rellena el texto de ayuda. Devuelve false si no hay nada que hacer.</summary>
        bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt);

        /// <summary>Acción principal (E / clic izquierdo): coger, dejar, combinar, entregar.</summary>
        void Interact(InteractionContext ctx);

        /// <summary>Acción secundaria mantenida (F / clic derecho): cortar, servir, limpiar, empezar.</summary>
        void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime);

        /// <summary>Cambiar opción (Q / rueda): sabor del batido, tipo de refresco...</summary>
        void Cycle(InteractionContext ctx, int direction);
    }

    /// <summary>Quien sostiene un objeto (jugador, hueco de estación, contenedor).</summary>
    public interface IItemHolder
    {
        /// <summary>El objeto se ha ido a otro sitio o se ha destruido.</summary>
        void OnItemRemoved(Item item);
    }
}
