using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BurgerKitchen.EditorTools
{
    /// <summary>
    /// Crea los datos de ejemplo (ingredientes, recetas, carta y jornada) como ScriptableObjects
    /// en Assets/BurgerKitchenData. Es seguro ejecutarlo varias veces: reutiliza los assets existentes
    /// y no pisa los cambios que hayas hecho.
    /// </summary>
    public static class SampleContentGenerator
    {
        public const string Root = "Assets/BurgerKitchenData";

        /// <summary>Todo lo generado, por clave, para que el constructor de cocina lo use.</summary>
        public class Content
        {
            public readonly Dictionary<string, ItemDefinition> Items = new Dictionary<string, ItemDefinition>();
            public readonly Dictionary<string, ProcessRecipe> Recipes = new Dictionary<string, ProcessRecipe>();
            public readonly Dictionary<string, ProductDefinition> Products = new Dictionary<string, ProductDefinition>();
            public MenuDefinition Menu;
            public ShiftSettings Shift;

            public ItemDefinition I(string key) => Items[key];
            public ProcessRecipe R(string key) => Recipes[key];
        }

        [MenuItem("Tools/Burger Kitchen/1. Crear datos de ejemplo", priority = 1)]
        public static void CreateFromMenu()
        {
            var c = CreateOrLoad();
            Selection.activeObject = c.Menu;
            EditorGUIUtility.PingObject(c.Menu);
            Debug.Log($"[BurgerKitchen] Datos listos en {Root}: {c.Items.Count} objetos, {c.Recipes.Count} recetas, {c.Products.Count} productos.");
        }

        public static Content CreateOrLoad()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Items");
            EnsureFolder(Root + "/Recipes");
            EnsureFolder(Root + "/Products");

            var c = new Content();
            CreateItems(c);
            CreateRecipes(c);
            CreateProducts(c);
            CreateMenuAndShift(c);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return c;
        }

        // ---------- Objetos ----------

        static void CreateItems(Content c)
        {
            var bun = new Color(0.86f, 0.62f, 0.3f);
            // Hamburguesa
            Item(c, "BunBottom", "Pan (base)", ItemTags.Bun | ItemTags.BurgerLayer, PrimitiveType.Cylinder, new Vector3(0.16f, 0.035f, 0.16f), bun, 0.035f, false);
            Item(c, "BunTop", "Pan (tapa)", ItemTags.Bun | ItemTags.BurgerLayer, PrimitiveType.Sphere, new Vector3(0.16f, 0.06f, 0.16f), bun, 0.05f, false);
            Item(c, "PattyRaw", "Carne cruda", ItemTags.Patty | ItemTags.RawFood, PrimitiveType.Cylinder, new Vector3(0.15f, 0.025f, 0.15f), new Color(0.9f, 0.35f, 0.4f), 0.025f, false);
            Item(c, "PattyCooked", "Carne a la plancha", ItemTags.Patty | ItemTags.BurgerLayer, PrimitiveType.Cylinder, new Vector3(0.15f, 0.025f, 0.15f), new Color(0.45f, 0.25f, 0.15f), 0.025f, false);
            Item(c, "PattyBurnt", "Carne quemada", ItemTags.Patty | ItemTags.Waste, PrimitiveType.Cylinder, new Vector3(0.15f, 0.025f, 0.15f), new Color(0.12f, 0.1f, 0.1f), 0.025f, true);
            Item(c, "BaconRaw", "Bacon crudo", ItemTags.Topping | ItemTags.RawFood, PrimitiveType.Cube, new Vector3(0.16f, 0.01f, 0.05f), new Color(0.95f, 0.55f, 0.55f), 0.012f, false);
            Item(c, "Bacon", "Bacon crujiente", ItemTags.Topping | ItemTags.BurgerLayer, PrimitiveType.Cube, new Vector3(0.16f, 0.01f, 0.05f), new Color(0.6f, 0.25f, 0.15f), 0.012f, false);
            Item(c, "BaconBurnt", "Bacon quemado", ItemTags.Topping | ItemTags.Waste, PrimitiveType.Cube, new Vector3(0.16f, 0.01f, 0.05f), new Color(0.12f, 0.1f, 0.1f), 0.012f, true);
            Item(c, "Cheese", "Queso", ItemTags.Topping | ItemTags.BurgerLayer, PrimitiveType.Cube, new Vector3(0.14f, 0.006f, 0.14f), new Color(1f, 0.8f, 0.15f), 0.008f, false);
            Item(c, "LettuceHead", "Lechuga entera", ItemTags.RawFood, PrimitiveType.Sphere, new Vector3(0.18f, 0.16f, 0.18f), new Color(0.45f, 0.85f, 0.35f), 0.1f, false);
            Item(c, "Lettuce", "Lechuga cortada", ItemTags.Topping | ItemTags.BurgerLayer, PrimitiveType.Cylinder, new Vector3(0.16f, 0.012f, 0.16f), new Color(0.4f, 0.9f, 0.3f), 0.012f, false);
            Item(c, "Tomato", "Tomate", ItemTags.RawFood, PrimitiveType.Sphere, new Vector3(0.09f, 0.08f, 0.09f), new Color(0.9f, 0.15f, 0.1f), 0.05f, false);
            Item(c, "TomatoSliced", "Rodajas de tomate", ItemTags.Topping | ItemTags.BurgerLayer, PrimitiveType.Cylinder, new Vector3(0.13f, 0.012f, 0.13f), new Color(0.95f, 0.25f, 0.2f), 0.012f, false);
            Item(c, "Sauce", "Salsa de la casa", ItemTags.Sauce | ItemTags.BurgerLayer, PrimitiveType.Cylinder, new Vector3(0.12f, 0.006f, 0.12f), new Color(0.95f, 0.6f, 0.4f), 0.006f, false);
            var wrap = Item(c, "Wrap", "Papel de hamburguesa", ItemTags.Packaging, PrimitiveType.Cube, new Vector3(0.26f, 0.005f, 0.26f), new Color(0.95f, 0.92f, 0.82f), 0.01f, false);
            Container(wrap, ItemTags.BurgerLayer, 9, ContainerLayout.Stack, new Vector3(0f, 0.006f, 0f));

            // Patatas
            Item(c, "FriesFrozen", "Patatas congeladas", ItemTags.Bulk | ItemTags.RawFood, PrimitiveType.Cube, new Vector3(0.22f, 0.1f, 0.18f), new Color(0.95f, 0.95f, 0.75f), 0.1f, false);
            Item(c, "FriesBatch", "Cesta de patatas fritas", ItemTags.Bulk, PrimitiveType.Cube, new Vector3(0.22f, 0.1f, 0.18f), new Color(1f, 0.8f, 0.25f), 0.1f, false);
            Item(c, "FriesBurnt", "Patatas quemadas", ItemTags.Bulk | ItemTags.Waste, PrimitiveType.Cube, new Vector3(0.22f, 0.1f, 0.18f), new Color(0.3f, 0.2f, 0.1f), 0.1f, true);
            Item(c, "FriesBoxEmpty", "Caja de patatas vacía", ItemTags.None, PrimitiveType.Cube, new Vector3(0.09f, 0.11f, 0.06f), new Color(0.85f, 0.15f, 0.15f), 0.11f, false);
            Item(c, "Fries", "Ración de patatas", ItemTags.Side, PrimitiveType.Cube, new Vector3(0.09f, 0.14f, 0.06f), new Color(1f, 0.75f, 0.2f), 0.14f, false);

            // Bebidas
            var cup = new Vector3(0.09f, 0.16f, 0.09f);
            Item(c, "ShakeCupEmpty", "Vaso de batido", ItemTags.None, PrimitiveType.Cylinder, cup, new Color(0.95f, 0.95f, 0.95f), 0.16f, false);
            Item(c, "ShakeVanilla", "Batido de vainilla", ItemTags.Drink, PrimitiveType.Cylinder, cup, new Color(1f, 0.95f, 0.75f), 0.16f, false);
            Item(c, "ShakeChocolate", "Batido de chocolate", ItemTags.Drink, PrimitiveType.Cylinder, cup, new Color(0.45f, 0.27f, 0.15f), 0.16f, false);
            Item(c, "ShakeStrawberry", "Batido de fresa", ItemTags.Drink, PrimitiveType.Cylinder, cup, new Color(1f, 0.55f, 0.7f), 0.16f, false);
            Item(c, "SodaCupEmpty", "Vaso de refresco", ItemTags.None, PrimitiveType.Cylinder, cup, new Color(0.8f, 0.85f, 1f), 0.16f, false);
            Item(c, "SodaCola", "Refresco de cola", ItemTags.Drink, PrimitiveType.Cylinder, cup, new Color(0.3f, 0.12f, 0.08f), 0.16f, false);
            Item(c, "SodaOrange", "Refresco de naranja", ItemTags.Drink, PrimitiveType.Cylinder, cup, new Color(1f, 0.55f, 0.1f), 0.16f, false);

            // Helados
            Item(c, "ConeEmpty", "Cucurucho", ItemTags.None, PrimitiveType.Cylinder, new Vector3(0.06f, 0.12f, 0.06f), new Color(0.85f, 0.65f, 0.35f), 0.12f, false);
            Item(c, "ConeIceCream", "Cucurucho de helado", ItemTags.Dessert, PrimitiveType.Capsule, new Vector3(0.07f, 0.2f, 0.07f), new Color(1f, 0.97f, 0.9f), 0.2f, false);
            Item(c, "DessertCupEmpty", "Tarrina", ItemTags.None, PrimitiveType.Cylinder, new Vector3(0.11f, 0.06f, 0.11f), new Color(0.7f, 0.85f, 1f), 0.06f, false);
            var sundae = Item(c, "SoftServeCup", "Tarrina de helado", ItemTags.Dessert, PrimitiveType.Cylinder, new Vector3(0.11f, 0.1f, 0.11f), new Color(1f, 0.97f, 0.9f), 0.1f, false);
            Container(sundae, ItemTags.DessertTopping, 2, ContainerLayout.Stack, new Vector3(0f, 0.1f, 0f));
            Item(c, "ChocoSyrup", "Sirope de chocolate", ItemTags.DessertTopping, PrimitiveType.Cylinder, new Vector3(0.09f, 0.01f, 0.09f), new Color(0.3f, 0.15f, 0.08f), 0.01f, false);
            Item(c, "Cookie", "Galleta troceada", ItemTags.DessertTopping, PrimitiveType.Cylinder, new Vector3(0.08f, 0.015f, 0.08f), new Color(0.25f, 0.2f, 0.18f), 0.015f, false);

            // Bandeja
            var tray = Item(c, "Tray", "Bandeja", ItemTags.Tray, PrimitiveType.Cube, new Vector3(0.45f, 0.02f, 0.32f), new Color(0.75f, 0.2f, 0.2f), 0.02f, false);
            tray.getsDirtyWhenDropped = false;
            Container(tray, ItemTags.Packaging | ItemTags.Side | ItemTags.Drink | ItemTags.Dessert, 4, ContainerLayout.Grid, new Vector3(0f, 0.02f, 0f));
            tray.container.gridSpacing = 0.16f;
            tray.container.gridColumns = 2;
            EditorUtility.SetDirty(tray);
        }

        static ItemDefinition Item(Content c, string key, string displayName, ItemTags tags, PrimitiveType shape,
            Vector3 size, Color color, float stackHeight, bool failed)
        {
            var path = $"{Root}/Items/Item_{key}.asset";
            var def = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ItemDefinition>();
                def.displayName = displayName;
                def.tags = tags;
                def.placeholderShape = shape;
                def.placeholderSize = size;
                def.placeholderColor = color;
                def.stackHeight = stackHeight;
                def.isFailedState = failed;
                AssetDatabase.CreateAsset(def, path);
            }
            c.Items[key] = def;
            return def;
        }

        static void Container(ItemDefinition def, ItemTags accepts, int capacity, ContainerLayout layout, Vector3 offset)
        {
            if (def.container != null && def.container.isContainer) return; // ya configurado
            def.container = new ContainerSettings
            {
                isContainer = true, acceptedTags = accepts, capacity = capacity, layout = layout, contentOffset = offset,
            };
            EditorUtility.SetDirty(def);
        }

        // ---------- Recetas ----------

        static void CreateRecipes(Content c)
        {
            Recipe(c, "GrillPatty", "Cocinar", "PattyRaw", "PattyCooked", 9f, false, 0.5f);
            Recipe(c, "BurnPatty", "Cocinar", "PattyCooked", "PattyBurnt", 8f, true, 0f);
            Recipe(c, "GrillBacon", "Cocinar", "BaconRaw", "Bacon", 5f, false, 0f);
            Recipe(c, "BurnBacon", "Cocinar", "Bacon", "BaconBurnt", 7f, true, 0f);
            Recipe(c, "ChopLettuce", "Cortar", "LettuceHead", "Lettuce", 1.5f, false, 0f);
            Recipe(c, "SliceTomato", "Cortar", "Tomato", "TomatoSliced", 1.5f, false, 0f);
            Recipe(c, "FryFries", "Freír", "FriesFrozen", "FriesBatch", 8f, false, 0f);
            Recipe(c, "BurnFries", "Freír", "FriesBatch", "FriesBurnt", 12f, true, 0f);
            Recipe(c, "ShakeVanilla", "Batir", "ShakeCupEmpty", "ShakeVanilla", 4f, false, 0f);
            Recipe(c, "ShakeChocolate", "Batir", "ShakeCupEmpty", "ShakeChocolate", 4f, false, 0f);
            Recipe(c, "ShakeStrawberry", "Batir", "ShakeCupEmpty", "ShakeStrawberry", 4f, false, 0f);
            Recipe(c, "SodaCola", "Servir", "SodaCupEmpty", "SodaCola", 2.5f, false, 0f);
            Recipe(c, "SodaOrange", "Servir", "SodaCupEmpty", "SodaOrange", 2.5f, false, 0f);
            Recipe(c, "FillCone", "Servir helado", "ConeEmpty", "ConeIceCream", 2f, false, 0f);
            Recipe(c, "FillCup", "Servir helado", "DessertCupEmpty", "SoftServeCup", 2.5f, false, 0f);
        }

        static void Recipe(Content c, string key, string action, string input, string output, float duration, bool overcook, float flipAt)
        {
            var path = $"{Root}/Recipes/Recipe_{key}.asset";
            var r = AssetDatabase.LoadAssetAtPath<ProcessRecipe>(path);
            if (r == null)
            {
                r = ScriptableObject.CreateInstance<ProcessRecipe>();
                r.actionLabel = action;
                r.input = c.I(input);
                r.output = c.I(output);
                r.duration = duration;
                r.isOvercook = overcook;
                r.flipAt = flipAt;
                AssetDatabase.CreateAsset(r, path);
            }
            c.Recipes[key] = r;
        }

        // ---------- Carta ----------

        static void CreateProducts(Content c)
        {
            Burger(c, "Classic", "Hamburguesa sencilla", 5, "BunBottom", "PattyCooked", "BunTop");
            Burger(c, "Cheese", "Hamburguesa con queso", 6, "BunBottom", "PattyCooked", "Cheese", "BunTop");
            Burger(c, "Garden", "Hamburguesa de la huerta", 8, "BunBottom", "Sauce", "PattyCooked", "Cheese", "Lettuce", "TomatoSliced", "BunTop");
            Burger(c, "DoubleBacon", "Doble bacon", 10, "BunBottom", "PattyCooked", "Cheese", "Bacon", "PattyCooked", "Cheese", "BunTop");

            Single(c, "Fries", "Patatas fritas", MenuCourse.Side, 3, "Fries", 5f);
            Single(c, "ShakeVanilla", "Batido de vainilla", MenuCourse.Drink, 4, "ShakeVanilla", 5f);
            Single(c, "ShakeChocolate", "Batido de chocolate", MenuCourse.Drink, 4, "ShakeChocolate", 5f);
            Single(c, "ShakeStrawberry", "Batido de fresa", MenuCourse.Drink, 4, "ShakeStrawberry", 5f);
            Single(c, "SodaCola", "Refresco de cola", MenuCourse.Drink, 2, "SodaCola", 0f);
            Single(c, "SodaOrange", "Refresco de naranja", MenuCourse.Drink, 2, "SodaOrange", 0f);
            Single(c, "Cone", "Cucurucho", MenuCourse.Dessert, 2, "ConeIceCream", 0f);
            Sundae(c, "SundaeChoco", "Tarrina con chocolate", 4, "ChocoSyrup");
            Sundae(c, "SundaeCookie", "Tarrina con galleta", 4, "Cookie");
            Sundae(c, "SundaeDeluxe", "Tarrina completa", 5, "ChocoSyrup", "Cookie");
        }

        static ProductDefinition Product(Content c, string key, string name, MenuCourse course, int price, out bool created)
        {
            var path = $"{Root}/Products/Product_{key}.asset";
            var p = AssetDatabase.LoadAssetAtPath<ProductDefinition>(path);
            created = p == null;
            if (created)
            {
                p = ScriptableObject.CreateInstance<ProductDefinition>();
                p.displayName = name;
                p.course = course;
                p.price = price;
                AssetDatabase.CreateAsset(p, path);
            }
            c.Products[key] = p;
            return p;
        }

        static void Burger(Content c, string key, string name, int price, params string[] layers)
        {
            var p = Product(c, key, name, MenuCourse.Main, price, out bool created);
            if (!created) return;
            p.matchMode = ProductMatchMode.Assembly;
            p.item = c.I("Wrap");
            p.orderRule = AssemblyOrderRule.FirstAndLastFixed;
            p.extraPatience = 10f + layers.Length * 3f;
            p.ingredients = new List<ItemDefinition>();
            foreach (var l in layers) p.ingredients.Add(c.I(l));
            EditorUtility.SetDirty(p);
        }

        static void Single(Content c, string key, string name, MenuCourse course, int price, string item, float extraPatience)
        {
            var p = Product(c, key, name, course, price, out bool created);
            if (!created) return;
            p.matchMode = ProductMatchMode.SingleItem;
            p.item = c.I(item);
            p.extraPatience = extraPatience;
            EditorUtility.SetDirty(p);
        }

        static void Sundae(Content c, string key, string name, int price, params string[] toppings)
        {
            var p = Product(c, key, name, MenuCourse.Dessert, price, out bool created);
            if (!created) return;
            p.matchMode = ProductMatchMode.Assembly;
            p.item = c.I("SoftServeCup");
            p.orderRule = AssemblyOrderRule.AnyOrder;
            p.extraPatience = 5f;
            p.ingredients = new List<ItemDefinition>();
            foreach (var t in toppings) p.ingredients.Add(c.I(t));
            EditorUtility.SetDirty(p);
        }

        static void CreateMenuAndShift(Content c)
        {
            var menuPath = Root + "/Menu_Principal.asset";
            c.Menu = AssetDatabase.LoadAssetAtPath<MenuDefinition>(menuPath);
            if (c.Menu == null)
            {
                c.Menu = ScriptableObject.CreateInstance<MenuDefinition>();
                void Add(string key, float weight, int day) =>
                    c.Menu.entries.Add(new MenuEntry { product = c.Products[key], weight = weight, unlockDay = day });
                Add("Classic", 3f, 1);
                Add("Cheese", 3f, 1);
                Add("Garden", 2f, 2);
                Add("DoubleBacon", 1.5f, 3);
                Add("Fries", 1f, 1);
                Add("SodaCola", 2f, 1);
                Add("SodaOrange", 1f, 1);
                Add("ShakeVanilla", 1f, 1);
                Add("ShakeChocolate", 1f, 1);
                Add("ShakeStrawberry", 1f, 2);
                Add("Cone", 2f, 1);
                Add("SundaeChoco", 1f, 2);
                Add("SundaeCookie", 1f, 2);
                Add("SundaeDeluxe", 0.7f, 3);
                AssetDatabase.CreateAsset(c.Menu, menuPath);
            }

            var shiftPath = Root + "/Shift_Normal.asset";
            c.Shift = AssetDatabase.LoadAssetAtPath<ShiftSettings>(shiftPath);
            if (c.Shift == null)
            {
                c.Shift = ScriptableObject.CreateInstance<ShiftSettings>();
                AssetDatabase.CreateAsset(c.Shift, shiftPath);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
