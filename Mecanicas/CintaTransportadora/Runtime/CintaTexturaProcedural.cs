using UnityEngine;

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Genera por código una textura de banda de goma con flechas (chevrons) para que
    /// la cinta se vea moverse sin necesitar ninguna imagen. Puedes sustituirla por la
    /// tuya poniendo una textura en el material de la banda.
    /// </summary>
    public static class CintaTexturaProcedural
    {
        static Texture2D cache;

        /// <summary>Devuelve (y guarda en caché) la textura por defecto.</summary>
        public static Texture2D Obtener()
        {
            if (cache == null) cache = Crear(128, new Color(0.13f, 0.13f, 0.14f), new Color(0.95f, 0.75f, 0.1f));
            return cache;
        }

        /// <summary>
        /// Crea una textura de tamaño x tamaño. El eje V (vertical de la imagen) es el
        /// sentido de avance: las flechas apuntan hacia V creciente. Hay 2 flechas por
        /// repetición de textura.
        /// </summary>
        public static Texture2D Crear(int tamano, Color goma, Color flecha)
        {
            tamano = Mathf.Clamp(tamano, 16, 2048);
            var tex = new Texture2D(tamano, tamano, TextureFormat.RGBA32, true)
            {
                name = "CintaBandaProcedural",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };

            var px = new Color[tamano * tamano];
            for (int y = 0; y < tamano; y++)
            {
                float v = (y + 0.5f) / tamano;
                for (int x = 0; x < tamano; x++)
                {
                    float u = (x + 0.5f) / tamano;
                    Color c = goma;

                    // Grano de la goma (ruido suave y repetible).
                    float ruido = Mathf.PerlinNoise(u * 40f, v * 40f) * 0.06f - 0.03f;
                    c += new Color(ruido, ruido, ruido, 0f);

                    // Listones transversales (juntas entre tramos de banda).
                    float listones = Mathf.Repeat(v * 8f, 1f);
                    if (listones < 0.06f) c *= 0.7f;

                    // Chevrons: dos por repetición, apuntando hacia V+.
                    float centro = Mathf.Abs(u - 0.5f);
                    if (centro < 0.32f)
                    {
                        float d = Mathf.Repeat(v * 2f + centro * 1.2f, 1f);
                        if (d > 0.62f && d < 0.8f) c = Color.Lerp(c, flecha, 0.85f);
                    }

                    // Bordes más oscuros.
                    if (u < 0.05f || u > 0.95f) c *= 0.55f;

                    c.a = 1f;
                    px[y * tamano + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(true, false);
            return tex;
        }
    }
}
