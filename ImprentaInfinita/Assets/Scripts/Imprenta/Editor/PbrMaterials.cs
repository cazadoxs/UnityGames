using System.IO;
using UnityEditor;
using UnityEngine;

namespace Imprenta.EditorTools
{
    /// <summary>
    /// Convierte las texturas PBR de Poly Haven (color, normal, rugosidad, metal, AO) en materiales URP/Lit
    /// en Assets/Resources/PBR/M_&lt;id&gt;.mat. URP no lee rugosidad: se empaqueta metal (R) + suavidad (A).
    /// </summary>
    public class PbrMaterials : AssetPostprocessor
    {
        const string Root = "Assets/Textures/PolyHaven";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/PolyHaven/")) return;
            var ti = (TextureImporter)assetImporter;
            string n = Path.GetFileNameWithoutExtension(assetPath);
            ti.maxTextureSize = 2048;
            if (n.EndsWith("_normal")) ti.textureType = TextureImporterType.NormalMap;
            else if (n.EndsWith("_rough") || n.EndsWith("_metal") || n.EndsWith("_ao") || n.EndsWith("_mask"))
            {
                ti.sRGBTexture = false;
                ti.isReadable = n.EndsWith("_rough") || n.EndsWith("_metal");
            }
        }

        [MenuItem("Imprenta/Crear materiales PBR")]
        public static void Build()
        {
            if (!Directory.Exists(Root)) { Debug.LogWarning("[Imprenta] No hay texturas en " + Root); return; }
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/Resources/PBR");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            int made = 0;
            foreach (var dir in Directory.GetDirectories(Root))
            {
                string id = Path.GetFileName(dir);
                string P(string kind)
                {
                    foreach (var ext in new[] { ".jpg", ".png" })
                    {
                        string p = (dir + "/" + id + "_" + kind + ext).Replace('\\', '/');
                        if (File.Exists(p)) return p;
                    }
                    return null;
                }
                var color = P("color"); if (color == null) continue;
                var mat = new Material(lit) { enableInstancing = true };
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(color));
                mat.SetColor("_BaseColor", Color.white);
                var nrm = P("normal");
                if (nrm != null) { mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(nrm)); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", 1f); }
                var ao = P("ao");
                if (ao != null) { mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ao)); mat.EnableKeyword("_OCCLUSIONMAP"); mat.SetFloat("_OcclusionStrength", 1f); }
                var mask = PackMask(dir, id, P("rough"), P("metal"));
                if (mask != null)
                {
                    mat.SetTexture("_MetallicGlossMap", mask);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetFloat("_Smoothness", 1f);
                    mat.SetFloat("_Metallic", 1f);
                    mat.SetFloat("_SmoothnessTextureChannel", 0);
                }
                string matPath = "Assets/Resources/PBR/M_" + id + ".mat";
                AssetDatabase.DeleteAsset(matPath);
                AssetDatabase.CreateAsset(mat, matPath);
                made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Imprenta] Materiales PBR creados: " + made);
        }

        static Texture2D PackMask(string dir, string id, string roughPath, string metalPath)
        {
            if (roughPath == null) return null;
            string outPath = (dir + "/" + id + "_mask.png").Replace('\\', '/');
            if (!File.Exists(outPath))
            {
                var rough = AssetDatabase.LoadAssetAtPath<Texture2D>(roughPath);
                var metal = metalPath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(metalPath) : null;
                if (rough == null || !rough.isReadable) { Debug.LogWarning("[Imprenta] Rugosidad no legible: " + roughPath); return null; }
                int w = rough.width, h = rough.height;
                var r = rough.GetPixels();
                Color[] m = metal != null && metal.isReadable && metal.width == w && metal.height == h ? metal.GetPixels() : null;
                var px = new Color[r.Length];
                for (int i = 0; i < r.Length; i++)
                {
                    float met = m != null ? m[i].r : 0f;
                    px[i] = new Color(met, 0, 0, 1f - r[i].r);
                }
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
                tex.SetPixels(px); tex.Apply();
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(outPath);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(outPath);
        }
    }
}
