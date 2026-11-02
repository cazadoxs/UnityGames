using System.Collections.Generic;
using Mecanicas.Inventario.Demo;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mecanicas.Inventario.Editor
{
    /// <summary>
    /// Genera la escena de demostración con objetos, prefabs, materiales y jugador.
    /// Menú: Herramientas > Mecánicas > Inventario > Crear escena demo.
    /// Todo se crea en Assets/Mecanicas/InventarioDemo (se puede borrar sin problema).
    /// </summary>
    public static class InventoryDemoBuilder
    {
        private const string Root = "Assets/Mecanicas/InventarioDemo";

        [MenuItem("Herramientas/Mecánicas/Inventario/Crear escena demo", priority = 10)]
        public static void BuildDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(Root);
            EnsureFolder(Root + "/Objetos");
            EnsureFolder(Root + "/Prefabs");
            EnsureFolder(Root + "/Materiales");

            // ---------- Objetos (ItemData) ----------
            var manzana = CreateItem<FoodItemData>("Manzana", "manzana", "Manzana",
                "Una manzana roja y crujiente.", "Comida", new Color(0.85f, 0.15f, 0.12f), 10, 0.2f, true, true,
                PrimitiveType.Sphere, Vector3.one * 0.18f, 1.2f);
            manzana.EditorSetNutrition(25f);
            EditorUtility.SetDirty(manzana);

            var piedra = CreateItem<ItemData>("Piedra", "piedra", "Piedra",
                "Una piedra normal y corriente. Pesa bastante.", "Materiales", new Color(0.5f, 0.5f, 0.52f), 20, 1f, false, false,
                PrimitiveType.Cube, new Vector3(0.22f, 0.16f, 0.2f), 1f);

            var moneda = CreateItem<ItemData>("Moneda", "moneda", "Moneda",
                "Moneda de oro. Se apila hasta 99.", "Valiosos", new Color(1f, 0.8f, 0.15f), 99, 0.01f, false, false,
                PrimitiveType.Cylinder, new Vector3(0.12f, 0.01f, 0.12f), 2f);

            var llave = CreateItem<ItemData>("Llave", "llave_oxidada", "Llave oxidada",
                "Abre alguna puerta... (úsala con Inventory.Contains / TryConsume desde tu puerta).", "Llaves",
                new Color(0.75f, 0.55f, 0.25f), 1, 0.05f, false, false,
                PrimitiveType.Cube, new Vector3(0.05f, 0.02f, 0.18f), 1.5f);

            var linterna = CreateItem<ItemData>("Linterna", "linterna", "Linterna",
                "Úsala para encenderla o apagarla.", "Herramientas", new Color(0.2f, 0.45f, 0.95f), 1, 0.5f, true, false,
                PrimitiveType.Cylinder, new Vector3(0.07f, 0.12f, 0.07f), 1f);

            var botella = CreateItem<ItemData>("Botella", "botella_agua", "Botella de agua",
                "No apilable más allá de 5.", "Comida", new Color(0.3f, 0.8f, 0.95f), 5, 0.5f, false, false,
                PrimitiveType.Capsule, new Vector3(0.1f, 0.14f, 0.1f), 1f);

            var all = new List<ItemData> { manzana, piedra, moneda, llave, linterna, botella };

            string dbPath = Root + "/BaseDeDatosObjetos.asset";
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
            if (db == null) { db = ScriptableObject.CreateInstance<ItemDatabase>(); AssetDatabase.CreateAsset(db, dbPath); }
            foreach (var it in all) db.Add(it);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();

            // ---------- Escena ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var go in scene.GetRootGameObjects())
                if (go.GetComponent<Camera>() != null) Object.DestroyImmediate(go);

            var floorMat = CreateMaterial("Suelo", new Color(0.42f, 0.44f, 0.46f));
            var wallMat = CreateMaterial("Pared", new Color(0.7f, 0.68f, 0.62f));
            var woodMat = CreateMaterial("Madera", new Color(0.45f, 0.3f, 0.18f));

            var env = new GameObject("Entorno");
            var floor = Block("Suelo", env.transform, new Vector3(0, -0.05f, 0), new Vector3(20, 0.1f, 20), floorMat);
            floor.isStatic = true;
            Block("Pared N", env.transform, new Vector3(0, 1.5f, 10), new Vector3(20, 3, 0.3f), wallMat);
            Block("Pared S", env.transform, new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.3f), wallMat);
            Block("Pared E", env.transform, new Vector3(10, 1.5f, 0), new Vector3(0.3f, 3, 20), wallMat);
            Block("Pared O", env.transform, new Vector3(-10, 1.5f, 0), new Vector3(0.3f, 3, 20), wallMat);
            Block("Mesa 1", env.transform, new Vector3(0, 0.4f, 4), new Vector3(2.4f, 0.8f, 1f), woodMat);
            Block("Mesa 2", env.transform, new Vector3(-4, 0.4f, 1), new Vector3(1f, 0.8f, 2.4f), woodMat);
            Block("Estanteria", env.transform, new Vector3(4, 0.6f, 2), new Vector3(1f, 1.2f, 3f), woodMat);

            // ---------- Jugador ----------
            var player = new GameObject("Jugador");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1f, -3f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = Vector3.zero;

            var camGo = new GameObject("Camara");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0.7f, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            camGo.AddComponent<AudioListener>();

            var hand = new GameObject("Mano");
            hand.transform.SetParent(camGo.transform, false);
            hand.transform.localPosition = new Vector3(0.32f, -0.28f, 0.55f);
            hand.transform.localRotation = Quaternion.Euler(10f, -15f, 0f);

            var flashlight = new GameObject("LuzLinterna");
            flashlight.transform.SetParent(camGo.transform, false);
            var spot = flashlight.AddComponent<Light>();
            spot.type = LightType.Spot; spot.range = 18f; spot.spotAngle = 45f; spot.intensity = 2.5f;
            flashlight.SetActive(false);

            var fpc = player.AddComponent<DemoFirstPersonController>();
            Set(fpc, "cameraPivot", camGo.transform);

            var inv = player.AddComponent<Inventory>();
            Set(inv, "capacity", 24);
            Set(inv, "hotbarSize", 6);
            Set(inv, "maxWeight", 30f);
            Set(inv, "database", db);
            Set(inv, "dropOrigin", camGo.transform);
            var so = new SerializedObject(inv);
            var start = so.FindProperty("startingItems");
            start.arraySize = 2;
            start.GetArrayElementAtIndex(0).FindPropertyRelative("item").objectReferenceValue = linterna;
            start.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 1;
            start.GetArrayElementAtIndex(1).FindPropertyRelative("item").objectReferenceValue = manzana;
            start.GetArrayElementAtIndex(1).FindPropertyRelative("amount").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();

            var interactor = player.AddComponent<InventoryInteractor>();
            Set(interactor, "inventory", inv);
            Set(interactor, "viewCamera", cam);
            var hotbar = player.AddComponent<InventoryHotbar>();
            Set(hotbar, "inventory", inv);
            Set(hotbar, "handAnchor", hand.transform);
            var saver = player.AddComponent<InventorySaver>();
            Set(saver, "inventory", inv);
            player.AddComponent<DemoHunger>();
            var toggle = player.AddComponent<DemoToggleActive>();
            Set(toggle, "target", flashlight);

            // ---------- Interfaz ----------
            var uiGo = new GameObject("InventarioUI");
            var ui = uiGo.AddComponent<InventoryUI>();
            Set(ui, "inventory", inv);
            Set(ui, "hotbar", hotbar);
            Set(ui, "interactor", interactor);
            Set(ui, "saver", saver);
            uiGo.AddComponent<DemoHelp>();

            // Linterna: al usarla se enciende/apaga la luz y sale un mensaje (sin programar: UnityEvents).
            var listenerGo = new GameObject("AlUsarLinterna");
            var listener = listenerGo.AddComponent<ItemUseListener>();
            Set(listener, "item", linterna);
            Set(listener, "inventory", inv);
            UnityEventTools.AddPersistentListener(listener.onUsed, toggle.Toggle);
            UnityEventTools.AddStringPersistentListener(listener.onUsed, ui.ShowMessage, "Linterna encendida / apagada");

            // ---------- Objetos en el mundo ----------
            var items = new GameObject("Objetos recogibles");
            Place(manzana, 3, new Vector3(-0.6f, 0.95f, 4f), items.transform);
            Place(manzana, 1, new Vector3(0.6f, 0.95f, 4f), items.transform);
            Place(llave, 1, new Vector3(0f, 0.9f, 4.2f), items.transform);
            Place(botella, 2, new Vector3(-4f, 0.95f, 0.5f), items.transform);
            Place(moneda, 10, new Vector3(-4f, 0.85f, 1.6f), items.transform);
            Place(piedra, 5, new Vector3(4f, 1.3f, 1.2f), items.transform);
            Place(piedra, 5, new Vector3(4f, 1.3f, 2.8f), items.transform);
            for (int i = 0; i < 6; i++)
            {
                var coin = Place(moneda, 5, new Vector3(-3f + i * 1.2f, 0.1f, -6f), items.transform);
                Set(coin, "pickupOnTouch", true); // estas se recogen solo con pasar por encima
            }

            string scenePath = Root + "/InventarioDemo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = player;
            EditorUtility.DisplayDialog("Inventario",
                "Escena demo creada en " + scenePath + ".\n\nPulsa Play. Controles: WASD, ratón, E recoger, Tab inventario, 1-6 barra rápida, G tirar.",
                "Vale");
        }

        // ================== Ayudantes ==================
        private static T CreateItem<T>(string file, string id, string displayName, string description, string category,
            Color color, int maxStack, float weight, bool usable, bool consume,
            PrimitiveType shape, Vector3 scale, float heldScale) where T : ItemData
        {
            string path = Root + "/Objetos/" + file + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<T>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.EditorSetup(id, displayName, description, category, color, maxStack, weight, usable, consume);

            var mat = CreateMaterial(file, color);
            var go = GameObject.CreatePrimitive(shape);
            go.name = file;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.05f, weight);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            go.AddComponent<WorldItem>().Setup(item, 1);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + file + ".prefab");
            Object.DestroyImmediate(go);

            item.EditorSetPrefabs(prefab, null, heldScale);
            EditorUtility.SetDirty(item);
            return item;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = Root + "/Materiales/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(FindLitShader());
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Shader FindLitShader()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            Shader s = null;
            if (rp != null)
            {
                string n = rp.GetType().Name;
                if (n.Contains("Universal")) s = Shader.Find("Universal Render Pipeline/Lit");
                else if (n.Contains("HD")) s = Shader.Find("HDRP/Lit");
            }
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            return s;
        }

        private static GameObject Block(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        private static WorldItem Place(ItemData item, int amount, Vector3 pos, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(item.WorldPrefab);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.name = item.DisplayName + (amount > 1 ? " x" + amount : "");
            var wi = go.GetComponent<WorldItem>();
            Set(wi, "amount", amount);
            return wi;
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[Inventario] Campo '{field}' no encontrado en {target.GetType().Name}"); return; }
            switch (value)
            {
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Object o: p.objectReferenceValue = o; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
