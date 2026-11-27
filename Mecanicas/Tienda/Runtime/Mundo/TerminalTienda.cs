using UnityEngine;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Objeto del mundo (mostrador, máquina expendedora, ordenador, NPC...) que abre la tienda en menú.
    /// Cada terminal puede tener su propio catálogo y su propio punto de aparición.
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Terminal de tienda (abre el menú)")]
    public class TerminalTienda : MonoBehaviour, IInteractuableTienda
    {
        [Tooltip("Menú que se abre. Vacío = el primero que haya en la escena.")]
        public TiendaMenu menu;
        [Tooltip("Catálogo de esta terminal. Vacío = el del menú o el del GestorTienda.")]
        public CatalogoTienda catalogo;
        [Tooltip("Dónde aparece lo comprado aquí (IReceptorCompra). Vacío = el del menú o el del GestorTienda.")]
        public MonoBehaviour receptor;
        public string textoAviso = "Abrir {tienda}";

        [Header("Zona (opcional)")]
        [Tooltip("Abrir al entrar en el trigger y cerrar al salir.")]
        public bool abrirAlEntrarEnTrigger = false;
        public string etiquetaJugador = "Player";

        public string TextoAviso
        {
            get
            {
                string n = catalogo != null ? catalogo.nombreTienda : "la tienda";
                return textoAviso.Replace("{tienda}", n);
            }
        }

        public bool PuedeInteractuar { get { return isActiveAndEnabled && Menu != null && !Menu.Abierta; } }

        TiendaMenu Menu
        {
            get
            {
                if (menu == null) menu = FindFirstObjectByType<TiendaMenu>();
                return menu;
            }
        }

        public void Interactuar(GameObject quien)
        {
            if (Menu == null) { Debug.LogWarning("[Tienda] No hay ningún TiendaMenu en la escena.", this); return; }
            Menu.Abrir(catalogo, receptor as IReceptorCompra);
        }

        void OnTriggerEnter(Collider otro)
        {
            if (abrirAlEntrarEnTrigger && EsJugador(otro)) Interactuar(otro.gameObject);
        }

        void OnTriggerExit(Collider otro)
        {
            if (abrirAlEntrarEnTrigger && EsJugador(otro) && Menu != null && Menu.Abierta) Menu.Cerrar();
        }

        bool EsJugador(Collider c)
        {
            return string.IsNullOrEmpty(etiquetaJugador) || c.CompareTag(etiquetaJugador);
        }
    }
}
