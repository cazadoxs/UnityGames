using UnityEditor;
using UnityEngine;

namespace Mecanicas.Patrulla.Editor
{
    /// <summary>
    /// Inspector de RutaDePuntos: botones para añadir, insertar, invertir y pegar puntos al suelo,
    /// y manejadores en la vista de escena para mover cada punto sin seleccionarlo.
    /// </summary>
    [CustomEditor(typeof(RutaDePuntos))]
    public class RutaDePuntosEditor : UnityEditor.Editor
    {
        static bool mostrarManejadores = true;

        public override void OnInspectorGUI()
        {
            var ruta = (RutaDePuntos)target;
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Herramientas", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"{ruta.NumeroDePuntos} puntos · {ruta.Longitud:0.0} m" + (ruta.cerrada ? " · circuito" : " · abierta") +
                "\nAmarillo = primer punto · Verde = esquina suave · Naranja = esquina en pico · Rojo = bloqueado",
                MessageType.None);

            if (!ruta.usarHijosComoPuntos)
            {
                EditorGUILayout.HelpBox("Los botones crean puntos como hijos y los añaden a la lista.", MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Añadir punto al final")) AnadirPunto(ruta, ruta.NumeroDePuntos);
                if (GUILayout.Button("Añadir al principio")) AnadirPunto(ruta, 0);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Invertir orden")) Invertir(ruta);
                if (GUILayout.Button("Pegar puntos al suelo")) PegarAlSuelo(ruta);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Todas las esquinas en pico")) CambiarEsquinas(ruta, TipoEsquina.Pico);
                if (GUILayout.Button("Todas suaves")) CambiarEsquinas(ruta, TipoEsquina.Suave);
            }
            if (GUILayout.Button("Recalcular ruta")) { ruta.Reconstruir(); SceneView.RepaintAll(); }

            mostrarManejadores = EditorGUILayout.Toggle("Manejadores en escena", mostrarManejadores);
        }

        void OnSceneGUI()
        {
            var ruta = (RutaDePuntos)target;
            if (!mostrarManejadores) return;
            int n = ruta.NumeroDePuntos;
            for (int i = 0; i < n; i++)
            {
                var t = ruta.ObtenerPunto(i);
                if (t == null) continue;
                Handles.Label(t.position + Vector3.up * 0.6f, $"{i}", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                Vector3 nueva = Handles.PositionHandle(t.position, Tools.pivotRotation == PivotRotation.Local ? t.rotation : Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(t, "Mover punto de ruta");
                    t.position = nueva;
                    ruta.MarcarSucia();
                }
            }
        }

        static void AnadirPunto(RutaDePuntos ruta, int indice)
        {
            int n = ruta.NumeroDePuntos;
            Vector3 pos;
            if (n == 0) pos = ruta.transform.position;
            else if (n == 1) pos = ruta.ObtenerPunto(0).position + ruta.transform.forward * 5f;
            else if (indice >= n)
            {
                Vector3 a = ruta.ObtenerPunto(n - 2).position, b = ruta.ObtenerPunto(n - 1).position;
                pos = b + (b - a).normalized * Mathf.Max(2f, Vector3.Distance(a, b));
            }
            else
            {
                Vector3 a = ruta.ObtenerPunto(1).position, b = ruta.ObtenerPunto(0).position;
                pos = b + (b - a).normalized * Mathf.Max(2f, Vector3.Distance(a, b));
            }

            var go = new GameObject("Punto");
            Undo.RegisterCreatedObjectUndo(go, "Añadir punto de ruta");
            go.transform.SetParent(ruta.transform, false);
            go.transform.position = pos;
            if (ruta.usarHijosComoPuntos)
            {
                go.transform.SetSiblingIndex(Mathf.Clamp(indice, 0, ruta.transform.childCount - 1));
            }
            else
            {
                Undo.RecordObject(ruta, "Añadir punto de ruta");
                ruta.puntos.Insert(Mathf.Clamp(indice, 0, ruta.puntos.Count), go.transform);
            }
            Renombrar(ruta);
            ruta.MarcarSucia();
            Selection.activeGameObject = go;
        }

        static void Invertir(RutaDePuntos ruta)
        {
            if (ruta.usarHijosComoPuntos)
            {
                Undo.RegisterFullObjectHierarchyUndo(ruta.gameObject, "Invertir ruta");
                int c = ruta.transform.childCount;
                for (int i = 0; i < c; i++) ruta.transform.GetChild(c - 1).SetSiblingIndex(i);
            }
            else
            {
                Undo.RecordObject(ruta, "Invertir ruta");
                ruta.puntos.Reverse();
            }
            Renombrar(ruta);
            ruta.MarcarSucia();
        }

        static void PegarAlSuelo(RutaDePuntos ruta)
        {
            for (int i = 0; i < ruta.NumeroDePuntos; i++)
            {
                var t = ruta.ObtenerPunto(i);
                if (Physics.Raycast(t.position + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Undo.RecordObject(t, "Pegar puntos al suelo");
                    t.position = hit.point;
                }
            }
            ruta.MarcarSucia();
        }

        static void CambiarEsquinas(RutaDePuntos ruta, TipoEsquina tipo)
        {
            Undo.RecordObject(ruta, "Cambiar esquinas");
            ruta.esquinaPorDefecto = tipo;
            for (int i = 0; i < ruta.NumeroDePuntos; i++)
            {
                var a = ruta.ObtenerAjustes(i);
                if (a == null) continue;
                Undo.RecordObject(a, "Cambiar esquinas");
                a.usarEsquinaPropia = false;
                EditorUtility.SetDirty(a);
            }
            EditorUtility.SetDirty(ruta);
            ruta.MarcarSucia();
        }

        static void Renombrar(RutaDePuntos ruta)
        {
            ruta.Reconstruir();
            for (int i = 0; i < ruta.NumeroDePuntos; i++)
            {
                var t = ruta.ObtenerPunto(i);
                if (t.name.StartsWith("Punto")) { Undo.RecordObject(t.gameObject, "Renombrar"); t.name = $"Punto {i}"; }
            }
        }
    }
}
