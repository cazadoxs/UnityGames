using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Una línea de diálogo. Una conversación es una lista de nodos que se recorre en orden,
    /// salvo que un nodo indique otro destino (campo <see cref="next"/>) o tenga respuestas.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        [Tooltip("Identificador opcional. Solo hace falta si alguna respuesta o salto apunta a esta línea.")]
        public string id;

        [Tooltip("Personaje que habla (opcional). Si está vacío se usa 'Nombre del hablante'.")]
        public DialogueActor actor;

        [Tooltip("Nombre del hablante si no hay Personaje asignado. Vacío = narrador (sin nombre).")]
        public string speakerName;

        [TextArea(2, 6)]
        [Tooltip("Texto de la línea. Admite rich text de Unity: <b>, <i>, <color=#ff0000>...")]
        public string text;

        [Tooltip("Audio de voz opcional para esta línea.")]
        public AudioClip voice;

        [Tooltip("0 = espera a que el jugador pulse continuar. >0 = avanza sola tras esos segundos (cuenta desde que termina de escribirse el texto).")]
        [Min(0f)] public float autoAdvanceDelay;

        [Tooltip("Respuestas que puede elegir el jugador. Vacío = línea sin elección.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [Tooltip("A qué línea saltar después (por id). Vacío = la siguiente de la lista. 'fin' = termina la conversación.")]
        public string next;

        [Header("Condiciones (si no se cumplen, la línea se salta)")]
        [Tooltip("La línea solo se muestra si esta variable está activada.")]
        public string requiredFlag;
        [Tooltip("La línea solo se muestra si esta variable NO está activada.")]
        public string forbiddenFlag;

        [Header("Efectos al mostrarse")]
        [Tooltip("Variable que se activa al mostrar esta línea.")]
        public string setFlag;
        [Tooltip("Variable que se desactiva al mostrar esta línea.")]
        public string clearFlag;
        [Tooltip("Nombre de evento que se lanza al mostrar esta línea (lo escucha un DialogueEventListener o DialogueManager.NodeEvent).")]
        public string eventKey;
    }

    /// <summary>Una respuesta que puede elegir el jugador.</summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("Texto del botón de respuesta.")]
        public string text;

        [Tooltip("Id de la línea a la que lleva. Vacío = la línea siguiente a la pregunta. 'fin' = termina.")]
        public string next;

        [Tooltip("La respuesta solo aparece si esta variable está activada.")]
        public string requiredFlag;
        [Tooltip("La respuesta solo aparece si esta variable NO está activada.")]
        public string forbiddenFlag;

        [Tooltip("Variable que se activa al elegirla.")]
        public string setFlag;
        [Tooltip("Variable que se desactiva al elegirla.")]
        public string clearFlag;
        [Tooltip("Evento que se lanza al elegirla.")]
        public string eventKey;
    }
}
