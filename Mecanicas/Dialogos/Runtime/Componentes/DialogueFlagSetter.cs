using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Lee y escribe variables de diálogo desde UnityEvents, y reacciona cuando cambian.
    /// Ejemplo: al recoger la llave (otro sistema) llama a SetFlag("tiene_llave") y el NPC
    /// mostrará la respuesta "[si:tiene_llave]".
    /// </summary>
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Flag (variable)")]
    public class DialogueFlagSetter : MonoBehaviour
    {
        [Tooltip("Variable que vigila este componente.")]
        public string flag;
        [Tooltip("Se lanza cuando la variable se activa (y al empezar si ya lo estaba).")]
        public UnityEvent onTrue;
        [Tooltip("Se lanza cuando la variable se desactiva.")]
        public UnityEvent onFalse;

        void OnEnable()
        {
            DialogueVariables.Changed += Handle;
            if (!string.IsNullOrEmpty(flag) && DialogueVariables.Get(flag)) onTrue?.Invoke();
        }

        void OnDisable() => DialogueVariables.Changed -= Handle;

        void Handle(string changed, bool value)
        {
            if (!string.Equals(changed, flag, System.StringComparison.OrdinalIgnoreCase)) return;
            (value ? onTrue : onFalse)?.Invoke();
        }

        public void SetFlag(string name) => DialogueVariables.Set(name, true);
        public void ClearFlag(string name) => DialogueVariables.Set(name, false);
        public void SetThisFlag() => DialogueVariables.Set(flag, true);
        public void ClearThisFlag() => DialogueVariables.Set(flag, false);
    }
}
