using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Conecta los "eventos" escritos en los diálogos con cosas de la escena.
    /// Ejemplo: la línea tiene Event Key = "abrir_puerta" → aquí añades una entrada "abrir_puerta"
    /// y en su UnityEvent arrastras la puerta y eliges su método Abrir().
    /// </summary>
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Event Listener (escuchar eventos)")]
    public class DialogueEventListener : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Nombre del evento tal como está escrito en el diálogo.")]
            public string key;
            public UnityEvent response;
        }

        public List<Entry> events = new List<Entry>();

        void OnEnable() => DialogueManager.NodeEvent += Handle;
        void OnDisable() => DialogueManager.NodeEvent -= Handle;

        void Handle(string key)
        {
            foreach (var e in events)
                if (e != null && string.Equals(e.key, key, StringComparison.OrdinalIgnoreCase))
                    e.response?.Invoke();
        }
    }
}
