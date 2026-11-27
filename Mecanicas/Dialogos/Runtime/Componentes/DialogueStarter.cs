using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Lanza un diálogo. Es el punto de enganche con cualquier otro sistema:
    /// - Desde un UnityEvent (botón, trigger, sistema de eventos, animación...): llama a Play().
    /// - Solo: marca "Al entrar en el trigger" y pon un Collider con Is Trigger en este objeto.
    /// - Desde código: GetComponent&lt;DialogueStarter&gt;().Play();
    /// </summary>
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Starter (iniciar diálogo)")]
    public class DialogueStarter : MonoBehaviour
    {
        [Tooltip("Conversación que se lanza.")]
        public DialogueAsset dialogue;

        [Tooltip("Conversación que se lanza a partir de la segunda vez (opcional). Útil para 'Ya te lo he dicho...'.")]
        public DialogueAsset repeatDialogue;

        [Tooltip("Etiqueta por la que empezar (vacío = inicio del diálogo).")]
        public string startNode;

        [Tooltip("Solo se puede lanzar una vez.")]
        public bool playOnce;

        [Tooltip("Si ya hay otra conversación en curso, cortarla y empezar esta.")]
        public bool interruptCurrent;

        [Header("Disparo automático (opcional)")]
        [Tooltip("Lanzar al empezar la escena.")]
        public bool playOnStart;
        [Tooltip("Lanzar cuando el jugador entra en el trigger de este objeto (necesita un Collider con Is Trigger).")]
        public bool playOnTriggerEnter;
        [Tooltip("Etiqueta (tag) del jugador para el trigger.")]
        public string playerTag = "Player";

        [Header("Eventos")]
        public UnityEvent onStarted;
        public UnityEvent onFinished;

        /// <summary>Veces que se ha lanzado.</summary>
        public int TimesPlayed { get; private set; }

        void Start() { if (playOnStart) Play(); }

        /// <summary>Lanza el diálogo. Pensado para UnityEvents.</summary>
        public void Play() => TryPlay();

        /// <summary>Lanza el diálogo empezando en una etiqueta. Pensado para UnityEvents con parámetro string.</summary>
        public void PlayFrom(string nodeId) => TryPlay(nodeId);

        /// <summary>Lanza el diálogo y devuelve si ha empezado.</summary>
        public bool TryPlay(string nodeOverride = null)
        {
            if (!isActiveAndEnabled) return false;
            if (playOnce && TimesPlayed > 0) return false;
            var d = TimesPlayed > 0 && repeatDialogue != null ? repeatDialogue : dialogue;
            if (d == null) { Debug.LogWarning($"[Diálogos] {name}: no hay diálogo asignado.", this); return false; }

            string node = !string.IsNullOrEmpty(nodeOverride) ? nodeOverride : (d == dialogue ? startNode : null);
            bool ok = DialogueManager.Play(d, node, () => onFinished?.Invoke(), interruptCurrent);
            if (ok) { TimesPlayed++; onStarted?.Invoke(); }
            return ok;
        }

        /// <summary>Permite volver a lanzarlo aunque tenga "solo una vez".</summary>
        public void ResetPlays() => TimesPlayed = 0;

        /// <summary>Puede lanzarse ahora mismo.</summary>
        public bool CanPlay => isActiveAndEnabled && !(playOnce && TimesPlayed > 0) && dialogue != null && !DialogueManager.IsRunning;

        void OnTriggerEnter(Collider other)
        {
            if (!playOnTriggerEnter) return;
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
            Play();
        }
    }
}
