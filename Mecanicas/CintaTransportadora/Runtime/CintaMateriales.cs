using UnityEngine;
using UnityEngine.Rendering;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Crea materiales sencillos que funcionan en Built-in, URP y HDRP (elige el shader
    /// "Lit" del pipeline activo). Los usan el constructor y la demo; en tu juego lo
    /// normal es poner tus propios materiales.
    /// </summary>
    public static class CintaMateriales
    {
        static Material banda, cuerpo, rodillo, prueba;

        public static Material Banda() => banda != null ? banda : banda = CrearBanda();
        public static Material Cuerpo() => cuerpo != null ? cuerpo : cuerpo = Crear("Cinta - Cuerpo", new Color(0.32f, 0.36f, 0.42f), 0.55f, 0.5f);
        public static Material Rodillo() => rodillo != null ? rodillo : rodillo = Crear("Cinta - Rodillo", new Color(0.7f, 0.7f, 0.72f), 0.9f, 0.7f);
        public static Material Prueba() => prueba != null ? prueba : prueba = Crear("Cinta - Objeto de prueba", Color.white, 0f, 0.4f);

        static Material CrearBanda()
        {
            var m = Crear("Cinta - Banda", Color.white, 0f, 0.25f);
            m.mainTexture = CintaTexturaProcedural.Obtener();
            return m;
        }

        /// <summary>Shader "Lit" del pipeline de render activo.</summary>
        public static Shader ShaderLit()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            Shader s = null;
            if (rp == null) s = Shader.Find("Standard");
            else
            {
                string tipo = rp.GetType().Name;
                if (tipo.Contains("HD")) s = Shader.Find("HDRP/Lit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Unlit/Texture");
            return s;
        }

        public static Material Crear(string nombre, Color color, float metalico, float suavidad)
        {
            var m = new Material(ShaderLit()) { name = nombre };
            m.color = color; // [MainColor]: _BaseColor (URP/HDRP) o _Color (Built-in)
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metalico);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", suavidad);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", suavidad);
            return m;
        }
    }
}
