using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mecanicas.Dialogos.Editor
{
    /// <summary>Menú "Herramientas > Mecánicas > Diálogos".</summary>
    public static class DialogueMenu
    {
        [MenuItem("Herramientas/Mecánicas/Diálogos/Añadir Dialogue Manager a la escena")]
        static void AddManager()
        {
            var existing = Object.FindAnyObjectByType<DialogueManager>();
            if (existing != null) { Selection.activeObject = existing; EditorGUIUtility.PingObject(existing); return; }
            var go = new GameObject("DialogueManager", typeof(DialogueManager));
            new GameObject("VistaDialogos", typeof(DialogueViewUGUI)).transform.SetParent(go.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "Añadir Dialogue Manager");
            Selection.activeObject = go;
        }

        [MenuItem("Herramientas/Mecánicas/Diálogos/Hacer hablable el objeto seleccionado")]
        static void MakeTalkable()
        {
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<DialogueInteractable>() == null) Undo.AddComponent<DialogueInteractable>(go);
                if (go.GetComponentInChildren<Collider>() == null) Undo.AddComponent<CapsuleCollider>(go);
            }
        }

        [MenuItem("Herramientas/Mecánicas/Diálogos/Hacer hablable el objeto seleccionado", true)]
        static bool MakeTalkableValidate() => Selection.gameObjects.Length > 0;

        [MenuItem("Herramientas/Mecánicas/Diálogos/Añadir Dialogue Interactor a la cámara principal")]
        static void AddInteractor()
        {
            var cam = Camera.main;
            if (cam == null) { EditorUtility.DisplayDialog("Diálogos", "No hay ninguna cámara con la etiqueta MainCamera.", "Vale"); return; }
            if (cam.GetComponent<DialogueInteractor>() == null) Undo.AddComponent<DialogueInteractor>(cam.gameObject);
            Selection.activeObject = cam.gameObject;
        }

        [MenuItem("Herramientas/Mecánicas/Diálogos/Abrir escena demo")]
        static void OpenDemo()
        {
            var guids = AssetDatabase.FindAssets("DialogosDemo t:Scene");
            if (guids.Length == 0) { EditorUtility.DisplayDialog("Diálogos", "No encuentro la escena DialogosDemo. ¿Copiaste la carpeta Demo?", "Vale"); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
