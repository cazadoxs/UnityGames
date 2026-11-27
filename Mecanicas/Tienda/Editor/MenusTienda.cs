using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Mecanicas.Tienda.Editor
{
    /// <summary>Menús del editor para montar la tienda sin escribir código.</summary>
    public static class MenusTienda
    {
        const string Raiz = "GameObject/Mecánicas/Tienda/";

        [MenuItem(Raiz + "Sistema de tienda (gestor + monedero + menú)", false, 10)]
        static void CrearSistema(MenuCommand cmd)
        {
            var g = FabricaTienda.CrearSistema(PosicionDeVista() + Vector3.up);
            Registrar(g.gameObject, cmd);
        }

        [MenuItem(Raiz + "Botón de compra con pedestal", false, 11)]
        static void CrearBoton(MenuCommand cmd)
        {
            var b = FabricaTienda.CrearBotonPedestal(PosicionDeVista(), Quaternion.identity, null);
            Registrar(b.gameObject, cmd);
        }

        [MenuItem(Raiz + "Terminal de tienda (abre el menú)", false, 12)]
        static void CrearTerminal(MenuCommand cmd)
        {
            var t = FabricaTienda.CrearTerminal(PosicionDeVista(), Quaternion.identity);
            Registrar(t.gameObject, cmd);
        }

        [MenuItem(Raiz + "Punto de aparición", false, 13)]
        static void CrearPunto(MenuCommand cmd)
        {
            var p = new GameObject("Punto de aparición").AddComponent<PuntoAparicion>();
            p.transform.position = PosicionDeVista() + Vector3.up;
            Registrar(p.gameObject, cmd);
        }

        [MenuItem("Herramientas/Mecánicas/Tienda/Borrar compras y dinero guardados (PlayerPrefs)", false, 50)]
        static void BorrarGuardado()
        {
            if (EditorUtility.DisplayDialog("Borrar guardado",
                "Esto borra TODOS los PlayerPrefs del proyecto (también los de otros sistemas). ¿Seguro?", "Borrar", "Cancelar"))
                PlayerPrefs.DeleteAll();
        }

        static void Registrar(GameObject go, MenuCommand cmd)
        {
            var padre = cmd != null ? cmd.context as GameObject : null;
            if (padre != null) GameObjectUtility.SetParentAndAlign(go, padre);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + go.name);
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        static Vector3 PosicionDeVista()
        {
            var vista = SceneView.lastActiveSceneView;
            if (vista == null) return Vector3.zero;
            var p = vista.pivot;
            return new Vector3(p.x, 0, p.z);
        }
    }
}
