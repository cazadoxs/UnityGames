using UnityEditor;
using UnityEngine;

namespace Mecanicas.Inventario.Editor
{
    /// <summary>Inspector del Inventory: en Play muestra el contenido y permite añadir objetos de prueba.</summary>
    [CustomEditor(typeof(Inventory))]
    public class InventoryEditor : UnityEditor.Editor
    {
        private ItemData testItem;
        private int testAmount = 1;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;

            var inv = (Inventory)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Contenido (en juego)", EditorStyles.boldLabel);
            for (int i = 0; i < inv.Capacity; i++)
            {
                var s = inv.GetSlot(i);
                if (s.IsEmpty) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((i < inv.HotbarSize ? "Barra " : "Mochila ") + i, GUILayout.Width(90));
                    EditorGUILayout.LabelField(s.item.DisplayName + " x" + s.amount);
                    if (GUILayout.Button("Usar", GUILayout.Width(50))) inv.UseItem(i);
                    if (GUILayout.Button("Tirar", GUILayout.Width(50))) inv.DropFromSlot(i, int.MaxValue);
                }
            }
            if (inv.MaxWeight > 0) EditorGUILayout.LabelField("Peso", inv.TotalWeight.ToString("0.##") + " / " + inv.MaxWeight);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pruebas", EditorStyles.boldLabel);
            testItem = (ItemData)EditorGUILayout.ObjectField("Objeto", testItem, typeof(ItemData), false);
            testAmount = Mathf.Max(1, EditorGUILayout.IntField("Cantidad", testAmount));
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = testItem != null;
                if (GUILayout.Button("Añadir")) inv.Add(testItem, testAmount);
                if (GUILayout.Button("Quitar")) inv.Remove(testItem, testAmount);
                GUI.enabled = true;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ordenar mochila")) inv.SortBackpack();
                if (GUILayout.Button("Vaciar")) inv.Clear();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Guardar (PlayerPrefs)")) inv.SaveToPlayerPrefs();
                if (GUILayout.Button("Cargar (PlayerPrefs)")) inv.LoadFromPlayerPrefs();
            }
            Repaint();
        }
    }
}
