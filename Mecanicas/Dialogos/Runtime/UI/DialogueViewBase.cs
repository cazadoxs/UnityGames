using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>Datos de la línea que se va a mostrar.</summary>
    public struct DialogueLineInfo
    {
        public string speaker;      // vacío = narrador
        public Color speakerColor;
        public Sprite portrait;     // puede ser null
        public string fullText;
    }

    /// <summary>
    /// Parte visual del sistema. El <see cref="DialogueManager"/> solo habla con esta clase, así que
    /// para cambiar el aspecto (TextMeshPro, UI Toolkit, bocadillos 3D...) basta con heredar de aquí
    /// y asignar tu vista en el campo "View" del DialogueManager.
    /// </summary>
    public abstract class DialogueViewBase : MonoBehaviour
    {
        /// <summary>Abre la caja de conversación.</summary>
        public abstract void Open();
        /// <summary>Cierra la caja de conversación (y las respuestas).</summary>
        public abstract void Close();

        /// <summary>Prepara una línea nueva: nombre, color, retrato. El texto llega después con <see cref="SetText"/>.</summary>
        public abstract void ShowLine(DialogueLineInfo line);
        /// <summary>Texto visible (el manager lo llama letra a letra para el efecto máquina de escribir).</summary>
        public abstract void SetText(string richText);
        /// <summary>Muestra u oculta el indicador de "pulsa para continuar".</summary>
        public abstract void SetContinueHint(bool visible);

        /// <summary>Muestra las respuestas. Llama a <paramref name="onChosen"/> con el índice elegido.</summary>
        public abstract void ShowChoices(IReadOnlyList<string> options, Action<int> onChosen);
        public abstract void HideChoices();

        /// <summary>Subtítulo para frases sueltas que no bloquean al jugador.</summary>
        public abstract void ShowSubtitle(string speaker, Color speakerColor, string text);
        public abstract void HideSubtitle();

        /// <summary>Aviso de interacción ("[E] Hablar").</summary>
        public abstract void ShowPrompt(string text);
        public abstract void HidePrompt();
    }
}
