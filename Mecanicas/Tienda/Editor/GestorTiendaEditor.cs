using UnityEditor;
using UnityEngine;

namespace Mecanicas.Tienda.Editor
{
    /// <summary>Inspector del GestorTienda: avisa de conexiones mal puestas y permite dar dinero en Play.</summary>
    [CustomEditor(typeof(GestorTienda))]
    public class GestorTiendaEditor : UnityEditor.Editor
    {
        int cantidad = 100;
        string moneda = "monedas";

        public override void OnInspectorGUI()
        {
            var g = (GestorTienda)target;
            if (g.monedero != null && !(g.monedero is IMonedero))
                EditorGUILayout.HelpBox("El componente de 'Monedero' no implementa IMonedero.", MessageType.Error);
            if (g.inventario != null && !(g.inventario is IInventarioCompras))
                EditorGUILayout.HelpBox("El componente de 'Inventario' no implementa IInventarioCompras.", MessageType.Error);
            if (g.receptorPorDefecto != null && !(g.receptorPorDefecto is IReceptorCompra))
                EditorGUILayout.HelpBox("El 'Receptor por defecto' no implementa IReceptorCompra (usa un PuntoAparicion).", MessageType.Error);
            if (g.catalogo == null)
                EditorGUILayout.HelpBox("Sin catálogo: la tienda en menú saldrá vacía (los botones del mundo funcionan igual).", MessageType.Info);

            DrawDefaultInspector();

            if (!Application.isPlaying) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pruebas (solo en Play)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            moneda = EditorGUILayout.TextField(moneda);
            cantidad = EditorGUILayout.IntField(cantidad);
            if (GUILayout.Button("Dar") && g.Monedero != null) g.Monedero.Anadir(moneda, cantidad);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Reiniciar stock y compras")) g.ReiniciarTienda();
            if (g.Monedero != null) EditorGUILayout.LabelField("Saldo de " + moneda + ": " + g.Monedero.Saldo(moneda));
        }
    }
}
