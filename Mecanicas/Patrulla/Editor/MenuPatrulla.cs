using UnityEditor;
using UnityEngine;

namespace Mecanicas.Patrulla.Editor
{
    /// <summary>Entradas de menú para crear rutas y seguidores sin escribir código.</summary>
    public static class MenuPatrulla
    {
        [MenuItem("GameObject/Mecanicas/Patrulla/Ruta de puntos (circuito)", false, 10)]
        static void CrearCircuito(MenuCommand cmd) => CrearRuta(cmd, true);

        [MenuItem("GameObject/Mecanicas/Patrulla/Ruta de puntos (abierta)", false, 11)]
        static void CrearAbierta(MenuCommand cmd) => CrearRuta(cmd, false);

        static void CrearRuta(MenuCommand cmd, bool cerrada)
        {
            var go = new GameObject(cerrada ? "Ruta (circuito)" : "Ruta (abierta)");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            if (cmd.context == null && SceneView.lastActiveSceneView != null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            var ruta = go.AddComponent<RutaDePuntos>();
            ruta.cerrada = cerrada;

            Vector3[] forma = cerrada
                ? new[] { new Vector3(-5, 0, -5), new Vector3(5, 0, -5), new Vector3(5, 0, 5), new Vector3(-5, 0, 5) }
                : new[] { new Vector3(-6, 0, 0), new Vector3(-2, 0, 4), new Vector3(2, 0, 0), new Vector3(6, 0, 4) };
            for (int i = 0; i < forma.Length; i++)
            {
                var p = new GameObject($"Punto {i}");
                p.transform.SetParent(go.transform, false);
                p.transform.localPosition = forma[i];
            }
            ruta.Reconstruir();
            Undo.RegisterCreatedObjectUndo(go, "Crear ruta de puntos");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/Mecanicas/Patrulla/Semáforo", false, 12)]
        static void CrearSemaforo(MenuCommand cmd)
        {
            var go = new GameObject("Semáforo");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            go.AddComponent<SemaforoDeRuta>();
            Undo.RegisterCreatedObjectUndo(go, "Crear semáforo");
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/Mecanicas/Patrulla/Generador de tráfico", false, 13)]
        static void CrearGenerador(MenuCommand cmd)
        {
            var go = new GameObject("Generador de tráfico");
            GameObjectUtility.SetParentAndAlign(go, cmd.context as GameObject);
            var g = go.AddComponent<GeneradorDeTrafico>();
            if (cmd.context is GameObject padre) g.ruta = padre.GetComponentInParent<RutaDePuntos>();
            Undo.RegisterCreatedObjectUndo(go, "Crear generador de tráfico");
            Selection.activeGameObject = go;
        }

        [MenuItem("Herramientas/Mecánicas/Patrulla/Hacer que los seleccionados sigan la ruta más cercana", false, 0)]
        static void AnadirSeguidor()
        {
            var rutas = Object.FindObjectsByType<RutaDePuntos>(FindObjectsSortMode.None);
            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<RutaDePuntos>() != null || go.GetComponentInParent<RutaDePuntos>() != null) continue;
                var s = go.GetComponent<SeguidorDeRuta>();
                if (s == null) s = Undo.AddComponent<SeguidorDeRuta>(go);
                Undo.RecordObject(s, "Asignar ruta");
                RutaDePuntos mejor = null; float mejorD = float.MaxValue;
                foreach (var r in rutas)
                {
                    if (!r.EsValida) continue;
                    float d = (r.PosicionEn(r.DistanciaMasCercana(go.transform.position)) - go.transform.position).sqrMagnitude;
                    if (d < mejorD) { mejorD = d; mejor = r; }
                }
                s.ruta = mejor;
                s.empezarEnLugarMasCercano = true;
                EditorUtility.SetDirty(s);
                Debug.Log(mejor != null ? $"[Patrulla] {go.name} seguirá '{mejor.name}'." : $"[Patrulla] No hay rutas en la escena para {go.name}.", go);
            }
        }

        [MenuItem("Herramientas/Mecánicas/Patrulla/Hacer que los seleccionados sigan la ruta más cercana", true)]
        static bool ValidarAnadirSeguidor() => Selection.gameObjects.Length > 0;
    }
}
