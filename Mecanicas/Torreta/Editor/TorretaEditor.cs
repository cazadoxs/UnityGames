using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mecanicas.Torretas.Editor
{
    /// <summary>
    /// Inspector de la torreta: oculta los campos que no sirven en el modo elegido, explica el modo
    /// y, en Play, enseña el estado y botones de prueba. En la vista de escena dibuja los límites.
    /// </summary>
    [CustomEditor(typeof(Torreta))]
    [CanEditMultipleObjects]
    public class TorretaEditor : UnityEditor.Editor
    {
        static readonly HashSet<string> SoloGiro = new HashSet<string>
        {
            "ejeGiro", "velocidadGiro", "limitarGiro", "giroMinimo", "giroMaximo",
            "volverAReposo", "vigilarSinObjetivo", "anguloVigilancia", "velocidadVigilancia"
        };
        static readonly HashSet<string> SoloElevacion = new HashSet<string>
        {
            "pivotElevacion", "velocidadElevacion", "elevacionMinima", "elevacionMaxima", "compensarGravedad", "arcoAlto"
        };
        static readonly HashSet<string> SoloMunicion = new HashSet<string> { "tamanoCargador", "tiempoRecarga" };
        static readonly HashSet<string> SoloRafaga = new HashSet<string> { "pausaEntreRafagas" };
        static readonly HashSet<string> SoloLimites = new HashSet<string> { "giroMinimo", "giroMaximo" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var t = (Torreta)target;
            var modo = (ModoApuntado)serializedObject.FindProperty("modo").enumValueIndex;
            bool varios = serializedObject.isEditingMultipleObjects;

            EditorGUILayout.HelpBox(Explicacion(modo), MessageType.Info);

            var prop = serializedObject.GetIterator();
            bool entrar = true;
            while (prop.NextVisible(entrar))
            {
                entrar = false;
                string n = prop.name;
                if (n == "m_Script") continue;
                if (!varios)
                {
                    if (modo == ModoApuntado.Fija && SoloGiro.Contains(n)) continue;
                    if (modo != ModoApuntado.TodosLosAngulos && SoloElevacion.Contains(n)) continue;
                    if (t.municionInfinita && SoloMunicion.Contains(n)) continue;
                    if (t.balasPorRafaga <= 1 && SoloRafaga.Contains(n)) continue;
                    if (!t.limitarGiro && SoloLimites.Contains(n)) continue;
                }
                EditorGUILayout.PropertyField(prop, true);
            }
            serializedObject.ApplyModifiedProperties();

            if (!varios) Avisos(t);

            if (Application.isPlaying && !varios)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Estado (Play)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Objetivo", t.Objetivo != null ? t.Objetivo.name : "—");
                EditorGUILayout.LabelField("Apuntando", t.Alineada ? "Sí" : "No");
                if (!t.municionInfinita) EditorGUILayout.LabelField("Munición", t.Recargando ? "Recargando..." : t.MunicionActual.ToString());
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Disparar ahora")) t.DispararAhora();
                if (GUILayout.Button("Recargar")) t.Recargar();
                if (GUILayout.Button("Soltar objetivo")) t.AsignarObjetivo(null);
                EditorGUILayout.EndHorizontal();
                Repaint();
            }
        }

        static string Explicacion(ModoApuntado modo)
        {
            switch (modo)
            {
                case ModoApuntado.Fija:
                    return "FIJA: no gira. Dispara en la dirección de su primer punto de disparo cuando un objetivo entra en el cono 'Angulo Vision'.";
                case ModoApuntado.UnEje:
                    return "UN EJE: solo gira 'Pivot Giro' alrededor de 'Eje Giro' (por defecto el vertical). El cañón no sube ni baja; 'Angulo Vision' dice cuánto puede estar el objetivo por encima o por debajo.";
                default:
                    return "TODOS LOS ÁNGULOS: 'Pivot Giro' gira y 'Pivot Elevacion' sube y baja el cañón. Sin 'Pivot Elevacion', 'Pivot Giro' mira directamente al objetivo. Con gravedad, sube el cañón para compensar la caída.";
            }
        }

        static void Avisos(Torreta t)
        {
            if (t.prefabProyectil == null)
                EditorGUILayout.HelpBox("Falta 'Prefab Proyectil': la torreta apuntará pero no disparará.", MessageType.Warning);
            else if (t.prefabProyectil.GetComponent<Proyectil>() == null)
                EditorGUILayout.HelpBox("El prefab de bala no tiene el componente Proyectil.", MessageType.Warning);
            if (t.puntosDisparo == null || t.puntosDisparo.Length == 0)
                EditorGUILayout.HelpBox("Sin 'Puntos Disparo' las balas salen del centro del pivote.", MessageType.None);
            if (t.modo == ModoApuntado.TodosLosAngulos && t.pivotElevacion != null && t.pivotGiro != null && !t.pivotElevacion.IsChildOf(t.pivotGiro))
                EditorGUILayout.HelpBox("'Pivot Elevacion' debería ser hijo de 'Pivot Giro'.", MessageType.Warning);
            if (t.gravedadProyectil > 0f && t.modo != ModoApuntado.TodosLosAngulos)
                EditorGUILayout.HelpBox("Las balas tienen gravedad, pero en este modo el cañón no puede subir para compensarla.", MessageType.None);
        }

        void OnSceneGUI()
        {
            var t = (Torreta)target;
            // En Play el pivote ya está girado: solo dibujamos en edición, cuando está en reposo.
            if (Application.isPlaying || t.modo == ModoApuntado.Fija || !t.limitarGiro) return;
            Transform pivot = t.pivotGiro != null ? t.pivotGiro : t.transform;
            Vector3 eje = t.ejeGiro.sqrMagnitude > 0f ? t.ejeGiro.normalized : Vector3.up;
            Vector3 ejeMundo = pivot.rotation * eje;
            Vector3 frente = pivot.rotation * Vector3.forward;
            Vector3 desde = Quaternion.AngleAxis(t.giroMinimo, ejeMundo) * frente;
            Handles.color = new Color(0f, 1f, 0.4f, 0.12f);
            Handles.DrawSolidArc(pivot.position, ejeMundo, desde, t.giroMaximo - t.giroMinimo, Mathf.Min(t.alcance, 5f));
        }
    }
}
