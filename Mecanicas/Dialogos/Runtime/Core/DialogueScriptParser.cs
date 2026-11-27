using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Convierte una conversación escrita como texto en una lista de <see cref="DialogueNode"/>.
    /// Formato (una instrucción por renglón):
    /// <code>
    /// # comentario
    /// [saludo]                      etiqueta: id de la línea siguiente
    /// Juan: Hola, forastero.        línea con hablante
    /// - Hace frío.                  línea de narrador (sin nombre)
    /// > Busco trabajo -> trabajo    respuesta de la línea anterior (destino opcional)
    /// > Adiós -> fin                respuesta que termina la conversación
    /// -> saludo                     salto tras la línea anterior
    /// ! evento abrir_puerta         lanza un evento al mostrar la línea anterior
    /// ! activar tiene_llave         activa una variable  (también: ! desactivar x)
    /// ! espera 2.5                  la línea anterior avanza sola tras 2.5 s
    /// ! inicio saludo               la conversación empieza en esa etiqueta
    /// </code>
    /// Etiquetas al final de una línea o respuesta: [si:variable] [no:variable];
    /// solo en respuestas además: [activa:variable] [desactiva:variable] [evento:nombre].
    /// </summary>
    public static class DialogueScriptParser
    {
        public class Result
        {
            public List<DialogueNode> Nodes = new List<DialogueNode>();
            public string StartNode;
            public List<string> Warnings = new List<string>();
        }

        static readonly Regex TrailingTag = new Regex(@"\s*\[(si|no|activa|desactiva|evento)\s*:\s*([^\]]+)\]\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex LabelLine = new Regex(@"^\[([^\]:]+)\]$", RegexOptions.CultureInvariant);
        static readonly Regex SpeakerLine = new Regex(@"^([^:\[\]<>]{1,40}):\s*(.*)$", RegexOptions.CultureInvariant);

        public static Result Parse(string script)
        {
            var r = new Result();
            if (string.IsNullOrEmpty(script)) return r;

            string pendingLabel = null;
            DialogueNode last = null;
            string[] lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//")) continue;

                // [etiqueta]
                var label = LabelLine.Match(line);
                if (label.Success)
                {
                    if (pendingLabel != null) r.Warnings.Add($"Renglón {lineNo}: la etiqueta '{pendingLabel}' no tiene ninguna línea debajo.");
                    pendingLabel = label.Groups[1].Value.Trim();
                    continue;
                }

                // -> salto
                if (line.StartsWith("->"))
                {
                    if (last == null) { r.Warnings.Add($"Renglón {lineNo}: salto sin línea anterior."); continue; }
                    last.next = line.Substring(2).Trim();
                    continue;
                }

                // > respuesta
                if (line.StartsWith(">"))
                {
                    if (last == null) { r.Warnings.Add($"Renglón {lineNo}: respuesta sin línea anterior."); continue; }
                    last.choices.Add(ParseChoice(line.Substring(1).Trim()));
                    continue;
                }

                // ! comando
                if (line.StartsWith("!"))
                {
                    ParseCommand(r, last, line.Substring(1).Trim(), lineNo);
                    continue;
                }

                // Línea de diálogo
                var node = new DialogueNode();
                string body = line;
                ExtractTags(ref body, out var tags);
                foreach (var (key, value) in tags)
                {
                    if (key == "si") node.requiredFlag = value;
                    else if (key == "no") node.forbiddenFlag = value;
                    else if (key == "activa") node.setFlag = value;
                    else if (key == "desactiva") node.clearFlag = value;
                    else if (key == "evento") node.eventKey = value;
                }

                if (body.StartsWith("- "))
                {
                    node.text = body.Substring(2).Trim();
                }
                else
                {
                    var sp = SpeakerLine.Match(body);
                    if (sp.Success && !sp.Groups[1].Value.Contains("http"))
                    {
                        node.speakerName = sp.Groups[1].Value.Trim();
                        node.text = sp.Groups[2].Value.Trim();
                    }
                    else node.text = body;
                }

                node.text = node.text.Replace("\\n", "\n");
                if (pendingLabel != null) { node.id = pendingLabel; pendingLabel = null; }
                r.Nodes.Add(node);
                last = node;
            }

            if (pendingLabel != null) r.Warnings.Add($"La etiqueta '{pendingLabel}' está al final y no tiene ninguna línea debajo.");
            return r;
        }

        static DialogueChoice ParseChoice(string text)
        {
            var c = new DialogueChoice();
            ExtractTags(ref text, out var tags);
            int arrow = text.LastIndexOf("->", StringComparison.Ordinal);
            if (arrow >= 0)
            {
                c.next = text.Substring(arrow + 2).Trim();
                text = text.Substring(0, arrow).Trim();
                // Las etiquetas pueden ir antes o después de la flecha.
                ExtractTags(ref text, out var more);
                tags.AddRange(more);
            }
            c.text = text;
            foreach (var (key, value) in tags)
            {
                switch (key)
                {
                    case "si": c.requiredFlag = value; break;
                    case "no": c.forbiddenFlag = value; break;
                    case "activa": c.setFlag = value; break;
                    case "desactiva": c.clearFlag = value; break;
                    case "evento": c.eventKey = value; break;
                }
            }
            return c;
        }

        static void ParseCommand(Result r, DialogueNode last, string cmd, int lineNo)
        {
            int space = cmd.IndexOf(' ');
            string verb = (space < 0 ? cmd : cmd.Substring(0, space)).Trim().ToLowerInvariant();
            string arg = space < 0 ? "" : cmd.Substring(space + 1).Trim();

            if (verb == "inicio" || verb == "start") { r.StartNode = arg; return; }
            if (last == null) { r.Warnings.Add($"Renglón {lineNo}: comando '!{verb}' sin línea anterior."); return; }

            switch (verb)
            {
                case "evento": case "event": last.eventKey = arg; break;
                case "activar": case "set": last.setFlag = arg; break;
                case "desactivar": case "clear": last.clearFlag = arg; break;
                case "espera": case "wait":
                    if (float.TryParse(arg.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
                        last.autoAdvanceDelay = Math.Max(0f, s);
                    else r.Warnings.Add($"Renglón {lineNo}: '{arg}' no es un número de segundos.");
                    break;
                default:
                    r.Warnings.Add($"Renglón {lineNo}: comando desconocido '!{verb}'.");
                    break;
            }
        }

        static void ExtractTags(ref string text, out List<(string, string)> tags)
        {
            tags = new List<(string, string)>();
            while (true)
            {
                var m = TrailingTag.Match(text);
                if (!m.Success) break;
                tags.Insert(0, (m.Groups[1].Value.ToLowerInvariant(), m.Groups[2].Value.Trim()));
                text = text.Substring(0, m.Index).TrimEnd();
            }
        }
    }
}
