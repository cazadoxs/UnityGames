using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Frase suelta (subtítulo que no bloquea), sin necesidad de crear un DialogueAsset.
    /// Ideal para comentarios de NPC al pasar ("¡Cuidado con el perro!") o pensamientos del jugador.
    /// Llama a Say() desde un UnityEvent, o marca "Al entrar en el trigger".
    /// Si pones varias frases se dicen en orden (o al azar, una cada vez).
    /// </summary>
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Line (frase suelta)")]
    public class DialogueLine : MonoBehaviour
    {
        public enum Mode { TodasEnOrden, UnaCadaVezEnOrden, UnaAlAzar }

        [Tooltip("Personaje que habla (opcional, da color al nombre).")]
        public DialogueActor actor;
        [Tooltip("Nombre si no hay personaje. Vacío = sin nombre.")]
        public string speakerName;
        [TextArea(1, 4)] public string[] lines = { "..." };
        [Tooltip("Voz opcional para cada frase (mismo orden que las frases).")]
        public AudioClip[] voices;
        public Mode mode = Mode.TodasEnOrden;
        [Tooltip("Segundos por frase. 0 = automático según la longitud.")]
        [Min(0f)] public float duration;

        [Header("Disparo")]
        public bool sayOnTriggerEnter;
        public string playerTag = "Player";
        [Tooltip("Solo se dice una vez.")]
        public bool once;
        [Tooltip("Segundos mínimos entre una vez y la siguiente.")]
        [Min(0f)] public float cooldown = 5f;

        int nextIndex;
        float lastTime = -999f;
        bool done;

        /// <summary>Dice la(s) frase(s).</summary>
        public void Say()
        {
            if (lines == null || lines.Length == 0 || (once && done)) return;
            if (Time.time - lastTime < cooldown) return;
            lastTime = Time.time;
            done = true;

            switch (mode)
            {
                case Mode.TodasEnOrden:
                    for (int i = 0; i < lines.Length; i++) Emit(i);
                    break;
                case Mode.UnaCadaVezEnOrden:
                    Emit(nextIndex);
                    nextIndex = (nextIndex + 1) % lines.Length;
                    break;
                case Mode.UnaAlAzar:
                    Emit(Random.Range(0, lines.Length));
                    break;
            }
        }

        void Emit(int i)
        {
            var voice = voices != null && i < voices.Length ? voices[i] : null;
            if (actor != null && string.IsNullOrEmpty(speakerName)) DialogueManager.ShowLine(actor, lines[i], duration, voice);
            else DialogueManager.ShowLine(speakerName, lines[i], duration, voice);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!sayOnTriggerEnter) return;
            if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
            Say();
        }
    }
}
