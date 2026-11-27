using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>Ayudas para construir el prototipo gris con primitivas y materiales URP/Lit.</summary>
    public static class Build
    {
        static Material baseMat;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Font mono;

        public static Material Mat(Color c, float smooth = 0.3f, float metal = 0f, Color? emission = null)
        {
            string k = c.ToString() + smooth + metal + emission;
            if (cache.TryGetValue(k, out var m)) return m;
            if (baseMat == null)
            {
                baseMat = Resources.Load<Material>("M_ImprentaBase");
                if (baseMat == null) baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
            m = new Material(baseMat);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            m.enableInstancing = true;
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            cache[k] = m;
            return m;
        }

        static readonly Dictionary<string, Material> pbrCache = new Dictionary<string, Material>();

        /// <summary>
        /// Material PBR de Poly Haven (Assets/Resources/PBR/M_id). <paramref name="tiling"/> = repeticiones por cara.
        /// Si las texturas no están, cae a un color liso para que el prototipo siga funcionando.
        /// </summary>
        public static Material Pbr(string id, Vector2 tiling, Color fallback, Color? tint = null, float smoothScale = 1f)
        {
            string k = id + tiling + tint + smoothScale;
            if (pbrCache.TryGetValue(k, out var m)) return m;
            var src = Resources.Load<Material>("PBR/M_" + id);
            if (src == null) m = Mat(fallback, 0.3f);
            else
            {
                m = new Material(src);
                m.SetTextureScale("_BaseMap", tiling);
                if (tint.HasValue) m.SetColor("_BaseColor", tint.Value);
                m.SetFloat("_Smoothness", smoothScale);
            }
            pbrCache[k] = m;
            return m;
        }

        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // Paleta (doc 5.1)
        public static readonly Color Concrete = Hex("#8d8a84");
        public static readonly Color ConcreteDark = Hex("#6b6863");
        public static readonly Color Steel = Hex("#9aa0a6");
        public static readonly Color SteelDark = Hex("#4f5459");
        public static readonly Color Oak = Hex("#7a5232");
        public static readonly Color Paper = Hex("#efece4");
        public static readonly Color PaperShade = Hex("#d9d5ca");
        public static readonly Color Beige = Hex("#cbbf9f");
        public static readonly Color Safety = Hex("#e8b923");
        public static readonly Color ButtonGreen = Hex("#3f9b4a");
        public static readonly Color Cork = Hex("#a87c4f");

        public static GameObject Box(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, bool collider = true, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Cilindro con eje Y. height = altura total en metros.</summary>
        public static GameObject Cyl(string name, Transform parent, Vector3 localPos, float diameter, float height, Material mat, bool collider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            return go;
        }

        /// <summary>
        /// Instancia un modelo de Blender de Resources/Models y le pone materiales según el nombre del material
        /// exportado (Plastico, Tela, Cromo...). Devuelve null si el modelo no está, para usar las primitivas.
        /// </summary>
        public static GameObject Model(string res, Transform parent, Vector3 localPos, Vector3? euler, System.Func<string, Material> map, bool shadows = true)
        {
            var src = Resources.Load<GameObject>("Models/" + res);
            if (src == null) return null;
            var go = Object.Instantiate(src, parent, false);
            go.name = res;
            go.transform.localPosition = localPos;
            go.transform.localRotation = euler.HasValue ? Quaternion.Euler(euler.Value) : Quaternion.identity;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name : "";
                    int cut = n.IndexOfAny(new[] { '.', ' ' });
                    if (cut > 0) n = n.Substring(0, cut);
                    mats[i] = map(n) ?? Mat(Color.gray, 0.3f);
                }
                r.sharedMaterials = mats;
                r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }

        public static Material Chrome => Mat(Hex("#c9ccd0"), 0.9f, 1f);
        public static Material Rubber => Mat(Hex("#141414"), 0.25f);
        static Material glass;
        /// <summary>Cristal transparente (si falta la variante, cristal oscuro opaco).</summary>
        public static Material Glass
        {
            get
            {
                if (glass == null)
                {
                    var src = Resources.Load<Material>("M_ImprentaGlass");
                    glass = src != null ? new Material(src) : Mat(new Color(0.12f, 0.12f, 0.14f), 0.95f, 0.1f);
                }
                return glass;
            }
        }

        public static Font MonoFont
        {
            get
            {
                if (mono == null)
                {
                    try { mono = Font.CreateDynamicFontFromOSFont(new[] { "Courier New", "Consolas", "Lucida Console" }, 64); } catch { }
                    if (mono == null) mono = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return mono;
            }
        }

        public static TextMesh Text(string name, Transform parent, Vector3 localPos, string text, float size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, Vector3? euler = null, Font font = null)
        {
            var go = Empty(name, parent, localPos, euler);
            var tm = go.AddComponent<TextMesh>();
            tm.font = font ?? MonoFont;
            tm.fontSize = 64;
            tm.characterSize = size / 64f * 10f;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.UpperLeft || anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.LowerLeft ? TextAlignment.Left : TextAlignment.Center;
            tm.richText = true;
            tm.color = color;
            tm.text = text;
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            return tm;
        }

        public static Light PointLight(Transform parent, Vector3 pos, Color c, float intensity, float range, bool shadows = false)
        {
            var go = Empty("Luz", parent, pos);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        public const int AutoColliderLayer = 2; // «Ignore Raycast»: chocan, pero no tapan lo que miras

        /// <summary>
        /// Da colisión a todo lo visible que aún no la tenga (doc: «que todo tenga colisiones»). Mallas pequeñas → MeshCollider
        /// exacto; mallas grandes → caja. Se salta textos, cristales, papel y lo que ya cuelga de un objeto con colisión.
        /// </summary>
        public static int AddColliders(Transform root)
        {
            int added = 0;
            var glass = Glass;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var go = mr.gameObject;
                if (go.GetComponent<Collider>() != null) continue;
                if (go.GetComponent<TextMesh>() != null) continue;
                if (go.GetComponentInParent<LooseSheet>() != null) continue;
                if (go.name.StartsWith("Hoja") || go.name.StartsWith("Luz") || go.name.StartsWith("Tirante")) continue;
                if (mr.sharedMaterial == glass) continue;
                var parentCol = go.transform.parent != null ? go.transform.parent.GetComponentInParent<Collider>() : null;
                if (parentCol != null && !(parentCol is MeshCollider) && parentCol.bounds.Intersects(mr.bounds)) continue;
                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var sz = mr.bounds.size;
                if (Mathf.Max(sz.x, Mathf.Max(sz.y, sz.z)) < 0.06f) continue;
                if (mf.sharedMesh.vertexCount <= 4000 && mf.sharedMesh.isReadable)
                {
                    var mc = go.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
                else go.AddComponent<BoxCollider>();
                go.layer = AutoColliderLayer;
                added++;
            }
            return added;
        }
    }
}
