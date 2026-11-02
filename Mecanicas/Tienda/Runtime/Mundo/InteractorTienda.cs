using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Ponlo en la cámara del jugador. Lanza un rayo hacia delante, muestra el aviso del objeto
    /// que mira ("[E] Comprar Caja · 20 monedas") y al pulsar la tecla llama a Interactuar().
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Interactor (cámara del jugador)")]
    public class InteractorTienda : MonoBehaviour
    {
        [Tooltip("Desde dónde se lanza el rayo. Vacío = este transform (pon el componente en la cámara).")]
        public Transform origen;
        [Min(0.1f)] public float distancia = 3f;
        public LayerMask capas = ~0;
        [Tooltip("Ignorar colliders trigger. Desmárcalo si tus botones usan collider trigger.")]
        public bool ignorarTriggers = true;

        [Header("Entrada")]
        public TeclaTienda tecla = TeclaTienda.E;
        public bool tambienConClic = false;

        [Header("Aviso en pantalla")]
        [Tooltip("Texto de UI donde escribir el aviso. Vacío = se dibuja solo con OnGUI.")]
        public Text textoAviso;
        public bool dibujarConOnGUI = true;
        [Tooltip("Se oculta el aviso mientras hay una tienda en menú abierta.")]
        public bool ocultarConMenuAbierto = true;

        /// <summary>Se lanza cuando cambia el texto del aviso (vacío = no se mira nada). Úsalo para tu propio HUD.</summary>
        public event Action<string> AvisoCambiado;

        public IInteractuableTienda Actual { get; private set; }
        string avisoActual = "";
        GUIStyle estilo;

        void Update()
        {
            if (ocultarConMenuAbierto && TiendaMenu.AlgunaAbierta)
            {
                Fijar(null);
                return;
            }

            Transform o = origen != null ? origen : transform;
            IInteractuableTienda encontrado = null;
            RaycastHit hit;
            var qti = ignorarTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;
            if (Physics.Raycast(o.position, o.forward, out hit, distancia, capas, qti))
                encontrado = hit.collider.GetComponentInParent<IInteractuableTienda>();
            Fijar(encontrado);

            if (Actual != null && Actual.PuedeInteractuar &&
                (EntradaTienda.Pulsada(tecla) || (tambienConClic && EntradaTienda.ClicIzquierdo())))
            {
                Actual.Interactuar(gameObject);
            }
        }

        void Fijar(IInteractuableTienda nuevo)
        {
            Actual = nuevo;
            string texto = "";
            if (nuevo != null && nuevo.PuedeInteractuar)
            {
                string t = EntradaTienda.Nombre(tecla);
                texto = (string.IsNullOrEmpty(t) ? "" : "[" + t + "] ") + nuevo.TextoAviso;
            }
            if (texto == avisoActual) return;
            avisoActual = texto;
            if (textoAviso != null) textoAviso.text = texto;
            if (AvisoCambiado != null) AvisoCambiado(texto);
        }

        void OnGUI()
        {
            if (!dibujarConOnGUI || textoAviso != null || string.IsNullOrEmpty(avisoActual)) return;
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                estilo.normal.textColor = Color.white;
            }
            var tam = estilo.CalcSize(new GUIContent(avisoActual));
            var r = new Rect((Screen.width - tam.x) / 2f - 10, Screen.height * 0.62f, tam.x + 20, tam.y + 10);
            GUI.Box(r, avisoActual, estilo);
        }

        void OnDrawGizmosSelected()
        {
            Transform o = origen != null ? origen : transform;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(o.position, o.position + o.forward * distancia);
        }
    }
}
