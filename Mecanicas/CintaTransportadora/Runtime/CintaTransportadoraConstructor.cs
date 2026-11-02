using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using MaterialFisico = UnityEngine.PhysicsMaterial;
#else
using MaterialFisico = UnityEngine.PhysicMaterial;
#endif

namespace Mecanicas.Cintas
{
    /// <summary>
    /// Construye una cinta transportadora completa con primitivas de Unity (cuerpo,
    /// banda animada, rodillos, barandillas y zona de detección). La usan el menú
    /// "GameObject > Mecanicas > Cinta transportadora" y la escena de demo, y puedes
    /// llamarla desde tu código para crear cintas en tiempo de juego.
    /// </summary>
    public static class CintaTransportadoraConstructor
    {
        /// <summary>Medidas y aspecto de la cinta. Todas las medidas en metros.</summary>
        [System.Serializable]
        public class Opciones
        {
            public string nombre = "Cinta Transportadora";
            public float ancho = 0.8f;
            public float largo = 4f;
            [Tooltip("Altura de la superficie de la banda desde la base (el pivote queda en el suelo).")]
            public float alto = 0.6f;
            public float velocidad = 1.5f;
            public bool barandillas = true;
            public float alturaBarandillas = 0.12f;
            public float grosorBarandillas = 0.05f;
            [Tooltip("Metros de banda que ocupa una repetición de la textura.")]
            public float metrosPorRepeticion = 0.5f;

            // Materiales opcionales (null = los genera CintaMateriales).
            public Material materialBanda;
            public Material materialCuerpo;
            public Material materialRodillos;
            public MaterialFisico materialFisico;
        }

        static MaterialFisico sinFriccion;

        /// <summary>Material físico sin fricción: la cinta controla ella sola el arrastre.</summary>
        public static MaterialFisico MaterialSinFriccion()
        {
            if (sinFriccion == null) sinFriccion = CrearMaterialSinFriccion();
            return sinFriccion;
        }

        public static MaterialFisico CrearMaterialSinFriccion()
        {
            return new MaterialFisico("Cinta - Sin friccion")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
#if UNITY_6000_0_OR_NEWER
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
#else
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
#endif
            };
        }

        public static CintaTransportadora Crear(Opciones o, Vector3 posicion, Quaternion rotacion, Transform padre = null)
        {
            if (o == null) o = new Opciones();
            float ancho = Mathf.Max(0.1f, o.ancho);
            float largo = Mathf.Max(0.2f, o.largo);
            float alto = Mathf.Max(0.05f, o.alto);
            float radio = Mathf.Min(alto * 0.45f, 0.12f, largo * 0.2f);

            var matBanda = o.materialBanda != null ? o.materialBanda : CintaMateriales.Banda();
            var matCuerpo = o.materialCuerpo != null ? o.materialCuerpo : CintaMateriales.Cuerpo();
            var matRodillo = o.materialRodillos != null ? o.materialRodillos : CintaMateriales.Rodillo();
            var matFisico = o.materialFisico != null ? o.materialFisico : MaterialSinFriccion();

            var raiz = new GameObject(o.nombre);
            raiz.transform.SetParent(padre, false);
            raiz.transform.SetPositionAndRotation(posicion, rotacion);

            // Collider sólido de la cinta (sobre él se apoyan los objetos).
            var caja = raiz.AddComponent<BoxCollider>();
            caja.size = new Vector3(ancho, alto, largo);
            caja.center = new Vector3(0f, alto * 0.5f, 0f);
            caja.sharedMaterial = matFisico;

            // Cuerpo (solo visual). Un poco más corto para que asomen los rodillos.
            var cuerpo = Primitiva(PrimitiveType.Cube, "Cuerpo", raiz.transform, matCuerpo);
            float altoCuerpo = alto - radio;
            cuerpo.transform.localPosition = new Vector3(0f, altoCuerpo * 0.5f, 0f);
            cuerpo.transform.localScale = new Vector3(ancho * 0.96f, altoCuerpo, largo - radio * 2f);

            // Rodillos de los extremos (giran con la cinta).
            var rodillos = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var r = Primitiva(PrimitiveType.Cylinder, i == 0 ? "Rodillo trasero" : "Rodillo delantero", raiz.transform, matRodillo);
                float z = (i == 0 ? -1f : 1f) * (largo * 0.5f - radio);
                r.transform.localPosition = new Vector3(0f, alto - radio, z);
                r.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // eje del cilindro = X de la cinta
                r.transform.localScale = new Vector3(radio * 2f, ancho * 0.5f * 0.98f, radio * 2f);
                rodillos[i] = r.transform;
            }

            // Banda: un Quad tumbado; su eje V va a lo largo de la cinta (Z local).
            var banda = Primitiva(PrimitiveType.Quad, "Banda", raiz.transform, matBanda);
            banda.transform.localPosition = new Vector3(0f, alto + 0.003f, 0f);
            banda.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            banda.transform.localScale = new Vector3(ancho * 0.94f, largo, 1f);

            // Barandillas laterales con collider para que no se caigan los objetos.
            if (o.barandillas)
            {
                for (int i = 0; i < 2; i++)
                {
                    var b = Primitiva(PrimitiveType.Cube, i == 0 ? "Barandilla izquierda" : "Barandilla derecha", raiz.transform, matCuerpo, true);
                    float total = alto + o.alturaBarandillas;
                    b.transform.localPosition = new Vector3((i == 0 ? -1f : 1f) * (ancho + o.grosorBarandillas) * 0.5f, total * 0.5f, 0f);
                    b.transform.localScale = new Vector3(o.grosorBarandillas, total, largo);
                    b.GetComponent<Collider>().sharedMaterial = matFisico;
                }
            }

            // Zona de detección (Trigger) justo encima de la banda.
            var zonaGo = new GameObject("ZonaDeteccion");
            zonaGo.transform.SetParent(raiz.transform, false);
            var zona = zonaGo.AddComponent<BoxCollider>();
            zona.isTrigger = true;
            float alturaZona = 0.35f;
            zona.size = new Vector3(ancho, alturaZona, largo);
            zona.center = new Vector3(0f, alto + alturaZona * 0.5f - 0.02f, 0f);
            zonaGo.AddComponent<CintaZonaDeteccion>();

            var cinta = raiz.AddComponent<CintaTransportadora>();
            cinta.EstablecerVelocidad(o.velocidad);

            var visual = raiz.AddComponent<CintaTransportadoraVisual>();
            visual.Configurar(cinta, banda.GetComponent<Renderer>(), largo, o.metrosPorRepeticion, rodillos, radio);

            return cinta;
        }

        static GameObject Primitiva(PrimitiveType tipo, string nombre, Transform padre, Material mat, bool conCollider = false)
        {
            var go = GameObject.CreatePrimitive(tipo);
            go.name = nombre;
            go.transform.SetParent(padre, false);
            if (!conCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
