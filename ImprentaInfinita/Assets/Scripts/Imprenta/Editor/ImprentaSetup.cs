using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Imprenta.EditorTools
{
    /// <summary>Crea la escena del prototipo y compila. Se usa desde el menú Imprenta o en modo batch.</summary>
    public static class ImprentaSetup
    {
        const string ScenePath = "Assets/Scenes/Imprenta.unity";

        [MenuItem("Imprenta/Crear escena del prototipo")]
        public static void Setup()
        {
            PbrMaterials.Build();
            HumanMaterials.Build();
            GraphicsSetup.Apply();
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources");
            if (!File.Exists("Assets/Resources/M_ImprentaBase.mat"))
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
                AssetDatabase.CreateAsset(m, "Assets/Resources/M_ImprentaBase.mat");
            }
            // Variante con mapa de normales (montaña de resmas, generada en tiempo de ejecución): si ningún
            // material del build la usa, Unity la descarta y la montaña no se dibuja.
            if (!File.Exists("Assets/Resources/M_ImprentaNormal.mat"))
            {
                var mn = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
                mn.EnableKeyword("_NORMALMAP");
                mn.SetTexture("_BumpMap", Texture2D.normalTexture);
                AssetDatabase.CreateAsset(mn, "Assets/Resources/M_ImprentaNormal.mat");
            }
            // Variante transparente para el cristal (mamparas, ventanillas, parabrisas)
            if (!File.Exists("Assets/Resources/M_ImprentaGlass.mat"))
            {
                var mg = new Material(Shader.Find("Universal Render Pipeline/Lit")) { enableInstancing = true };
                mg.SetFloat("_Surface", 1); mg.SetFloat("_Blend", 0);
                mg.SetOverrideTag("RenderType", "Transparent");
                mg.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mg.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mg.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mg.SetFloat("_ZWrite", 0);
                mg.renderQueue = 3000;
                mg.SetColor("_BaseColor", new Color(0.7f, 0.8f, 0.85f, 0.18f));
                mg.SetFloat("_Smoothness", 0.95f);
                AssetDatabase.CreateAsset(mg, "Assets/Resources/M_ImprentaGlass.mat");
            }
            // DirectX 11: más estable que DX12 con miles de instancias y sombras
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Imprenta Infinita";
            PlayerSettings.companyName = "Imprenta Infinita";
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
            Debug.Log("[Imprenta] Escena lista: " + ScenePath);
        }

        [MenuItem("Imprenta/Compilar para Windows")]
        public static void BuildWindows()
        {
            Setup();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/ImprentaInfinita.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            Debug.Log("[Imprenta] Build: " + report.summary.result + " · " + report.summary.totalErrors + " errores · " + report.summary.totalSize / (1024 * 1024) + " MB");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
