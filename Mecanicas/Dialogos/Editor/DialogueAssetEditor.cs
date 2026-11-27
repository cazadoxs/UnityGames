using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mecanicas.Dialogos.Editor
{
    /// <summary>
    /// Inspector del DialogueAsset: escribir la conversación como texto, validarla y ver un resumen.
    /// </summary>
    [CustomEditor(typeof(DialogueAsset))]
    public class DialogueAssetEditor : UnityEditor.Editor
    {
        string script;
        bool showScript = true;
        bool showPreview;
        string lastReport;
        MessageType lastReportType;
        Vector2 scroll;

        void OnEnable()
        {
            var asset = (DialogueAsset)target;
            script = string.IsNullOrEmpty(asset.sourceScript) ? Template : asset.sourceScript;
        }

        public override void OnInspectorGUI()
        {
            var asset = (DialogueAsset)target;

            showScript = EditorGUILayout.BeginFoldoutHeaderGroup(showScript, "Escribir como texto");
            if (showScript)
            {
                EditorGUILayout.HelpBox(
                    "Nombre: texto   → línea      |  - texto → narrador\n" +
                    "[etiqueta]       → id de la línea de abajo\n" +
                    "> respuesta -> etiqueta   (o -> fin)\n" +
                    "-> etiqueta      → salto tras la línea anterior\n" +
                    "! evento x  ! activar x  ! desactivar x  ! espera 2  ! inicio x\n" +
                    "Al final de línea o respuesta: [si:var] [no:var]  · respuestas: [activa:var] [desactiva:var] [evento:x]",
                    MessageType.None);
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(160), GUILayout.MaxHeight(420));
                script = EditorGUILayout.TextArea(script, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Importar texto → líneas"))
                    {
                        if (asset.nodes.Count == 0 || EditorUtility.DisplayDialog("Importar texto",
                                "Se reemplazarán las líneas actuales del diálogo por las del texto. ¿Seguir?", "Reemplazar", "Cancelar"))
                        {
                            Undo.RecordObject(asset, "Importar diálogo");
                            var warnings = asset.LoadScript(script);
                            EditorUtility.SetDirty(asset);
                            var problems = asset.Validate();
                            warnings.AddRange(problems);
                            Report(warnings.Count == 0 ? $"Importadas {asset.nodes.Count} líneas sin problemas." : string.Join("\n", warnings),
                                warnings.Count == 0 ? MessageType.Info : MessageType.Warning);
                        }
                    }
                    if (GUILayout.Button("Recuperar texto guardado", GUILayout.Width(170)))
                        script = asset.sourceScript ?? "";
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validar saltos y etiquetas"))
                {
                    var p = asset.Validate();
                    Report(p.Count == 0 ? "Todo correcto." : string.Join("\n", p), p.Count == 0 ? MessageType.Info : MessageType.Warning);
                }
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button(new GUIContent("Probar (Play Mode)", "Lanza este diálogo en la escena que se está ejecutando.")))
                        DialogueManager.Play(asset, null, null, true);
                }
            }
            if (!string.IsNullOrEmpty(lastReport)) EditorGUILayout.HelpBox(lastReport, lastReportType);

            showPreview = EditorGUILayout.Foldout(showPreview, $"Resumen ({asset.nodes.Count} líneas)", true);
            if (showPreview) EditorGUILayout.HelpBox(BuildPreview(asset), MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Datos", EditorStyles.boldLabel);
            DrawPropertiesExcluding(serializedObject, "m_Script", "sourceScript");
            serializedObject.ApplyModifiedProperties();
        }

        void Report(string text, MessageType type) { lastReport = text; lastReportType = type; }

        static string BuildPreview(DialogueAsset a)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < a.nodes.Count; i++)
            {
                var n = a.nodes[i];
                if (n == null) continue;
                if (!string.IsNullOrEmpty(n.id)) sb.Append('[').Append(n.id).Append("] ");
                string who = n.actor != null ? n.actor.displayName : n.speakerName;
                sb.Append(i).Append(". ").Append(string.IsNullOrEmpty(who) ? "(narrador)" : who).Append(": ").AppendLine(Trim(n.text));
                if (n.choices != null)
                    foreach (var c in n.choices)
                        sb.Append("      > ").Append(Trim(c.text)).Append(string.IsNullOrEmpty(c.next) ? "" : "  → " + c.next).AppendLine();
                if (!string.IsNullOrEmpty(n.next)) sb.Append("      → ").AppendLine(n.next);
            }
            return sb.ToString();
        }

        static string Trim(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length > 60 ? s.Substring(0, 60) + "…" : s.Replace('\n', ' '));

        const string Template =
@"# Ejemplo. Borra esto y escribe tu conversación.
Juan: ¡Hola! ¿Qué te trae por aquí?
> Busco trabajo -> trabajo
> Nada, solo paseaba -> fin
[trabajo]
Juan: Pues has llegado en buen momento.
! activar tiene_trabajo
";
    }
}
