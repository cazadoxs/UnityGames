using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Hace que un NPC u objeto sea "hablable": cuando el jugador lo mira de cerca aparece el aviso
    /// y al pulsar la tecla de interacción se lanza su <see cref="DialogueStarter"/>.
    /// Necesita un Collider (no trigger) en este objeto o en un hijo para que la mirada lo detecte.
    /// Si ya tienes tu propio sistema de interacción, puedes llamar directamente a Interact().
    /// </summary>
    [RequireComponent(typeof(DialogueStarter))]
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Interactable (NPC hablable)")]
    public class DialogueInteractable : MonoBehaviour
    {
        [Tooltip("Texto del aviso. {tecla} = tecla de interacción, {nombre} = nombre del NPC.")]
        public string promptText = "[{tecla}] Hablar con {nombre}";
        [Tooltip("Nombre que se usa en el aviso. Vacío = nombre del GameObject.")]
        public string displayName;
        [Tooltip("Distancia máxima desde la que se puede hablar (además de la del DialogueInteractor).")]
        [Min(0.1f)] public float maxDistance = 3f;

        DialogueStarter starter;

        public DialogueStarter Starter => starter != null ? starter : (starter = GetComponent<DialogueStarter>());

        public bool CanInteract => isActiveAndEnabled && Starter != null && Starter.CanPlay;

        public string GetPrompt(KeyCode key)
        {
            string n = string.IsNullOrEmpty(displayName) ? name : displayName;
            return promptText.Replace("{tecla}", KeyName(key)).Replace("{nombre}", n);
        }

        /// <summary>Empieza la conversación.</summary>
        public void Interact() { if (CanInteract) Starter.Play(); }

        static string KeyName(KeyCode k)
        {
            if (k >= KeyCode.A && k <= KeyCode.Z) return k.ToString();
            if (k == KeyCode.Mouse0) return "Clic";
            return k.ToString();
        }
    }
}
