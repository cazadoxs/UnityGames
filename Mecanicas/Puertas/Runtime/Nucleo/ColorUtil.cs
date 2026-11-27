using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>Cambia el color de un Renderer sin crear materiales nuevos (Built-in, URP y HDRP).</summary>
    public static class ColorUtil
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int Emision = Shader.PropertyToID("_EmissionColor");
        static MaterialPropertyBlock bloque;

        public static void Aplicar(Renderer r, Color color, float emision = 0f)
        {
            if (r == null) return;
            if (bloque == null) bloque = new MaterialPropertyBlock();
            r.GetPropertyBlock(bloque);
            bloque.SetColor(BaseColor, color);
            bloque.SetColor(ColorId, color);
            bloque.SetColor(Emision, color * emision);
            r.SetPropertyBlock(bloque);
        }

        public static void AplicarEmision(Renderer r, Color emision)
        {
            if (r == null) return;
            if (bloque == null) bloque = new MaterialPropertyBlock();
            r.GetPropertyBlock(bloque);
            bloque.SetColor(Emision, emision);
            r.SetPropertyBlock(bloque);
        }
    }
}
