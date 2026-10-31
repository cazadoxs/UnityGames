using UnityEngine;

namespace BurgerKitchen
{
    public static class KitchenUtil
    {
        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        public static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var mat = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        }

        /// <summary>Crea una primitiva sin colisionador, con su base en el origen y el tamaño real indicado.</summary>
        public static GameObject CreatePlaceholder(PrimitiveType shape, Vector3 size, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = "Placeholder";
            var col = go.GetComponent<Collider>();
            if (col != null) SafeDestroy(col);

            Vector3 scale = size;
            if (shape == PrimitiveType.Cylinder || shape == PrimitiveType.Capsule) scale.y = size.y * 0.5f;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = scale;
            SetColor(go.GetComponent<Renderer>(), color);
            return go;
        }

        public static string Colorize(string text, Color c)
            => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";
    }
}
