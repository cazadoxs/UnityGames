using System.Collections.Generic;
using System.IO;
using Mecanicas.Construccion.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mecanicas.Construccion.Editor
{
    /// <summary>
    /// Crea la escena de demostración con todo montado: suelo, una cuadrícula, obstáculos, una zona
    /// prohibida, el jugador en primera persona, el BuildSystem con cuatro objetos y la leyenda.
    /// Menú: Herramientas &gt; Mecánicas &gt; Construcción &gt; Crear escena de demostración.
    /// </summary>
    public static class ConstruccionDemoCreator
    {
        private const string MenuPath = "Herramientas/Mecánicas/Construcción/Crear escena de demostración";

        [MenuItem(MenuPath, priority = 0)]
        public static void CreateDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string root = FindModuleRoot();
            string outDir = root + "/Demo/Generado";
            EnsureFolder(root + "/Demo", "Generado");
            EnsureFolder(outDir, "Prefabs");
            EnsureFolder(outDir, "Objetos");
            EnsureFolder(outDir, "Materiales");

            // ---------- Materiales ----------
            Material wood = CreateLitMaterial(outDir, "Madera", new Color(0.62f, 0.43f, 0.25f));
            Material stone = CreateLitMaterial(outDir, "Piedra", new Color(0.55f, 0.57f, 0.6f));
            Material roof = CreateLitMaterial(outDir, "Tejado", new Color(0.7f, 0.25f, 0.2f));
            Material lamp = CreateLitMaterial(outDir, "Lampara", new Color(1f, 0.9f, 0.55f));
            Material grass = CreateLitMaterial(outDir, "Cesped", new Color(0.33f, 0.55f, 0.28f));
            Material rock = CreateLitMaterial(outDir, "Roca", new Color(0.4f, 0.38f, 0.36f));

            // ---------- Prefabs (hechos con primitivas) ----------
            GameObject cratePrefab = SavePrefab(outDir, "Caja", BuildCrate(wood));
            GameObject towerPrefab = SavePrefab(outDir, "Torre", BuildTower(stone, roof));
            GameObject housePrefab = SavePrefab(outDir, "Casita", BuildHouse(wood, roof));
            GameObject lampPrefab = SavePrefab(outDir, "Farola", BuildLamp(stone, lamp));

            // ---------- Fichas construibles ----------
            BuildableDefinition crate = CreateDefinition(outDir, "Caja", "Caja de madera", cratePrefab, new Vector2Int(1, 1), AllowedPlacementModes.Both, 20, "Ocupa 1 casilla. Se puede apilar en modo libre.");
            BuildableDefinition tower = CreateDefinition(outDir, "Torre", "Torre", towerPrefab, new Vector2Int(1, 1), AllowedPlacementModes.Grid, 60, "Solo en cuadrícula.");
            BuildableDefinition house = CreateDefinition(outDir, "Casita", "Casita", housePrefab, new Vector2Int(2, 3), AllowedPlacementModes.Both, 120, "Ocupa 2x3 casillas; gírala para ver cómo cambia la huella.");
            BuildableDefinition lampDef = CreateDefinition(outDir, "Farola", "Farola", lampPrefab, new Vector2Int(1, 1), AllowedPlacementModes.Free, 10, "Solo en modo libre.");
            lampDef.freeRotationStep = 45f;
            EditorUtility.SetDirty(lampDef);

            BuildLegendConfig legendConfig = AssetDatabase.LoadAssetAtPath<BuildLegendConfig>(outDir + "/Objetos/LeyendaConstruccion.asset");
            if (legendConfig == null)
            {
                legendConfig = ScriptableObject.CreateInstance<BuildLegendConfig>();
                legendConfig.ResetToDefaults();
                legendConfig.entries.Insert(0, new BuildLegendConfig.LegendEntry { text = "Monedas: <b>{dinero}</b>", showWhen = BuildLegendConfig.Condition.Always });
                AssetDatabase.CreateAsset(legendConfig, outDir + "/Objetos/LeyendaConstruccion.asset");
            }

            // ---------- Escena ----------
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.GetComponent<Camera>() != null) Object.DestroyImmediate(go);
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Suelo";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<Renderer>().sharedMaterial = grass;

            // Rampa para probar la inclinación máxima en modo libre.
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Rampa (40°, demasiado inclinada)";
            ramp.transform.position = new Vector3(-12f, 1f, 6f);
            ramp.transform.rotation = Quaternion.Euler(-40f, 0f, 0f);
            ramp.transform.localScale = new Vector3(4f, 0.3f, 6f);
            ramp.GetComponent<Renderer>().sharedMaterial = stone;

            // Cuadrícula de 9x5 casillas de 1,5 m.
            var gridGo = new GameObject("Cuadrícula de construcción");
            gridGo.transform.position = new Vector3(-6.75f, 0f, 4f);
            BuildGrid grid = gridGo.AddComponent<BuildGrid>();
            gridGo.AddComponent<BuildGridVisual>();
            var so = new SerializedObject(grid);
            so.FindProperty("width").intValue = 9;
            so.FindProperty("depth").intValue = 5;
            so.FindProperty("cellSize").floatValue = 1.5f;
            SerializedProperty blocked = so.FindProperty("blockedCells");
            blocked.arraySize = 2;
            blocked.GetArrayElementAtIndex(0).vector2IntValue = new Vector2Int(4, 2);
            blocked.GetArrayElementAtIndex(1).vector2IntValue = new Vector2Int(4, 3);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Roca encima de la cuadrícula: la detecta la comprobación física.
            GameObject gridRock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gridRock.name = "Roca en la cuadrícula";
            gridRock.transform.position = grid.CellCenterWorld(1, 3) + Vector3.up * 0.4f;
            gridRock.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);
            gridRock.GetComponent<Renderer>().sharedMaterial = rock;

            // Obstáculos en la zona libre.
            for (int i = 0; i < 5; i++)
            {
                GameObject r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                r.name = "Roca " + (i + 1);
                r.transform.position = new Vector3(4f + i * 2.5f, 0.5f, -4f - (i % 2) * 3f);
                r.transform.localScale = Vector3.one * (1f + (i % 3) * 0.4f);
                r.GetComponent<Renderer>().sharedMaterial = rock;
            }

            // Zona prohibida.
            var zoneGo = new GameObject("Zona prohibida");
            zoneGo.transform.position = new Vector3(10f, 0f, 6f);
            zoneGo.AddComponent<BuildZone>();
            var zso = new SerializedObject(zoneGo.GetComponent<BuildZone>());
            zso.FindProperty("zoneType").enumValueIndex = (int)BuildZone.ZoneType.Forbidden;
            zso.FindProperty("center").vector3Value = new Vector3(0f, 1f, 0f);
            zso.FindProperty("size").vector3Value = new Vector3(6f, 2f, 6f);
            zso.ApplyModifiedPropertiesWithoutUndo();
            GameObject zoneMark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            zoneMark.name = "Marca (solo visual)";
            Object.DestroyImmediate(zoneMark.GetComponent<Collider>());
            zoneMark.transform.SetParent(zoneGo.transform, false);
            zoneMark.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            zoneMark.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            zoneMark.transform.localScale = new Vector3(6f, 6f, 1f);
            zoneMark.GetComponent<Renderer>().sharedMaterial = CreateLitMaterial(outDir, "ZonaProhibida", new Color(0.6f, 0.15f, 0.15f));

            var placedParent = new GameObject("Construido");

            // Jugador en primera persona.
            var player = new GameObject("Jugador");
            player.transform.position = new Vector3(0f, 1f, -6f);
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            player.transform.position = new Vector3(0f, 0.05f, -6f);

            var camGo = new GameObject("Cámara");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            DemoFirstPersonController fpc = player.AddComponent<DemoFirstPersonController>();
            var fso = new SerializedObject(fpc);
            fso.FindProperty("cameraPivot").objectReferenceValue = camGo.transform;
            fso.ApplyModifiedPropertiesWithoutUndo();
            player.AddComponent<DemoCrosshair>();

            // Sistema de construcción.
            BuildSystem build = player.AddComponent<BuildSystem>();
            DemoWallet wallet = player.AddComponent<DemoWallet>();
            MaxCountBuildRule rule = player.AddComponent<MaxCountBuildRule>();
            var rso = new SerializedObject(rule);
            rso.FindProperty("definition").objectReferenceValue = tower;
            rso.FindProperty("maxCount").intValue = 4;
            rso.FindProperty("failMessage").stringValue = "Máximo 4 torres";
            rso.ApplyModifiedPropertiesWithoutUndo();

            var bso = new SerializedObject(build);
            SerializedProperty cat = bso.FindProperty("catalog");
            var defs = new List<BuildableDefinition> { crate, tower, house, lampDef };
            cat.arraySize = defs.Count;
            for (int i = 0; i < defs.Count; i++) cat.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            bso.FindProperty("aimCamera").objectReferenceValue = cam;
            bso.FindProperty("placedParent").objectReferenceValue = placedParent.transform;
            bso.FindProperty("costProvider").objectReferenceValue = wallet;
            bso.ApplyModifiedPropertiesWithoutUndo();

            // Leyenda.
            var legendGo = new GameObject("Leyenda de construcción");
            BuildLegend legend = legendGo.AddComponent<BuildLegend>();
            var lso = new SerializedObject(legend);
            lso.FindProperty("buildSystem").objectReferenceValue = build;
            lso.FindProperty("config").objectReferenceValue = legendConfig;
            lso.ApplyModifiedPropertiesWithoutUndo();

            var wso = new SerializedObject(wallet);
            wso.FindProperty("legend").objectReferenceValue = legend;
            wso.ApplyModifiedPropertiesWithoutUndo();

            // Cartel de ayuda (fuera del modo construcción).
            var helpGo = new GameObject("Ayuda");
            helpGo.AddComponent<DemoHelpLabel>();

            string scenePath = outDir + "/Construccion_Demo.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = player;
            EditorUtility.DisplayDialog("Construcción",
                "Escena creada en:\n" + scenePath + "\n\nPulsa Play y luego B para entrar en el modo construcción.", "Vale");
        }

        // ---------------- Prefabs de ejemplo ----------------

        private static GameObject BuildCrate(Material wood)
        {
            GameObject root = new GameObject("Caja");
            GameObject body = Child(root, PrimitiveType.Cube, "Cuerpo", new Vector3(0f, 0.5f, 0f), new Vector3(1f, 1f, 1f), wood);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = Vector3.one;
            return root;
        }

        private static GameObject BuildTower(Material stone, Material roofMat)
        {
            GameObject root = new GameObject("Torre");
            Child(root, PrimitiveType.Cylinder, "Fuste", new Vector3(0f, 1.25f, 0f), new Vector3(1f, 1.25f, 1f), stone);
            Child(root, PrimitiveType.Cube, "Almena", new Vector3(0f, 2.65f, 0f), new Vector3(1.25f, 0.3f, 1.25f), roofMat);
            return root;
        }

        private static GameObject BuildHouse(Material wood, Material roofMat)
        {
            // Huella de 2x3 casillas de 1,5 m = 3 x 4,5 m.
            GameObject root = new GameObject("Casita");
            Child(root, PrimitiveType.Cube, "Paredes", new Vector3(0f, 1f, 0f), new Vector3(2.8f, 2f, 4.3f), wood);
            GameObject r = Child(root, PrimitiveType.Cube, "Tejado", new Vector3(0f, 2.35f, 0f), new Vector3(3f, 0.7f, 4.5f), roofMat);
            Object.DestroyImmediate(r.GetComponent<Collider>());
            Child(root, PrimitiveType.Cube, "Puerta", new Vector3(0f, 0.7f, -2.16f), new Vector3(0.8f, 1.4f, 0.05f), roofMat);
            return root;
        }

        private static GameObject BuildLamp(Material stone, Material light)
        {
            GameObject root = new GameObject("Farola");
            Child(root, PrimitiveType.Cylinder, "Poste", new Vector3(0f, 1.2f, 0f), new Vector3(0.15f, 1.2f, 0.15f), stone);
            GameObject bulb = Child(root, PrimitiveType.Sphere, "Luz", new Vector3(0f, 2.5f, 0f), Vector3.one * 0.4f, light);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            Light l = bulb.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 6f;
            l.intensity = 1.5f;
            l.color = new Color(1f, 0.85f, 0.6f);
            return root;
        }

        private static GameObject Child(GameObject parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        // ---------------- Utilidades de assets ----------------

        private static string FindModuleRoot()
        {
            string[] guids = AssetDatabase.FindAssets("ConstruccionDemoCreator t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("/Editor/ConstruccionDemoCreator.cs")) continue;
                return Path.GetDirectoryName(Path.GetDirectoryName(path)).Replace('\\', '/');
            }
            return "Assets/Mecanicas/Construccion";
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string p = Path.GetDirectoryName(parent).Replace('\\', '/');
                EnsureFolder(p, Path.GetFileName(parent));
            }
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }

        private static Material CreateLitMaterial(string outDir, string name, Color color)
        {
            string path = outDir + "/Materiales/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = null;
            RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
            if (rp != null && rp.defaultMaterial != null) shader = rp.defaultMaterial.shader;
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader) { name = name };
            BuildVisualUtility.SetColor(m, color);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static GameObject SavePrefab(string outDir, string name, GameObject instance)
        {
            string path = outDir + "/Prefabs/" + name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static BuildableDefinition CreateDefinition(string outDir, string file, string displayName, GameObject prefab,
            Vector2Int size, AllowedPlacementModes modes, int cost, string description)
        {
            string path = outDir + "/Objetos/" + file + ".asset";
            BuildableDefinition def = AssetDatabase.LoadAssetAtPath<BuildableDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<BuildableDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.displayName = displayName;
            def.description = description;
            def.prefab = prefab;
            def.gridSize = size;
            def.allowedModes = modes;
            def.cost = cost;
            EditorUtility.SetDirty(def);
            return def;
        }
    }
}
