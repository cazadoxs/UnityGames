using UnityEditor;
using UnityEngine;

namespace Mecanicas.MiradaNPC.Editor
{
    /// <summary>Inspector con botones de configuración y dibujo de los límites en la vista de escena.</summary>
    [CustomEditor(typeof(SeguimientoMiradaNPC))]
    public class SeguimientoMiradaNPCEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var npc = (SeguimientoMiradaNPC)target;

            EditorGUILayout.HelpBox(
                "1) Pulsa 'Autoconfigurar huesos' (detecta Humanoid o nombres como Head/Neck/Spine/Eye).\n" +
                "2) Ajusta los límites del cuello y de los ojos (arcos verde y azul en la escena).\n" +
                "3) Pon un 'Objetivo de Mirada' en la cámara del jugador, o deja que use la cámara principal.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Autoconfigurar huesos"))
                {
                    Undo.RecordObject(npc, "Autoconfigurar huesos de mirada");
                    npc.AutoConfigurar();
                    EditorUtility.SetDirty(npc);
                }
                if (GUILayout.Button("Calibrar ojos"))
                {
                    Undo.RecordObject(npc, "Calibrar ojos");
                    npc.CalibrarOjos();
                    EditorUtility.SetDirty(npc);
                }
            }

            if (npc.cadena.Count == 0 && npc.ojos.Count == 0)
                EditorGUILayout.HelpBox("No hay huesos asignados: el NPC no podrá girar nada.", MessageType.Warning);

            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Estado", npc.EstaMirando
                    ? $"Mirando a {(npc.ObjetivoActual != null ? npc.ObjetivoActual.name : "un punto")}  (H {npc.GiroHorizontal:0}°, V {npc.GiroVertical:0}°)"
                    : "Sin objetivo");
                Repaint();
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        void OnSceneGUI()
        {
            var npc = (SeguimientoMiradaNPC)target;
            Transform referencia = npc.referenciaCuerpo != null ? npc.referenciaCuerpo : npc.transform;
            Vector3 origen = npc.origenMirada != null ? npc.origenMirada.position : OrigenAproximado(npc);
            float radio = 0.7f * HandleUtility.GetHandleSize(origen);

            Vector3 adelante = referencia.forward;
            Vector3 arriba = referencia.up;
            Vector3 derecha = referencia.right;
            var l = npc.limitesCuello;

            // Cuello: horizontal (verde) y vertical (verde claro).
            Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.18f);
            Handles.DrawSolidArc(origen, arriba, Quaternion.AngleAxis(-l.izquierda, arriba) * adelante, l.izquierda + l.derecha, radio);
            Handles.color = new Color(0.6f, 1f, 0.4f, 0.18f);
            Handles.DrawSolidArc(origen, derecha, Quaternion.AngleAxis(-l.arriba, derecha) * adelante, l.arriba + l.abajo, radio);

            // Campo de visión (contorno gris).
            if (npc.campoVision < 360f)
            {
                Handles.color = new Color(1f, 1f, 1f, 0.5f);
                float mitad = npc.campoVision * 0.5f;
                Handles.DrawWireArc(origen, arriba, Quaternion.AngleAxis(-mitad, arriba) * adelante, npc.campoVision, radio * 1.4f);
            }

            // Ojos (azul), relativos a la cabeza.
            foreach (var ojo in npc.ojos)
            {
                if (ojo?.hueso == null) continue;
                Quaternion marco = ojo.calibrado ? ojo.hueso.rotation * ojo.calibracion : referencia.rotation;
                Vector3 f = marco * Vector3.forward, u = marco * Vector3.up, r = marco * Vector3.right;
                var lo = npc.limitesOjos;
                float ro = radio * 0.35f;
                Handles.color = new Color(0.3f, 0.6f, 1f, 0.3f);
                Handles.DrawSolidArc(ojo.hueso.position, u, Quaternion.AngleAxis(-lo.izquierda, u) * f, lo.izquierda + lo.derecha, ro);
                Handles.DrawSolidArc(ojo.hueso.position, r, Quaternion.AngleAxis(-lo.arriba, r) * f, lo.arriba + lo.abajo, ro);
            }
        }

        static Vector3 OrigenAproximado(SeguimientoMiradaNPC npc)
        {
            int n = 0;
            Vector3 suma = Vector3.zero;
            foreach (var o in npc.ojos)
                if (o?.hueso != null) { suma += o.hueso.position; n++; }
            if (n > 0) return suma / n;
            for (int i = npc.cadena.Count - 1; i >= 0; i--)
                if (npc.cadena[i]?.hueso != null) return npc.cadena[i].hueso.position;
            return npc.transform.position + Vector3.up * 1.6f;
        }
    }
}
