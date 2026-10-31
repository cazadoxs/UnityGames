using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BurgerKitchen.EditorTools
{
    /// <summary>
    /// Monta en la escena abierta una cocina de prueba con bloques grises: todas las estaciones configuradas,
    /// el jugador en primera persona, los clientes y la interfaz. Sirve para probar ya y como referencia:
    /// cuando tengas tu escenario, mueve cada estación a su sitio y cambia el bloque por tu modelo.
    /// </summary>
    public static class GreyboxKitchenBuilder
    {
        const float CounterHeight = 0.95f;
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        static SampleContentGenerator.Content data;
        static Transform root;

        [MenuItem("Tools/Burger Kitchen/2. Montar cocina de prueba en la escena", priority = 2)]
        public static void Build()
        {
            data = SampleContentGenerator.CreateOrLoad();
            materials.Clear();

            root = new GameObject("BurgerKitchen_Greybox").transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Montar cocina de prueba");

            BuildFloor();
            var player = BuildPlayer();
            BuildBackRow();
            BuildIsland();
            var flow = BuildFrontAndCustomers();
            BuildSystems(player, flow);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("[BurgerKitchen] Cocina de prueba creada. Dale a Play y pulsa Enter para abrir el local. F1 muestra los controles.");
        }

        // ---------- Suelo y jugador ----------

        static void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Suelo (bórralo si usas tu escenario)";
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(1f, 0f, 2f);
            floor.transform.localScale = new Vector3(2.4f, 1f, 2.4f);
            Paint(floor.GetComponent<Renderer>(), new Color(0.75f, 0.75f, 0.72f));
        }

        static PlayerInteractor BuildPlayer()
        {
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam.CompareTag("MainCamera"))
                {
                    Undo.RecordObject(cam.gameObject, "Desactivar cámara");
                    cam.gameObject.SetActive(false);
                    Debug.Log($"[BurgerKitchen] Se ha desactivado la cámara '{cam.name}' para usar la del jugador.");
                }
            }

            var player = new GameObject("Player");
            player.transform.SetParent(root, false);
            player.transform.localPosition = new Vector3(0.5f, 0.05f, 0.5f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var input = player.AddComponent<PlayerInputReader>();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            pivot.localPosition = new Vector3(0f, 1.65f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(pivot, false);
            var camera = camGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();

            var hold = new GameObject("HoldPoint").transform;
            hold.SetParent(camGo.transform, false);
            hold.localPosition = new Vector3(0.28f, -0.32f, 0.6f);

            var fp = player.AddComponent<FirstPersonController>();
            fp.input = input;
            fp.cameraPivot = pivot;

            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.input = input;
            interactor.viewCamera = camera;
            interactor.holdPoint = hold;
            return interactor;
        }

        // ---------- Fila del fondo: plancha, patatas, basura ----------

        static void BuildBackRow()
        {
            float z = -3f;
            Source("Nevera: carne", new Vector3(-4.6f, 0, z), "PattyRaw", new Color(0.8f, 0.85f, 0.9f));
            Source("Nevera: bacon", new Vector3(-3.7f, 0, z), "BaconRaw", new Color(0.8f, 0.85f, 0.9f));

            var grill = Processing("Plancha", new Vector3(-2.3f, 0, z), new Vector3(1.6f, CounterHeight, 0.8f), new Color(0.25f, 0.25f, 0.28f),
                ProcessingStation.WorkMode.Automatic, ProcessingStation.StartMode.WhenPlaced,
                "GrillPatty", "BurnPatty", "GrillBacon", "BurnBacon");
            Slots(grill, new Vector3(-0.55f, CounterHeight, 0f), new Vector3(-0.18f, CounterHeight, 0f),
                new Vector3(0.18f, CounterHeight, 0f), new Vector3(0.55f, CounterHeight, 0f));

            Source("Congelador: patatas", new Vector3(-0.85f, 0, z), "FriesFrozen", new Color(0.7f, 0.85f, 1f));

            var fryer = Processing("Freidora", new Vector3(0.25f, 0, z), new Vector3(1.0f, CounterHeight, 0.8f), new Color(0.65f, 0.65f, 0.7f),
                ProcessingStation.WorkMode.Automatic, ProcessingStation.StartMode.WhenPlaced, "FryFries", "BurnFries");
            Slots(fryer, new Vector3(-0.25f, CounterHeight - 0.04f, 0f), new Vector3(0.25f, CounterHeight - 0.04f, 0f));
            var oil = fryer.gameObject.AddComponent<MachineMaintenance>();
            oil.usesBeforeService = 8;
            oil.serviceLabel = "Cambiar el aceite";
            oil.serviceDuration = 3f;

            var bulkGo = Block("Reparto de patatas", new Vector3(1.35f, 0, z), new Vector3(0.9f, CounterHeight, 0.8f), new Color(0.95f, 0.85f, 0.4f));
            var bulk = bulkGo.AddComponent<BulkPortionStation>();
            bulk.bulkInput = data.I("FriesBatch");
            bulk.portionRecipes.Add(new BulkPortionStation.PortionRecipe { emptyContainer = data.I("FriesBoxEmpty"), filledResult = data.I("Fries") });

            Source("Cajas de patatas", new Vector3(2.3f, 0, z), "FriesBoxEmpty", new Color(0.85f, 0.3f, 0.3f));

            var trash = Block("Basura", new Vector3(3.3f, 0, z), new Vector3(0.6f, 0.8f, 0.6f), new Color(0.2f, 0.3f, 0.2f));
            trash.AddComponent<TrashStation>();
        }

        // ---------- Isla central: verduras, montaje, panes ----------

        static void BuildIsland()
        {
            float z = -0.9f;
            Source("Lechugas", new Vector3(-4.3f, 0, z), "LettuceHead", new Color(0.5f, 0.75f, 0.45f));
            Source("Tomates", new Vector3(-3.5f, 0, z), "Tomato", new Color(0.85f, 0.4f, 0.35f));

            var board = Processing("Tabla de cortar", new Vector3(-2.5f, 0, z), new Vector3(1.0f, CounterHeight, 0.8f), new Color(0.75f, 0.6f, 0.4f),
                ProcessingStation.WorkMode.HoldToWork, ProcessingStation.StartMode.Manual, "ChopLettuce", "SliceTomato");
            Slots(board, new Vector3(-0.25f, CounterHeight, 0f), new Vector3(0.25f, CounterHeight, 0f));

            for (int i = 0; i < 2; i++)
            {
                var counterGo = Block("Encimera de montaje " + (i + 1), new Vector3(-1.3f + i * 1.2f, 0, z), new Vector3(1.2f, CounterHeight, 0.8f), new Color(0.85f, 0.85f, 0.85f));
                var counter = counterGo.AddComponent<CounterStation>();
                counter.stationName = "Encimera de montaje";
                Slots(counter, new Vector3(-0.3f, CounterHeight, 0f), new Vector3(0.3f, CounterHeight, 0f));
            }

            Source("Pan (base)", new Vector3(0.9f, 0, z), "BunBottom", new Color(0.9f, 0.7f, 0.45f));
            Source("Pan (tapa)", new Vector3(1.6f, 0, z), "BunTop", new Color(0.9f, 0.7f, 0.45f));
            Source("Queso", new Vector3(2.3f, 0, z), "Cheese", new Color(1f, 0.85f, 0.3f));
            Source("Salsa de la casa", new Vector3(3.0f, 0, z), "Sauce", new Color(0.95f, 0.6f, 0.4f));
            Source("Papel de hamburguesa", new Vector3(3.7f, 0, z), "Wrap", new Color(0.95f, 0.92f, 0.8f));
        }

        // ---------- Mostrador: bebidas, caja, bandejas, entrega, helados ----------

        static CustomerFlow BuildFrontAndCustomers()
        {
            float z = 1.6f;
            Source("Vasos de batido", new Vector3(-4.3f, 0, z), "ShakeCupEmpty", new Color(0.9f, 0.9f, 0.95f));
            var shaker = Processing("Batidora", new Vector3(-3.5f, 0, z), new Vector3(0.8f, CounterHeight, 0.7f), new Color(0.95f, 0.6f, 0.75f),
                ProcessingStation.WorkMode.Automatic, ProcessingStation.StartMode.Manual, "ShakeVanilla", "ShakeChocolate", "ShakeStrawberry");
            var shakeClean = shaker.gameObject.AddComponent<MachineMaintenance>();
            shakeClean.usesBeforeService = 6;
            shakeClean.serviceLabel = "Limpiar la batidora";

            Source("Vasos de refresco", new Vector3(-2.7f, 0, z), "SodaCupEmpty", new Color(0.8f, 0.85f, 1f));
            var soda = Processing("Grifo de refrescos", new Vector3(-1.8f, 0, z), new Vector3(1.0f, CounterHeight, 0.7f), new Color(0.3f, 0.45f, 0.8f),
                ProcessingStation.WorkMode.HoldToWork, ProcessingStation.StartMode.Manual, "SodaCola", "SodaOrange");
            Slots(soda, new Vector3(-0.25f, CounterHeight, 0f), new Vector3(0.25f, CounterHeight, 0f));

            var regGo = Block("Caja registradora", new Vector3(-0.8f, 0, z), new Vector3(0.9f, CounterHeight + 0.25f, 0.7f), new Color(0.2f, 0.2f, 0.25f));
            var register = regGo.AddComponent<OrderRegister>();

            Source("Bandejas", new Vector3(0.3f, 0, z), "Tray", new Color(0.75f, 0.25f, 0.25f));
            var trayCounterGo = Block("Encimera de bandejas", new Vector3(1.3f, 0, z), new Vector3(1.0f, CounterHeight, 0.7f), new Color(0.85f, 0.85f, 0.85f));
            var trayCounter = trayCounterGo.AddComponent<CounterStation>();
            trayCounter.stationName = "Encimera de bandejas";
            Slots(trayCounter, new Vector3(0f, CounterHeight, 0f));

            var pickupGo = Block("Entrega de pedidos", new Vector3(2.4f, 0, z), new Vector3(1.0f, CounterHeight, 0.7f), new Color(0.3f, 0.7f, 0.35f));
            pickupGo.AddComponent<PickupCounter>();

            Source("Cucuruchos", new Vector3(3.4f, 0, z), "ConeEmpty", new Color(0.85f, 0.65f, 0.35f));
            Source("Tarrinas", new Vector3(4.1f, 0, z), "DessertCupEmpty", new Color(0.7f, 0.85f, 1f));
            var iceCream = Processing("Máquina de helados", new Vector3(5.0f, 0, z), new Vector3(1.0f, CounterHeight + 0.6f, 0.7f), new Color(0.9f, 0.9f, 0.95f),
                ProcessingStation.WorkMode.HoldToWork, ProcessingStation.StartMode.Manual, "FillCone", "FillCup");
            Slots(iceCream, new Vector3(-0.25f, 0.6f, -0.45f), new Vector3(0.25f, 0.6f, -0.45f));
            var dripTray = Block("Bandeja de goteo", new Vector3(5.0f, 0, z - 0.45f), new Vector3(0.9f, 0.6f, 0.2f), new Color(0.6f, 0.6f, 0.65f));
            Object.DestroyImmediate(dripTray.GetComponent<BoxCollider>());
            Object.DestroyImmediate(dripTray.GetComponent<Highlightable>());
            Object.DestroyImmediate(dripTray.transform.Find("Etiqueta").gameObject);
            dripTray.transform.SetParent(iceCream.transform, true);
            var iceMaint = iceCream.gameObject.AddComponent<MachineMaintenance>();
            iceMaint.usesBeforeService = 10;
            iceMaint.breakdownChancePerUse = 0.06f;
            iceMaint.serviceLabel = "Limpiar la máquina";
            iceMaint.repairLabel = "Reparar la máquina";
            iceMaint.serviceDuration = 5f;
            iceMaint.statusOffset = new Vector3(0f, 2f, 0f);
            Source("Sirope de chocolate", new Vector3(5.9f, 0, z), "ChocoSyrup", new Color(0.4f, 0.25f, 0.15f));
            Source("Galleta troceada", new Vector3(6.6f, 0, z), "Cookie", new Color(0.35f, 0.3f, 0.28f));

            // Recorrido de clientes
            var flowGo = new GameObject("Clientes");
            flowGo.transform.SetParent(root, false);
            var flow = flowGo.AddComponent<CustomerFlow>();
            var spawner = flowGo.AddComponent<CustomerSpawner>();
            spawner.flow = flow;
            flow.spawnPoint = Point(flowGo.transform, "Entrada", new Vector3(-0.8f, 0f, 9f));
            flow.exitPoint = Point(flowGo.transform, "Salida", new Vector3(4.5f, 0f, 9f));
            flow.lookAtPoint = Point(flowGo.transform, "Mirar hacia", new Vector3(1.5f, 1f, z));
            for (int i = 0; i < 6; i++) flow.queuePoints.Add(Point(flowGo.transform, "Cola " + i, new Vector3(-0.8f, 0f, 2.5f + i * 0.9f)));
            for (int i = 0; i < 5; i++) flow.waitPoints.Add(Point(flowGo.transform, "Espera " + i, new Vector3(1.6f + i * 0.85f, 0f, 2.8f)));
            register.flow = flow;
            return flow;
        }

        static void BuildSystems(PlayerInteractor player, CustomerFlow flow)
        {
            var sys = new GameObject("Sistemas");
            sys.transform.SetParent(root, false);
            var gm = sys.AddComponent<KitchenGameManager>();
            gm.settings = data.Shift;
            gm.menu = data.Menu;
            gm.input = player.input;
            sys.AddComponent<OrderManager>();
            var hud = sys.AddComponent<KitchenHUD>();
            hud.interactor = player;
            hud.worldCamera = player.viewCamera;

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Luz");
                lightGo.transform.SetParent(root, false);
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                lightGo.AddComponent<Light>().type = LightType.Directional;
            }
        }

        // ---------- Ayudas ----------

        static GameObject Block(string name, Vector3 pos, Vector3 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;

            var vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.name = "Modelo (cámbialo por el tuyo)";
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            vis.transform.localScale = size;
            Paint(vis.GetComponent<Renderer>(), color);

            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, size.y * 0.5f, 0f);
            box.size = size;
            go.AddComponent<Highlightable>();
            Label(go.transform, name, size.y + 0.25f, pos.z < 0f);
            return go;
        }

        static ItemSource Source(string name, Vector3 pos, string itemKey, Color color)
        {
            var go = Block(name, pos, new Vector3(0.6f, CounterHeight, 0.6f), color);
            var src = go.AddComponent<ItemSource>();
            src.item = data.I(itemKey);
            src.stationName = name;
            // Una muestra del objeto encima, para verlo de lejos.
            var def = src.item;
            var sample = new GameObject("Muestra");
            sample.transform.SetParent(go.transform, false);
            sample.transform.localPosition = new Vector3(0f, CounterHeight, 0f);
            if (def.visualPrefab != null) Object.Instantiate(def.visualPrefab, sample.transform, false);
            else
            {
                var p = GameObject.CreatePrimitive(def.placeholderShape);
                Object.DestroyImmediate(p.GetComponent<Collider>());
                p.transform.SetParent(sample.transform, false);
                var s = def.placeholderSize;
                if (def.placeholderShape == PrimitiveType.Cylinder || def.placeholderShape == PrimitiveType.Capsule) s.y *= 0.5f;
                p.transform.localScale = s;
                p.transform.localPosition = new Vector3(0f, def.placeholderSize.y * 0.5f, 0f);
                Paint(p.GetComponent<Renderer>(), def.placeholderColor);
            }
            return src;
        }

        static ProcessingStation Processing(string name, Vector3 pos, Vector3 size, Color color,
            ProcessingStation.WorkMode mode, ProcessingStation.StartMode start, params string[] recipeKeys)
        {
            var go = Block(name, pos, size, color);
            var st = go.AddComponent<ProcessingStation>();
            st.stationName = name;
            st.workMode = mode;
            st.startMode = start;
            foreach (var k in recipeKeys) st.recipes.Add(data.R(k));
            st.defaultSlotOffset = new Vector3(0f, CounterHeight, 0f);
            return st;
        }

        static void Slots(Station station, params Vector3[] localPositions)
        {
            station.slotAnchors.Clear();
            for (int i = 0; i < localPositions.Length; i++)
            {
                var t = new GameObject("Hueco " + (i + 1)).transform;
                t.SetParent(station.transform, false);
                t.localPosition = localPositions[i];
                station.slotAnchors.Add(t);
            }
        }

        static Transform Point(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static void Label(Transform parent, string text, float height, bool faceForward)
        {
            var go = new GameObject("Etiqueta");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, faceForward ? 180f : 0f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = 0.05f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.black;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
        }

        static void Paint(Renderer r, Color color)
        {
            if (r == null) return;
            if (!materials.TryGetValue(color, out var mat))
            {
                var baseMat = r.sharedMaterial;
                mat = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                materials[color] = mat;
            }
            r.sharedMaterial = mat;
        }
    }
}
