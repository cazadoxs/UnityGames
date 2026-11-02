using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Una conversación completa (o una sola frase). Se crea en el Project:
    /// clic derecho > Create > Mecánicas > Diálogos > Diálogo.
    /// Se puede rellenar a mano en el Inspector o escribiéndola como texto (botón "Importar desde texto").
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Diálogos/Diálogo", fileName = "NuevoDialogo", order = 0)]
    public class DialogueAsset : ScriptableObject
    {
        /// <summary>Palabra reservada para terminar la conversación desde un salto o una respuesta.</summary>
        public const string EndKeyword = "fin";

        [Tooltip("Id de la línea por la que empieza. Vacío = la primera de la lista.")]
        public string startNode;

        [Tooltip("Personajes que participan. Las líneas cuyo nombre coincida con el de un personaje usan su color y retrato.")]
        public List<DialogueActor> actors = new List<DialogueActor>();

        [Tooltip("Líneas de la conversación, en orden.")]
        public List<DialogueNode> nodes = new List<DialogueNode>();

        [Tooltip("Si está activo, el jugador queda bloqueado (cursor libre, controles desactivados) durante la conversación. Desactívalo para conversaciones 'de fondo' que se leen mientras se juega.")]
        public bool blocksPlayer = true;

        [Tooltip("Texto de origen si se importó desde texto (se guarda para poder volver a editarlo).")]
        [TextArea(3, 20)] public string sourceScript;

        /// <summary>Índice de la línea con ese id, o -1.</summary>
        public int IndexOf(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return -1;
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != null && string.Equals(nodes[i].id, nodeId, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>Índice de inicio (startNode o 0).</summary>
        public int StartIndex => string.IsNullOrEmpty(startNode) ? 0 : Mathf.Max(0, IndexOf(startNode));

        /// <summary>Busca el personaje por nombre en la lista de actores.</summary>
        public DialogueActor FindActor(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var a in actors)
                if (a != null && string.Equals(a.displayName, name, System.StringComparison.OrdinalIgnoreCase))
                    return a;
            return null;
        }

        /// <summary>Crea un diálogo en memoria a partir de texto (ver README, formato de texto).</summary>
        public static DialogueAsset FromScript(string script, string name = "Dialogo")
        {
            var asset = CreateInstance<DialogueAsset>();
            asset.name = name;
            asset.LoadScript(script);
            return asset;
        }

        /// <summary>Reemplaza las líneas por las del texto. Devuelve los avisos del analizador.</summary>
        public List<string> LoadScript(string script)
        {
            var result = DialogueScriptParser.Parse(script);
            nodes = result.Nodes;
            startNode = result.StartNode;
            sourceScript = script;
            return result.Warnings;
        }

        /// <summary>Comprueba saltos rotos e ids repetidos. Devuelve una lista de problemas (vacía = todo bien).</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                if (n == null) continue;
                if (!string.IsNullOrEmpty(n.id) && !seen.Add(n.id))
                    problems.Add($"Línea {i}: el id '{n.id}' está repetido.");
                CheckTarget(problems, $"Línea {i} ('{Short(n.text)}')", n.next);
                if (n.choices != null)
                    for (int c = 0; c < n.choices.Count; c++)
                        CheckTarget(problems, $"Línea {i}, respuesta {c + 1} ('{Short(n.choices[c].text)}')", n.choices[c].next);
            }
            if (!string.IsNullOrEmpty(startNode) && IndexOf(startNode) < 0)
                problems.Add($"La línea de inicio '{startNode}' no existe.");
            return problems;
        }

        void CheckTarget(List<string> problems, string where, string target)
        {
            if (string.IsNullOrEmpty(target) || IsEnd(target)) return;
            if (IndexOf(target) < 0) problems.Add($"{where}: salta a '{target}', que no existe.");
        }

        public static bool IsEnd(string target) =>
            string.Equals(target, EndKeyword, System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(target, "end", System.StringComparison.OrdinalIgnoreCase);

        static string Short(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 25 ? s.Substring(0, 25) + "…" : s);
    }
}
