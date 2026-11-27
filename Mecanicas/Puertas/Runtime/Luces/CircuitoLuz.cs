using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Grupo de luces que se encienden y apagan juntas. Cualquier número de <see cref="Interruptor"/>
    /// puede controlar el mismo circuito (luz conmutada, como en un pasillo con dos interruptores).
    /// "Corriente" permite cortar la luz desde un cuadro eléctrico (palanca) sin tocar los interruptores.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Circuito de luz")]
    public class CircuitoLuz : MonoBehaviour, ISenal
    {
        [Header("Luces")]
        [SerializeField] List<Light> luces = new List<Light>();
        [Tooltip("Mallas de las lámparas cuyo brillo (emisión) se enciende y apaga.")]
        [SerializeField] List<Renderer> lamparas = new List<Renderer>();
        [ColorUsage(false, true)] [SerializeField] Color colorEmision = new Color(1f, 0.92f, 0.75f) * 2f;

        [Header("Estado")]
        [SerializeField] bool encendidoInicial = true;
        [SerializeField] bool conCorriente = true;
        [Tooltip("Segundos de fundido al encender/apagar (0 = instantáneo).")]
        [SerializeField] float fundido = 0.08f;

        [Header("Parpadeo al encender (fluorescente)")]
        [SerializeField] bool parpadeoAlEncender;
        [SerializeField] int parpadeos = 3;

        [Header("Eventos")]
        public UnityEvent alEncender = new UnityEvent();
        public UnityEvent alApagar = new UnityEvent();

        readonly List<float> intensidades = new List<float>();
        bool encendido;
        float factor, destino;
        bool parpadeando;
        bool luzAnterior;

        public event Action<bool> AlCambiarSenal;

        /// <summary>Estado de los interruptores (aunque no haya corriente).</summary>
        public bool Encendido => encendido;
        public bool ConCorriente => conCorriente;
        /// <summary>True si la luz está realmente dando luz.</summary>
        public bool Activa => encendido && conCorriente;

        void Awake()
        {
            foreach (var l in luces) intensidades.Add(l != null ? l.intensity : 0f);
            encendido = encendidoInicial;
            luzAnterior = Activa;
            factor = destino = Activa ? 1f : 0f;
            AplicarFactor();
        }

        public void Encender() => EstablecerEncendido(true);
        public void Apagar() => EstablecerEncendido(false);
        public void Alternar() => EstablecerEncendido(!encendido);

        public void EstablecerEncendido(bool valor)
        {
            if (encendido == valor) return;
            encendido = valor;
            Actualizar();
        }

        public void EstablecerCorriente(bool valor)
        {
            if (conCorriente == valor) return;
            conCorriente = valor;
            Actualizar();
        }

        public void AlternarCorriente() => EstablecerCorriente(!conCorriente);

        void Actualizar()
        {
            bool activa = Activa;
            if (activa == luzAnterior) return;
            luzAnterior = activa;
            if (activa && parpadeoAlEncender && isActiveAndEnabled) StartCoroutine(Parpadeo());
            else destino = activa ? 1f : 0f;
            if (activa) alEncender.Invoke(); else alApagar.Invoke();
            AlCambiarSenal?.Invoke(activa);
        }

        IEnumerator Parpadeo()
        {
            parpadeando = true;
            for (int i = 0; i < parpadeos && Activa; i++)
            {
                factor = 1f; AplicarFactor();
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.09f));
                factor = 0f; AplicarFactor();
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.05f, 0.25f));
            }
            parpadeando = false;
            destino = Activa ? 1f : 0f;
        }

        void Update()
        {
            if (parpadeando) return;
            if (Mathf.Approximately(factor, destino)) return;
            factor = fundido <= 0f ? destino : Mathf.MoveTowards(factor, destino, Time.deltaTime / fundido);
            AplicarFactor();
        }

        void AplicarFactor()
        {
            for (int i = 0; i < luces.Count; i++)
            {
                var l = luces[i];
                if (l == null) continue;
                float baseI = i < intensidades.Count ? intensidades[i] : l.intensity;
                l.intensity = baseI * factor;
                l.enabled = factor > 0.001f;
            }
            foreach (var r in lamparas) ColorUtil.AplicarEmision(r, colorEmision * factor);
        }
    }
}
