using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mecanicas.Eventos.EditorTools
{
    /// <summary>
    /// Inspector de la zona: el de siempre más un botón para añadir acciones de cualquier tipo
    /// (también las que crees tú) y otro para recogerlas de los hijos.
    /// </summary>
    [CustomEditor(typeof(ZonaEvento))]
    public class ZonaEventoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ZonaEvento zona = (ZonaEvento)target;

            Collider col = zona.GetComponent<Collider>();
            if (col == null)
                EditorGUILayout.HelpBox("Falta un Collider (Box, Sphere...) con 'Is Trigger' marcado.", MessageType.Error);
            else if (!col.isTrigger)
            {
                EditorGUILayout.HelpBox("El Collider no es trigger: el jugador chocará con él y la zona no se activará.", MessageType.Warning);
                if (GUILayout.Button("Marcar 'Is Trigger'"))
                {
                    Undo.RecordObject(col, "Marcar trigger");
                    col.isTrigger = true;
                }
            }

            DrawDefaultInspector();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Añadir acción…"))
                    MenuAcciones(zona).ShowAsContext();

                if (GUILayout.Button("Recoger de hijos"))
                {
                    Undo.RecordObject(zona, "Recoger acciones");
                    zona.RecogerAcciones();
                    EditorUtility.SetDirty(zona);
                }
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("En juego", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Activaciones", zona.Activaciones.ToString());
                EditorGUILayout.LabelField("En curso", zona.EnCurso ? "Sí" : "No");
                EditorGUILayout.LabelField("Objetos dentro", zona.ObjetosDentro.Count.ToString());
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Activar")) zona.ForzarActivacion();
                    if (GUILayout.Button("Detener")) zona.Detener();
                    if (GUILayout.Button("Reiniciar")) zona.Reiniciar();
                }
                Repaint();
            }
        }

        private static GenericMenu MenuAcciones(ZonaEvento zona)
        {
            GenericMenu menu = new GenericMenu();
            var tipos = TypeCache.GetTypesDerivedFrom<AccionEvento>()
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.Name);

            foreach (Type t in tipos)
            {
                AddComponentMenu attr = (AddComponentMenu)Attribute.GetCustomAttribute(t, typeof(AddComponentMenu));
                string nombre = attr != null && !string.IsNullOrEmpty(attr.componentMenu)
                    ? attr.componentMenu.Split('/').Last()
                    : ObjectNames.NicifyVariableName(t.Name);

                Type tipo = t;
                menu.AddItem(new GUIContent(nombre), false, () =>
                {
                    Component c = Undo.AddComponent(zona.gameObject, tipo);
                    Undo.RecordObject(zona, "Añadir acción");
                    zona.Acciones.Add((AccionEvento)c);
                    EditorUtility.SetDirty(zona);
                });
            }
            return menu;
        }
    }

    /// <summary>Entradas de menú para crear zonas rápido.</summary>
    public static class MenuZonaEvento
    {
        [MenuItem("GameObject/Mecánicas/Zona de evento (caja)", false, 10)]
        private static void CrearCaja(MenuCommand cmd) => Crear(cmd, false);

        [MenuItem("GameObject/Mecánicas/Zona de evento (esfera)", false, 11)]
        private static void CrearEsfera(MenuCommand cmd) => Crear(cmd, true);

        private static void Crear(MenuCommand cmd, bool esfera)
        {
            GameObject go = new GameObject("Zona de evento");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);

            if (esfera) go.AddComponent<SphereCollider>().radius = 1.5f;
            else go.AddComponent<BoxCollider>().size = new Vector3(3f, 3f, 3f);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<ZonaEvento>();

            // Colocarla delante de la vista de escena si no tiene padre.
            if (cmd.context == null && SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;

            Undo.RegisterCreatedObjectUndo(go, "Crear zona de evento");
            Selection.activeGameObject = go;
        }
    }
}
