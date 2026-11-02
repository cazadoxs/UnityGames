using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Construye torretas de ejemplo con formas básicas de Unity (cubos y cilindros), ya montadas
    /// y configuradas. Sirve para probar el sistema sin modelos 3D y como referencia de cómo
    /// debe estar organizada la jerarquía de una torreta.
    /// </summary>
    public static class FabricaTorretas
    {
        static readonly Dictionary<Color, Material> materiales = new Dictionary<Color, Material>();

        public static readonly Color ColorBase = new Color(0.25f, 0.27f, 0.3f);
        public static readonly Color ColorCuerpo = new Color(0.45f, 0.5f, 0.55f);
        public static readonly Color ColorCanon = new Color(0.15f, 0.15f, 0.17f);
        public static readonly Color ColorFogonazo = new Color(1f, 0.8f, 0.3f);

        /// <summary>
        /// Crea una torreta completa (Torreta + TorretaEfectos + su propia bala) en el modo pedido.
        /// </summary>
        public static Torreta Crear(ModoApuntado modo, Vector3 posicion, Quaternion rotacion, Transform padre = null)
        {
            string nombre = modo == ModoApuntado.Fija ? "Torreta fija"
                          : modo == ModoApuntado.UnEje ? "Torreta giratoria (un eje)"
                          : "Torreta todos los ángulos";
            var raiz = new GameObject(nombre).transform;
            raiz.SetParent(padre, false);
            raiz.SetPositionAndRotation(posicion, rotacion);

            // Base fija al suelo
            Pieza(PrimitiveType.Cylinder, "Base", raiz, new Vector3(0f, 0.2f, 0f), new Vector3(1.4f, 0.2f, 1.4f), ColorBase, true);

            // Pieza que gira (en Fija no gira, pero mantenemos la misma jerarquía)
            var pivotGiro = Vacio("Pivot Giro", raiz, new Vector3(0f, 0.4f, 0f));
            Pieza(PrimitiveType.Cube, "Cuerpo", pivotGiro, new Vector3(0f, 0.35f, 0f), new Vector3(1f, 0.6f, 1.1f), ColorCuerpo, true);

            Transform soporteCanones = pivotGiro;
            Transform pivotElev = null;
            if (modo == ModoApuntado.TodosLosAngulos)
            {
                pivotElev = Vacio("Pivot Elevacion", pivotGiro, new Vector3(0f, 0.45f, 0.2f));
                Pieza(PrimitiveType.Cylinder, "Eje", pivotElev, Vector3.zero, new Vector3(0.35f, 0.65f, 0.35f), ColorBase, false)
                    .localRotation = Quaternion.Euler(0f, 0f, 90f);
                soporteCanones = pivotElev;
            }

            // Cañones: uno en "todos los ángulos", dos en las demás (para ver el modo Alternar)
            float[] xs = modo == ModoApuntado.TodosLosAngulos ? new[] { 0f } : new[] { -0.25f, 0.25f };
            float alturaCanon = modo == ModoApuntado.TodosLosAngulos ? 0f : 0.4f;
            var bocas = new Transform[xs.Length];
            var retrocesos = new Transform[xs.Length];
            var fogonazos = new GameObject[xs.Length];
            for (int i = 0; i < xs.Length; i++)
            {
                var canon = Vacio("Cañón " + (i + 1), soporteCanones, new Vector3(xs[i], alturaCanon, 0.5f));
                Pieza(PrimitiveType.Cylinder, "Tubo", canon, new Vector3(0f, 0f, 0.5f), new Vector3(0.16f, 0.5f, 0.16f), ColorCanon, false)
                    .localRotation = Quaternion.Euler(90f, 0f, 0f);
                var boca = Vacio("Boca", canon, new Vector3(0f, 0f, 1.05f));
                var fog = Pieza(PrimitiveType.Sphere, "Fogonazo", boca, new Vector3(0f, 0f, 0.15f), new Vector3(0.3f, 0.3f, 0.55f), ColorFogonazo, false, true);
                fog.gameObject.SetActive(false);
                bocas[i] = boca; retrocesos[i] = canon; fogonazos[i] = fog.gameObject;
            }

            var luzGo = new GameObject("Luz fogonazo");
            luzGo.transform.SetParent(bocas[0], false);
            var luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = ColorFogonazo;
            luz.range = 6f;
            luz.intensity = 0f;
            luz.shadows = LightShadows.None;

            // Plantilla de bala guardada dentro de la torreta (desactivada) para que sea autónoma.
            var bala = CrearProyectil(modo == ModoApuntado.TodosLosAngulos ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 0.85f, 0.3f));
            bala.transform.SetParent(raiz, false);

            // Componentes
            var torreta = raiz.gameObject.AddComponent<Torreta>();
            torreta.modo = modo;
            torreta.pivotGiro = pivotGiro;
            torreta.pivotElevacion = pivotElev;
            torreta.puntosDisparo = bocas;
            torreta.prefabProyectil = bala;
            switch (modo)
            {
                case ModoApuntado.Fija:
                    torreta.anguloVision = 12f;
                    torreta.cadencia = 6f;
                    torreta.modoCanones = ModoCanones.Simultaneo;
                    break;
                case ModoApuntado.UnEje:
                    torreta.velocidadGiro = 90f;
                    torreta.anguloVision = 10f;
                    torreta.cadencia = 8f;
                    torreta.balasPorRafaga = 6;
                    torreta.pausaEntreRafagas = 0.8f;
                    torreta.vigilarSinObjetivo = true;
                    break;
                default:
                    torreta.velocidadGiro = 150f;
                    torreta.velocidadElevacion = 100f;
                    torreta.cadencia = 2.5f;
                    torreta.velocidadProyectil = 30f;
                    torreta.gravedadProyectil = 0.5f;
                    torreta.danio = 25f;
                    break;
            }

            var fx = raiz.gameObject.AddComponent<TorretaEfectos>();
            fx.piezasRetroceso = retrocesos;
            fx.fogonazos = fogonazos;
            fx.luzFogonazo = luz;
            return torreta;
        }

        /// <summary>Crea una bala básica (esfera brillante con estela), desactivada, lista para usarse como plantilla.</summary>
        public static GameObject CrearProyectil(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Plantilla bala";
            QuitarCollider(go);
            go.transform.localScale = Vector3.one * 0.18f;
            go.GetComponent<Renderer>().sharedMaterial = CrearMaterial(color, true);

            var estela = go.AddComponent<TrailRenderer>();
            estela.time = 0.12f;
            estela.startWidth = 0.12f;
            estela.endWidth = 0f;
            estela.minVertexDistance = 0.05f;
            estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shaderEstela = Shader.Find("Sprites/Default");
            if (shaderEstela != null) estela.sharedMaterial = new Material(shaderEstela) { name = "Estela bala" };
            estela.startColor = color;
            estela.endColor = new Color(color.r, color.g, color.b, 0f);

            var p = go.AddComponent<Proyectil>();
            p.efectoImpacto = CrearChispa(color);
            p.efectoImpacto.transform.SetParent(go.transform, false);

            go.SetActive(false);
            return go;
        }

        static GameObject CrearChispa(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Chispa impacto";
            QuitarCollider(go);
            go.GetComponent<Renderer>().sharedMaterial = CrearMaterial(color, true);
            go.AddComponent<EfectoTemporal>();
            go.SetActive(false);
            return go;
        }

        // ------------------------------------------------------------------ Utilidades
        static Transform Vacio(string nombre, Transform padre, Vector3 posLocal)
        {
            var t = new GameObject(nombre).transform;
            t.SetParent(padre, false);
            t.localPosition = posLocal;
            return t;
        }

        static Transform Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 posLocal, Vector3 escala,
            Color color, bool conCollider, bool brillante = false)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            if (!conCollider) QuitarCollider(go);
            var t = go.transform;
            t.SetParent(padre, false);
            t.localPosition = posLocal;
            t.localScale = escala;
            go.GetComponent<Renderer>().sharedMaterial = CrearMaterial(color, brillante);
            return t;
        }

        static void QuitarCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c == null) return;
            Object.DestroyImmediate(c);
        }

        /// <summary>
        /// Material del color pedido, copiado del material por defecto del proyecto, así funciona
        /// igual con Built-in, URP o HDRP.
        /// </summary>
        public static Material CrearMaterial(Color color, bool brillante = false)
        {
            Color clave = brillante ? new Color(color.r, color.g, color.b, 0.999f) : color;
            if (materiales.TryGetValue(clave, out var m) && m != null) return m;

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var baseMat = temp.GetComponent<Renderer>().sharedMaterial;
            Object.DestroyImmediate(temp);

            m = baseMat != null ? new Material(baseMat) : new Material(Shader.Find("Standard"));
            m.name = "Torreta " + ColorUtility.ToHtmlStringRGB(color) + (brillante ? " brillo" : "");
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (brillante && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2f);
            }
            materiales[clave] = m;
            return m;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => materiales.Clear();
    }
}
