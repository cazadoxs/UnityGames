using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Imprenta
{
    /// <summary>
    /// Construye la nave (doc 4.1) con primitivas: 120 m de ancho × 240 m de fondo × 30 m de alto.
    /// El portón está en z = -10; el puesto de trabajo a 20 m, en z = 10; la montaña empieza en z = 22.
    /// </summary>
    public static class WorldBuilder
    {
        public const float HalfWidth = 60f, ZStart = -10f, ZEnd = 230f, Height = 30f;

        public static Transform BuildHall(Transform root)
        {
            var hall = Build.Empty("Nave", root, Vector3.zero).transform;
            var concrete = Build.Pbr("hangar_concrete_floor", new Vector2(30, 60), Build.Concrete);
            var wallMat = Build.Pbr("corrugated_iron", new Vector2(60, 8), Build.Hex("#7d8286"));
            var steel = Build.Mat(Build.SteelDark, 0.4f, 0.8f);
            var safety = Build.Mat(Build.Safety, 0.35f);

            float len = ZEnd - ZStart, midZ = (ZStart + ZEnd) / 2f;
            Build.Box("Suelo", hall, new Vector3(0, -0.1f, midZ), new Vector3(HalfWidth * 2, 0.2f, len), concrete);
            // Paredes
            Build.Box("Pared_Oeste", hall, new Vector3(-HalfWidth - 0.25f, Height / 2, midZ), new Vector3(0.5f, Height, len), wallMat);
            Build.Box("Pared_Este", hall, new Vector3(HalfWidth + 0.25f, Height / 2, midZ), new Vector3(0.5f, Height, len), wallMat);
            Build.Box("Pared_Fondo", hall, new Vector3(0, Height / 2, ZEnd + 0.25f), new Vector3(HalfWidth * 2, Height, 0.5f), wallMat);
            // Pared frontal con portón
            Build.Box("Pared_Frontal_A", hall, new Vector3(-33, Height / 2, ZStart - 0.25f), new Vector3(54, Height, 0.5f), wallMat);
            // tramo derecho con el hueco del muelle de carga (x 25-31, 5 m de alto)
            Build.Box("Pared_Frontal_B1", hall, new Vector3(15.5f, Height / 2, ZStart - 0.25f), new Vector3(19, Height, 0.5f), wallMat);
            Build.Box("Pared_Frontal_B2", hall, new Vector3(45.5f, Height / 2, ZStart - 0.25f), new Vector3(29, Height, 0.5f), wallMat);
            Build.Box("Pared_Frontal_Muelle", hall, new Vector3(28f, 5f + (Height - 5f) / 2, ZStart - 0.25f), new Vector3(6, Height - 5f, 0.5f), wallMat);
            // explanada de asfalto delante de la nave, para que el camión tenga por dónde llegar
            Build.Box("Explanada", hall, new Vector3(0, -0.1f, ZStart - 40.5f), new Vector3(200, 0.2f, 80f), Build.Pbr("hangar_concrete_floor", new Vector2(40, 16), Build.Hex("#3d3d3d"), Build.Hex("#6b6b6b")));
            // Túnel de carga: el camión sale de la oscuridad marcha atrás hasta el muelle
            {
                const float tx = 28f, tw = 6.4f, th = 5.2f, tz0 = ZStart - 0.5f, tz1 = ZStart - 62f;
                float tl = tz0 - tz1, tmz = (tz0 + tz1) / 2f;
                var tun = Build.Mat(Build.Hex("#2b2c2e"), 0.18f);
                var tunDark = Build.Mat(Build.Hex("#151617"), 0.1f);
                var ribM = Build.Mat(Build.Hex("#38393b"), 0.25f, 0.2f);
                Build.Box("Tunel_Pared", hall, new Vector3(tx - tw / 2 - 0.3f, th / 2, tmz), new Vector3(0.6f, th, tl), tun);
                Build.Box("Tunel_Pared", hall, new Vector3(tx + tw / 2 + 0.3f, th / 2, tmz), new Vector3(0.6f, th, tl), tun);
                Build.Box("Tunel_Techo", hall, new Vector3(tx, th + 0.3f, tmz), new Vector3(tw + 1.2f, 0.6f, tl), tun);
                Build.Box("Tunel_Fondo", hall, new Vector3(tx, th / 2, tz1 - 0.3f), new Vector3(tw + 1.2f, th, 0.6f), Build.Mat(Color.black, 0f));
                // bloque exterior para que no entre luz por arriba ni por los lados
                Build.Box("Tunel_Tierra", hall, new Vector3(tx, th + 3f, tmz), new Vector3(tw + 10f, 4.8f, tl), tunDark, false);
                for (float z = tz0 - 2f; z > tz1; z -= 4f)
                {
                    Build.Box("Tunel_Costilla", hall, new Vector3(tx - tw / 2 + 0.12f, th / 2, z), new Vector3(0.25f, th, 0.35f), ribM, false);
                    Build.Box("Tunel_Costilla", hall, new Vector3(tx + tw / 2 - 0.12f, th / 2, z), new Vector3(0.25f, th, 0.35f), ribM, false);
                    Build.Box("Tunel_Costilla", hall, new Vector3(tx, th - 0.12f, z), new Vector3(tw, 0.25f, 0.35f), ribM, false);
                    Build.Box("Tunel_Bordillo", hall, new Vector3(tx - tw / 2 + 0.3f, 0.1f, z), new Vector3(0.6f, 0.2f, 4f), Build.Mat(Build.Hex("#4a4a46"), 0.2f), false);
                    Build.Box("Tunel_Bordillo", hall, new Vector3(tx + tw / 2 - 0.3f, 0.1f, z), new Vector3(0.6f, 0.2f, 4f), Build.Mat(Build.Hex("#4a4a46"), 0.2f), false);
                    Build.Box("Tunel_Raya", hall, new Vector3(tx, 0.006f, z), new Vector3(0.15f, 0.01f, 2f), Build.Mat(Build.Hex("#c9a227"), 0.3f), false);
                }
                // lámparas de sodio cada 12 m, cada vez más débiles hacia la boca oscura
                int li = 0;
                for (float z = tz0 - 4f; z > tz1 + 10f; z -= 12f, li++)
                {
                    Build.Box("Tunel_Lampara", hall, new Vector3(tx + tw / 2 - 0.35f, th - 0.6f, z), new Vector3(0.18f, 0.12f, 0.6f),
                              Build.Mat(Color.black, 0.5f, 0f, new Color(3.2f, 1.6f, 0.4f) * (1f - li * 0.17f)), false);
                    if (li < 4) Build.PointLight(hall, new Vector3(tx + tw / 2 - 0.8f, th - 0.9f, z), Build.Hex("#ff9a3c"), 2.2f - li * 0.45f, 9f);
                }
            }
            Build.Box("Pared_Frontal_Dintel", hall, new Vector3(0, 19, ZStart - 0.25f), new Vector3(12, 22, 0.5f), wallMat);
            Build.Box("Porton", hall, new Vector3(0, 4, ZStart - 0.1f), new Vector3(12, 8, 0.2f), Build.Pbr("painted_metal_shutter", new Vector2(3, 2), Build.Hex("#5d6f7c")));
            for (int i = 0; i < 16; i++)
                Build.Box("Porton_Lama", hall, new Vector3(0, 0.25f + i * 0.5f, ZStart + 0.02f), new Vector3(12, 0.04f, 0.05f), steel, false);

            // Tejado con lucernarios: franjas de chapa con huecos cada 20 m
            var roof = Build.Pbr("corrugated_iron", new Vector2(40, 6), Build.Hex("#5b5f63"));
            var glass = Build.Mat(new Color(0.75f, 0.78f, 0.7f, 1), 0.9f, 0f, new Color(0.25f, 0.25f, 0.22f));
            for (float z = ZStart; z < ZEnd; z += 20f)
            {
                Build.Box("Tejado", hall, new Vector3(0, Height + 0.2f, z + 8f), new Vector3(HalfWidth * 2, 0.4f, 16f), roof, false);
                // El lucernario «sucio»: un marco con cristal fino a los lados y hueco central para que entre el sol
                Build.Box("Lucernario_Marco", hall, new Vector3(0, Height + 0.2f, z + 16.1f), new Vector3(HalfWidth * 2, 0.4f, 0.2f), steel, false);
                Build.Box("Lucernario_Marco", hall, new Vector3(0, Height + 0.2f, z + 19.9f), new Vector3(HalfWidth * 2, 0.4f, 0.2f), steel, false);
                for (float x = -HalfWidth; x < HalfWidth; x += 6f)
                    Build.Box("Lucernario_Montante", hall, new Vector3(x, Height + 0.2f, z + 18f), new Vector3(0.15f, 0.3f, 4f), steel, false);
            }
            // Cerchas cada 10 m y pilares en las paredes
            for (float z = ZStart + 10; z < ZEnd; z += 10f)
            {
                Build.Box("Cercha_Cordon", hall, new Vector3(0, Height - 0.6f, z), new Vector3(HalfWidth * 2, 0.35f, 0.25f), steel, false);
                Build.Box("Cercha_Inferior", hall, new Vector3(0, Height - 3.2f, z), new Vector3(HalfWidth * 2, 0.25f, 0.2f), steel, false);
                for (float x = -HalfWidth + 3; x < HalfWidth; x += 6f)
                    Build.Box("Cercha_Diagonal", hall, new Vector3(x, Height - 1.9f, z), new Vector3(0.12f, 3.4f, 0.12f), steel, false, new Vector3(0, 0, (x % 12f == 3f || x % 12f == -9f) ? 40 : -40));
                Build.Box("Pilar_O", hall, new Vector3(-HalfWidth + 0.3f, Height / 2, z), new Vector3(0.5f, Height, 0.5f), steel);
                Build.Box("Pilar_E", hall, new Vector3(HalfWidth - 0.3f, Height / 2, z), new Vector3(0.5f, Height, 0.5f), steel);
            }

            // Marcas del suelo: pasillo amarillo de la entrada al puesto y zona de trabajo
            Build.Box("Marca_Pasillo_I", hall, new Vector3(-2.2f, 0.005f, 0f), new Vector3(0.12f, 0.01f, 18f), safety, false);
            Build.Box("Marca_Pasillo_D", hall, new Vector3(2.2f, 0.005f, 0f), new Vector3(0.12f, 0.01f, 18f), safety, false);
            Build.Box("Marca_Zona_A", hall, new Vector3(4f, 0.005f, 8.4f), new Vector3(20f, 0.01f, 0.12f), safety, false);
            Build.Box("Marca_Zona_B", hall, new Vector3(4f, 0.005f, 15.5f), new Vector3(20f, 0.01f, 0.12f), safety, false);
            // Manchas de carretilla
            var stain = Build.Mat(Build.ConcreteDark, 0.6f);
            for (int i = 0; i < 14; i++)
            {
                float x = Random.Range(-25f, 25f), z = Random.Range(-8f, 20f);
                Build.Box("Marca_Rueda", hall, new Vector3(x, 0.003f, z), new Vector3(0.18f, 0.005f, Random.Range(3f, 9f)), stain, false, new Vector3(0, Random.Range(-40f, 40f), 0));
            }

            // Pasarela metálica sobre el vestuario, con escalera: se ve la nave desde arriba
            {
                var grate = Build.Pbr("metal_plate", new Vector2(8, 1), Build.Hex("#5a5e62"));
                var rail = Build.Mat(Build.Safety, 0.45f, 0.3f);
                float py = 4f, z0 = -9.95f, z1 = -8.2f, x0 = -30f, x1 = -4f;
                Build.Box("Pasarela", hall, new Vector3((x0 + x1) / 2, py, (z0 + z1) / 2), new Vector3(x1 - x0, 0.08f, z1 - z0), grate);
                Build.Box("Pasarela_Viga", hall, new Vector3((x0 + x1) / 2, py - 0.15f, z1 - 0.05f), new Vector3(x1 - x0, 0.25f, 0.1f), steel, false);
                for (float x = x0 + 0.5f; x <= x1; x += 3f)
                {
                    Build.Box("Pasarela_Mensula", hall, new Vector3(x, py - 0.3f, (z0 + z1) / 2), new Vector3(0.08f, 0.5f, z1 - z0), steel, false);
                    Build.Box("Barandilla_Poste", hall, new Vector3(x, py + 0.55f, z1 - 0.05f), new Vector3(0.05f, 1.1f, 0.05f), rail);
                }
                Build.Box("Barandilla", hall, new Vector3((x0 + x1) / 2, py + 1.1f, z1 - 0.05f), new Vector3(x1 - x0, 0.06f, 0.06f), rail);
                Build.Box("Barandilla_Media", hall, new Vector3((x0 + x1) / 2, py + 0.55f, z1 - 0.05f), new Vector3(x1 - x0, 0.04f, 0.04f), rail);
                Build.Box("Barandilla_Fin", hall, new Vector3(x1, py + 0.55f, (z0 + z1) / 2), new Vector3(0.05f, 1.1f, z1 - z0), rail);
                // escalera de x=-36 (suelo) a x=-30 (arriba): peldaños y una rampa invisible para subir suave
                int steps = 20;
                float sx0 = -36f, run = x0 - sx0;
                for (int i = 0; i < steps; i++)
                {
                    float t = (i + 0.5f) / steps;
                    Build.Box("Peldaño", hall, new Vector3(sx0 + run * t, py * t, (z0 + z1) / 2), new Vector3(run / steps + 0.02f, 0.05f, z1 - z0), grate, false);
                }
                float ang = Mathf.Atan2(py, run) * Mathf.Rad2Deg, slen = Mathf.Sqrt(py * py + run * run);
                var ramp = Build.Box("Escalera_Rampa", hall, new Vector3(sx0 + run / 2, py / 2 - 0.05f, (z0 + z1) / 2), new Vector3(slen, 0.06f, z1 - z0), grate, true, new Vector3(0, 0, ang));
                ramp.GetComponent<MeshRenderer>().enabled = false;
                Build.Box("Zanca", hall, new Vector3(sx0 + run / 2, py / 2 - 0.15f, z1), new Vector3(slen, 0.25f, 0.06f), steel, false, new Vector3(0, 0, ang));
                Build.Box("Pasamanos", hall, new Vector3(sx0 + run / 2, py / 2 + 0.95f, z1), new Vector3(slen, 0.05f, 0.05f), rail, false, new Vector3(0, 0, ang));
            }

            // Objetos de ambiente cerca de la entrada
            var cardboard = Build.Mat(Build.Hex("#a7834f"), 0.15f);
            for (int i = 0; i < 9; i++)
            {
                var p = new Vector3(Random.Range(-14f, -7f), 0, Random.Range(-8f, 4f));
                float s = Random.Range(0.4f, 0.7f);
                Build.Box("Caja_Carton", hall, p + Vector3.up * s / 2, new Vector3(s, s, s * 1.2f), cardboard, true, new Vector3(0, Random.Range(0, 90f), 0));
            }
            var pallet = Build.Pbr("plywood", new Vector2(1, 1), Build.Hex("#9c7a52"));
            Build.Box("Palet", hall, new Vector3(9, 0.07f, -5), new Vector3(1.2f, 0.14f, 0.8f), pallet);
            Build.Box("Palet", hall, new Vector3(9, 0.21f, -5), new Vector3(1.2f, 0.14f, 0.8f), pallet, true, new Vector3(0, 8, 0));
            Build.Box("Extintor", hall, new Vector3(-2.8f, 0.3f, 6.5f), new Vector3(0.18f, 0.6f, 0.18f), Build.Mat(Build.Hex("#b3261e"), 0.6f));
            return hall;
        }

        public static void BuildLighting(Transform root)
        {
            // Sol a través de los lucernarios
            var sunGo = Build.Empty("Sol", root, new Vector3(0, 50, 0), new Vector3(52, -32, 0));
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Build.Hex("#ffe7c4");
            sun.intensity = 2.2f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Build.Hex("#59606a");
            RenderSettings.ambientEquatorColor = Build.Hex("#575249");
            RenderSettings.ambientGroundColor = Build.Hex("#24221f");
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.006f;
            RenderSettings.fogColor = Build.Hex("#575650");

            // Fluorescentes colgados sobre el puesto de trabajo
            var tube = Build.Mat(Color.white, 0.5f, 0f, new Color(3.2f, 3.2f, 3.0f));
            var housing = Build.Mat(Build.Steel, 0.5f, 0.7f);
            Vector3[] fixtures = { new Vector3(0, 4.2f, 9.5f), new Vector3(7, 4.2f, 10.5f), new Vector3(-6, 4.2f, 9f), new Vector3(0, 4.2f, 1f), new Vector3(0, 4.2f, -6f), new Vector3(13, 4.2f, 10.5f) };
            foreach (var f in fixtures)
            {
                var fx = Build.Empty("Fluorescente", root, f).transform;
                Build.Box("Carcasa", fx, Vector3.zero, new Vector3(1.4f, 0.08f, 0.25f), housing, false);
                Build.Box("Tubo", fx, new Vector3(0, -0.06f, 0), new Vector3(1.3f, 0.04f, 0.06f), tube, false);
                Build.Box("Cable", fx, new Vector3(0.6f, (Height - f.y) / 2f, 0), new Vector3(0.01f, Height - f.y, 0.01f), housing, false);
                Build.Box("Cable", fx, new Vector3(-0.6f, (Height - f.y) / 2f, 0), new Vector3(0.01f, Height - f.y, 0.01f), housing, false);
                Build.PointLight(fx, new Vector3(0, -0.3f, 0), Build.Hex("#e8f0ff"), 6f, 12f, false);
            }
            // Campanas industriales de luz cálida colgadas sobre la zona de trabajo (como en un taller de verdad)
            var shade = Build.Mat(Build.Hex("#2c3a33"), 0.55f, 0.6f);
            var warm = Build.Mat(Color.white, 0.5f, 0, new Color(5f, 3.6f, 2.0f));
            for (int ix = 0; ix < 5; ix++)
                for (int iz = 0; iz < 2; iz++)
                {
                    var p = new Vector3(-24f + ix * 14f, 6.5f, -3f + iz * 14f);
                    var lamp = Build.Empty("Campana", root, p).transform;
                    Build.Box("Cable", lamp, new Vector3(0, (Height - p.y) / 2f, 0), new Vector3(0.015f, Height - p.y, 0.015f), housing, false);
                    var cone = Build.Cyl("Pantalla", lamp, new Vector3(0, 0.12f, 0), 0.7f, 0.25f, shade, false);
                    cone.transform.localScale = new Vector3(0.7f, 0.12f, 0.7f);
                    Build.Cyl("Cuello", lamp, new Vector3(0, 0.32f, 0), 0.18f, 0.2f, shade, false);
                    Build.Cyl("Bombilla", lamp, new Vector3(0, 0.0f, 0), 0.2f, 0.12f, warm, false);
                    Build.PointLight(lamp, new Vector3(0, -0.3f, 0), Build.Hex("#ffc98a"), 7f, 15f, ix == 2 && iz == 0);
                }
            // Luces de relleno para que la montaña no sea un muro negro
            // Focos de obra que iluminan el pie de la montaña, donde se cogen las hojas
            Build.PointLight(root, new Vector3(-6, 5, 17), Build.Hex("#fff1d6"), 40f, 22f, true);
            Build.PointLight(root, new Vector3(6, 5, 17), Build.Hex("#fff1d6"), 40f, 22f);
            Build.PointLight(root, new Vector3(-20, 12, 40), Build.Hex("#c9c2b3"), 25f, 45f);
            Build.PointLight(root, new Vector3(20, 12, 40), Build.Hex("#c9c2b3"), 25f, 45f);
            Build.PointLight(root, new Vector3(0, 18, 90), Build.Hex("#c9c2b3"), 40f, 70f);

            // Post-procesado: tonemapping ACES, bloom suave, viñeta y algo de grano
            var volGo = Build.Empty("PostProcesado", root, Vector3.zero);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.85f); bloom.threshold.Override(0.95f); bloom.scatter.Override(0.65f);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(9f); wb.tint.Override(2f);
            var st = profile.Add<SplitToning>(true); st.shadows.Override(Build.Hex("#4a5d6b")); st.highlights.Override(Build.Hex("#f2c98a")); st.balance.Override(-15f);
            var smh = profile.Add<ShadowsMidtonesHighlights>(true); smh.shadows.Override(new Vector4(0.95f, 0.97f, 1.05f, -0.04f));
            var vig = profile.Add<Vignette>(true); vig.intensity.Override(0.28f); vig.smoothness.Override(0.5f);
            var ca = profile.Add<ColorAdjustments>(true); ca.postExposure.Override(0.6f); ca.contrast.Override(12f); ca.saturation.Override(-8f);
            var grain = profile.Add<FilmGrain>(true); grain.intensity.Override(0.15f);
            vol.sharedProfile = profile;
            Atmosphere.Create(root);
        }

        // ------------------------------------------------------------------ puesto de trabajo
        public static Transform BuildDesk(Transform root, out Transform typewriterAnchor, out Light lampLight)
        {
            var desk = Build.Empty("Puesto", root, new Vector3(0, 0, 10)).transform;
            var oak = Build.Pbr("fine_grained_wood", new Vector2(1, 1), Build.Oak, null, 0.9f);
            var steel = Build.Mat(Build.SteelDark, 0.4f, 0.8f);
            Build.Box("Tablero", desk, new Vector3(0, 0.75f, 0), new Vector3(1.5f, 0.05f, 0.75f), oak);
            foreach (var x in new[] { -0.7f, 0.7f })
                foreach (var z in new[] { -0.32f, 0.32f })
                    Build.Box("Pata", desk, new Vector3(x, 0.37f, z), new Vector3(0.05f, 0.74f, 0.05f), oak);
            Build.Box("Cajonera", desk, new Vector3(-0.5f, 0.45f, 0), new Vector3(0.4f, 0.55f, 0.68f), oak);
            Build.Box("Tirador", desk, new Vector3(-0.5f, 0.58f, -0.345f), new Vector3(0.12f, 0.02f, 0.02f), steel, false);
            Build.Box("Tirador", desk, new Vector3(-0.5f, 0.35f, -0.345f), new Vector3(0.12f, 0.02f, 0.02f), steel, false);

            // Silla de oficina vieja
            var chair = Build.Empty("Silla", desk, new Vector3(0, 0, -0.75f), new Vector3(0, 8, 0)).transform;
            var fabric = Build.Mat(Build.Hex("#3e4a3d"), 0.1f);
            var lampMat = Build.Mat(Build.Hex("#2f5d50"), 0.55f, 0.35f);
            var bulb = Build.Mat(Color.white, 0.5f, 0, new Color(4, 3.4f, 2.4f));
            System.Func<string, Material> props = n =>
                n == "Tela" ? fabric : n == "Metal" ? Build.Mat(Build.Hex("#3a3b3e"), 0.55f, 0.85f) : n == "Cromo" ? Build.Chrome :
                n == "Goma" ? Build.Rubber : n == "Plastico" ? Build.Mat(Build.Hex("#2b2b2a"), 0.35f) : n == "Pintura" ? lampMat :
                n == "Bombilla" ? bulb : n == "Papel" ? Build.Mat(Build.Paper, 0.15f) : null;
            if (Build.Model("Silla", chair, Vector3.zero, null, props) != null)
            {
                var cc = chair.gameObject.AddComponent<BoxCollider>();
                cc.center = new Vector3(0, 0.5f, -0.03f); cc.size = new Vector3(0.6f, 1f, 0.6f);
            }
            else
            {
                Build.Box("Asiento", chair, new Vector3(0, 0.47f, 0), new Vector3(0.48f, 0.08f, 0.46f), fabric);
                Build.Box("Respaldo", chair, new Vector3(0, 0.82f, -0.24f), new Vector3(0.46f, 0.5f, 0.06f), fabric, true, new Vector3(-8, 0, 0));
                Build.Cyl("Columna", chair, new Vector3(0, 0.25f, 0), 0.05f, 0.42f, steel);
                for (int i = 0; i < 5; i++)
                    Build.Box("Pata", chair, Quaternion.Euler(0, i * 72, 0) * new Vector3(0, 0.05f, 0.18f), new Vector3(0.04f, 0.03f, 0.36f), steel, false, new Vector3(0, i * 72, 0));
            }

            // Flexo
            var lamp = Build.Empty("Flexo", desk, new Vector3(0.66f, 0.775f, 0.3f)).transform;
            var head = Build.Empty("Cabeza", lamp, new Vector3(-0.18f, 0.38f, -0.25f), new Vector3(55, -35, 0)).transform;
            if (Build.Model("Flexo", lamp, Vector3.zero, null, props) != null)
                Build.Model("FlexoPantalla", head, Vector3.zero, null, props, false);
            else
            {
                Build.Cyl("Base", lamp, new Vector3(0, 0.015f, 0), 0.16f, 0.03f, lampMat, true);
                Build.Box("Brazo1", lamp, new Vector3(0, 0.2f, -0.05f), new Vector3(0.02f, 0.4f, 0.02f), steel, false, new Vector3(-15, 0, 0));
                Build.Box("Brazo2", lamp, new Vector3(-0.08f, 0.42f, -0.12f), new Vector3(0.02f, 0.02f, 0.3f), steel, false, new Vector3(20, 35, 0));
                Build.Cyl("Pantalla", head, Vector3.zero, 0.14f, 0.12f, lampMat, false, new Vector3(90, 0, 0));
                Build.Cyl("Bombilla", head, new Vector3(0, 0, 0.05f), 0.07f, 0.02f, bulb, false, new Vector3(90, 0, 0));
            }
            var spotGo = Build.Empty("LuzFlexo", head, new Vector3(0, 0, 0.065f));
            lampLight = spotGo.AddComponent<Light>();
            lampLight.type = LightType.Spot; lampLight.spotAngle = 75; lampLight.range = 3f; lampLight.intensity = 3.5f;
            lampLight.color = Build.Hex("#ffd9a0"); lampLight.shadows = LightShadows.Soft;

            // Papelera metálica con bolas de papel
            var bin = Build.Model("Papelera", desk, new Vector3(-0.95f, 0, -0.1f), null, n => n == "Pintura" ? Build.Mat(Build.Hex("#2d5b8a"), 0.45f, 0.4f) : props(n));
            if (bin != null) { var bc = bin.AddComponent<CapsuleCollider>(); bc.center = new Vector3(0, 0.18f, 0); bc.radius = 0.15f; bc.height = 0.36f; }
            else Build.Cyl("Papelera", desk, new Vector3(-0.95f, 0.18f, -0.1f), 0.3f, 0.36f, Build.Mat(Build.Hex("#2d5b8a"), 0.3f), true);

            typewriterAnchor = Build.Empty("AnclaMaquina", desk, new Vector3(0.05f, 0.775f, 0.02f)).transform;
            return desk;
        }
    }
}
