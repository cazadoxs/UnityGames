using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Imprenta.EditorTools
{
    /// <summary>Calidad gráfica: sombras más lejanas y suaves, HDR y oclusión ambiental (SSAO) en el renderer de URP.</summary>
    public static class GraphicsSetup
    {
        static void Set(SerializedObject so, string prop, float v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.Log("[Imprenta] (sin propiedad " + prop + ")"); return; }
            switch (p.propertyType)
            {
                case SerializedPropertyType.Float: p.floatValue = v; break;
                case SerializedPropertyType.Integer: p.intValue = (int)v; break;
                case SerializedPropertyType.Boolean: p.boolValue = v > 0.5f; break;
                case SerializedPropertyType.Enum: p.intValue = (int)v; break;
            }
        }

        [MenuItem("Imprenta/Ajustar gráficos")]
        public static void Apply()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset"))
            {
                var a = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                var so = new SerializedObject(a);
                Set(so, "m_ShadowDistance", 60f);
                Set(so, "m_ShadowCascadeCount", 3);
                Set(so, "m_MainLightShadowmapResolution", 4096);
                Set(so, "m_SoftShadowsSupported", 1);
                Set(so, "m_SupportsHDR", 1);
                Set(so, "m_AdditionalLightShadowsSupported", 1);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }
            var ssaoType = System.Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (ssaoType == null) { Debug.LogWarning("[Imprenta] No encuentro el SSAO de URP"); return; }
            int added = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var rd = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
                if (rd == null) continue;
                ScriptableRendererFeature feat = null;
                foreach (var f in rd.rendererFeatures) if (f != null && f.GetType() == ssaoType) feat = f;
                if (feat == null)
                {
                    feat = (ScriptableRendererFeature)ScriptableObject.CreateInstance(ssaoType);
                    feat.name = "SSAO";
                    AssetDatabase.AddObjectToAsset(feat, rd);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feat, out string _, out long id);
                    var so = new SerializedObject(rd);
                    var fl = so.FindProperty("m_RendererFeatures"); fl.arraySize++;
                    fl.GetArrayElementAtIndex(fl.arraySize - 1).objectReferenceValue = feat;
                    var map = so.FindProperty("m_RendererFeatureMap"); map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    added++;
                }
                var fso = new SerializedObject(feat);
                Set(fso, "m_Settings.Intensity", 1.6f);
                Set(fso, "m_Settings.Radius", 0.45f);
                Set(fso, "m_Settings.DirectLightingStrength", 0.3f);
                Set(fso, "m_Settings.Downsample", 1);
                fso.ApplyModifiedPropertiesWithoutUndo();
                feat.SetActive(true);
                EditorUtility.SetDirty(feat); EditorUtility.SetDirty(rd);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Imprenta] Gráficos ajustados; SSAO añadido a " + added + " renderers");
        }
    }
}
