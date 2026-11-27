using System.IO;
using UnityEditor;
using UnityEngine;

namespace Imprenta.EditorTools
{
    /// <summary>
    /// Humanos MPFB: importa las texturas de Assets/Resources/Humanos (las *_normal como mapa de normales) y crea
    /// un material URP/Lit por línea de materiales.txt en Assets/Resources/Humanos/Mats (así sus variantes entran en el build).
    /// Línea: nombre|difusa|normal|alfa(0/1)|r,g,b|tipo (piel, ropa, pelo, ojos)
    /// </summary>
    public class HumanMaterials : AssetPostprocessor
    {
        const string Dir = "Assets/Resources/Humanos";

        void OnPreprocessTexture()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/Humanos/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = 2048;
            if (Path.GetFileNameWithoutExtension(p).ToLower().EndsWith("_normal")) ti.textureType = TextureImporterType.NormalMap;
            else ti.alphaIsTransparency = true;
        }

        void OnPreprocessModel()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/Models/H")) return;
            var mi = (ModelImporter)assetImporter;
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.importAnimation = false;
            mi.importCameras = false; mi.importLights = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        [MenuItem("Imprenta/Crear materiales de humanos")]
        public static void Build()
        {
            string mf = Dir + "/materiales.txt";
            if (!File.Exists(mf)) return;
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Dir + "/Mats");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            int made = 0;
            foreach (var raw in File.ReadAllLines(mf))
            {
                var f = raw.Trim().Split('|');
                if (f.Length < 6) continue;
                string matPath = Dir + "/Mats/" + f[0] + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                bool isNew = mat == null;
                if (isNew) mat = new Material(lit);
                mat.enableInstancing = true;
                Texture2D Tex(string res)
                {
                    if (string.IsNullOrEmpty(res)) return null;
                    foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".tga" })
                    {
                        var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/" + res + ext);
                        if (t != null) return t;
                    }
                    return null;
                }
                var d = Tex(f[1]);
                var n = Tex(f[2]);
                var c = f[4].Split(',');
                var col = new Color(float.Parse(c[0], System.Globalization.CultureInfo.InvariantCulture),
                                    float.Parse(c[1], System.Globalization.CultureInfo.InvariantCulture),
                                    float.Parse(c[2], System.Globalization.CultureInfo.InvariantCulture), 1);
                if (d != null) mat.SetTexture("_BaseMap", d);
                mat.SetColor("_BaseColor", d != null ? Color.white : col);
                if (n != null)
                {
                    mat.SetTexture("_BumpMap", n); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", 1f);
                }
                string kind = f[5];
                mat.SetFloat("_Smoothness", kind == "piel" ? 0.38f : kind == "ojos" ? 0.85f : kind == "pelo" ? 0.3f : 0.12f);
                mat.SetFloat("_Metallic", 0);
                if (f[3] == "1")
                {
                    mat.SetFloat("_AlphaClip", 1); mat.EnableKeyword("_ALPHATEST_ON");
                    mat.SetFloat("_Cutoff", 0.4f);
                    mat.SetFloat("_Cull", 0); // pelo de tarjetas: doble cara
                    mat.renderQueue = 2450;
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                }
                if (isNew) AssetDatabase.CreateAsset(mat, matPath);
                else EditorUtility.SetDirty(mat);
                made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Imprenta] Materiales de humanos: " + made);
        }
    }
}
