using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Va en la cámara del jugador (primera persona). Lanza un rayo hacia delante; si da con un
    /// <see cref="DialogueInteractable"/> cercano, muestra el aviso y al pulsar la tecla empieza a hablar.
    /// </summary>
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Interactor (jugador)")]
    public class DialogueInteractor : MonoBehaviour
    {
        [Tooltip("Desde dónde se lanza el rayo. Vacío = este objeto (pon este componente en la cámara).")]
        public Transform rayOrigin;
        [Tooltip("Tecla para hablar.")]
        public KeyCode interactKey = KeyCode.E;
        [Tooltip("Alcance máximo del rayo.")]
        [Min(0.1f)] public float reach = 3f;
        [Tooltip("Radio del rayo (0 = rayo fino). Un poco de grosor hace más fácil apuntar a los NPC.")]
        [Min(0f)] public float radius = 0.15f;
        [Tooltip("Capas que puede tocar el rayo.")]
        public LayerMask layers = ~0;
        [Tooltip("Mostrar el aviso '[E] Hablar con...'.")]
        public bool showPrompt = true;

        DialogueInteractable current;
        bool promptShown;

        /// <summary>NPC al que está mirando ahora (o null).</summary>
        public DialogueInteractable Current => current;

        void Update()
        {
            if (DialogueManager.IsRunning) { SetPrompt(null); current = null; return; }

            current = Probe();
            SetPrompt(current != null ? current.GetPrompt(interactKey) : null);

            if (current != null && DialogueInput.GetKeyDown(interactKey))
            {
                SetPrompt(null);
                current.Interact();
            }
        }

        DialogueInteractable Probe()
        {
            var origin = rayOrigin != null ? rayOrigin : transform;
            var ray = new Ray(origin.position, origin.forward);
            bool hit = radius > 0f
                ? Physics.SphereCast(ray, radius, out var info, reach, layers, QueryTriggerInteraction.Ignore)
                : Physics.Raycast(ray, out info, reach, layers, QueryTriggerInteraction.Ignore);
            if (!hit) return null;
            var target = info.collider.GetComponentInParent<DialogueInteractable>();
            if (target == null || !target.CanInteract) return null;
            if (info.distance > target.maxDistance) return null;
            return target;
        }

        void SetPrompt(string text)
        {
            if (!showPrompt) return;
            bool show = !string.IsNullOrEmpty(text);
            if (!show && !promptShown) return;
            promptShown = show;
            if (!show && !DialogueManager.HasInstance) return;
            if (DialogueManager.Instance != null) DialogueManager.Instance.SetPrompt(text);
        }

        void OnDisable() => SetPrompt(null);
    }
}
