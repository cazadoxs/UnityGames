using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Patrulla
{
    /// <summary>
    /// Semáforo de dos fases para cruces: alterna entre dejar pasar al grupo A y al grupo B,
    /// bloqueando los PuntoDeRuta del grupo que está en rojo. Entre fases hay un tiempo en ámbar
    /// (los dos grupos bloqueados) para vaciar el cruce. Opcionalmente colorea unas luces.
    /// </summary>
    [AddComponentMenu("Mecanicas/Patrulla/Semaforo De Ruta")]
    public class SemaforoDeRuta : MonoBehaviour
    {
        public enum Fase { VerdeA, AmbarA, VerdeB, AmbarB }

        [Header("Puntos controlados")]
        [Tooltip("Puntos que se bloquean cuando el grupo A está en rojo (normalmente el punto de la entrada al cruce).")]
        public List<PuntoDeRuta> grupoA = new List<PuntoDeRuta>();
        [Tooltip("Puntos que se bloquean cuando el grupo B está en rojo. Puede quedar vacío (paso de peatones, barrera).")]
        public List<PuntoDeRuta> grupoB = new List<PuntoDeRuta>();

        [Header("Tiempos (segundos)")]
        [Min(0.1f)] public float tiempoVerdeA = 8f;
        [Min(0.1f)] public float tiempoVerdeB = 8f;
        [Min(0f)] public float tiempoAmbar = 2f;
        public Fase faseInicial = Fase.VerdeA;
        [Tooltip("Si está desactivado, el semáforo no cambia solo: usa CambiarFase() desde código o eventos.")]
        public bool automatico = true;

        [Header("Luces (opcional)")]
        [Tooltip("Renderers que muestran el color del grupo A.")]
        public List<Renderer> lucesA = new List<Renderer>();
        public List<Renderer> lucesB = new List<Renderer>();
        public Color colorVerde = new Color(0.1f, 0.9f, 0.2f);
        public Color colorAmbar = new Color(1f, 0.7f, 0.05f);
        public Color colorRojo = new Color(0.95f, 0.1f, 0.1f);

        [Header("Eventos")]
        public UnityEvent alCambiarFase = new UnityEvent();

        public Fase FaseActual { get; private set; }
        float tiempo;
        MaterialPropertyBlock bloque;

        void Start()
        {
            AplicarFase(faseInicial);
        }

        void Update()
        {
            if (!automatico) return;
            tiempo -= Time.deltaTime;
            if (tiempo <= 0f) CambiarFase();
        }

        /// <summary>Pasa a la siguiente fase (VerdeA → AmbarA → VerdeB → AmbarB → VerdeA).</summary>
        public void CambiarFase()
        {
            var siguiente = (Fase)(((int)FaseActual + 1) % 4);
            if (tiempoAmbar <= 0f && (siguiente == Fase.AmbarA || siguiente == Fase.AmbarB))
                siguiente = (Fase)(((int)siguiente + 1) % 4);
            AplicarFase(siguiente);
        }

        /// <summary>Fuerza una fase concreta.</summary>
        public void AplicarFase(Fase fase)
        {
            FaseActual = fase;
            switch (fase)
            {
                case Fase.VerdeA: tiempo = tiempoVerdeA; break;
                case Fase.VerdeB: tiempo = tiempoVerdeB; break;
                default: tiempo = tiempoAmbar; break;
            }

            bool pasaA = fase == Fase.VerdeA;
            bool pasaB = fase == Fase.VerdeB;
            foreach (var p in grupoA) if (p != null) p.bloqueado = !pasaA;
            foreach (var p in grupoB) if (p != null) p.bloqueado = !pasaB;

            Pintar(lucesA, fase == Fase.VerdeA ? colorVerde : fase == Fase.AmbarA ? colorAmbar : colorRojo);
            Pintar(lucesB, fase == Fase.VerdeB ? colorVerde : fase == Fase.AmbarB ? colorAmbar : colorRojo);
            alCambiarFase.Invoke();
        }

        void Pintar(List<Renderer> luces, Color c)
        {
            if (luces.Count == 0) return;
            if (bloque == null) bloque = new MaterialPropertyBlock();
            foreach (var r in luces)
            {
                if (r == null) continue;
                r.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", c);   // URP / HDRP
                bloque.SetColor("_Color", c);       // Built-in
                bloque.SetColor("_EmissionColor", c * 2f);
                r.SetPropertyBlock(bloque);
            }
        }
    }
}
