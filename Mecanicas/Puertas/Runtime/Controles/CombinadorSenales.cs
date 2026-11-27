using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Puertas
{
    public enum ModoCombinacion
    {
        /// <summary>Activo si TODAS las fuentes están activas (puzle de varias palancas).</summary>
        Todas,
        /// <summary>Activo si ALGUNA fuente está activa.</summary>
        Alguna,
        /// <summary>Activo si un número impar de fuentes está activo (como interruptores conmutados).</summary>
        Impar
    }

    /// <summary>
    /// Combina varias señales (palancas, botones, interruptores, paneles...) en una sola.
    /// Úsalo para puzles: "abre la reja cuando las tres palancas estén arriba".
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Combinador de señales")]
    public class CombinadorSenales : MonoBehaviour, ISenal
    {
        [Tooltip("Componentes que implementan ISenal.")]
        [SerializeField] List<MonoBehaviour> fuentes = new List<MonoBehaviour>();
        [SerializeField] ModoCombinacion modo = ModoCombinacion.Todas;
        [Tooltip("Una vez activo, se queda activo para siempre.")]
        [SerializeField] bool mantenerAlActivar;

        public UnityEvent alActivar = new UnityEvent();
        public UnityEvent alDesactivar = new UnityEvent();
        public EventoBool alCambiar = new EventoBool();

        readonly List<ISenal> senales = new List<ISenal>();
        bool activa;

        public event Action<bool> AlCambiarSenal;
        public bool Activa => activa;

        void Awake()
        {
            foreach (var f in fuentes) if (f is ISenal s) senales.Add(s);
        }

        void OnEnable()
        {
            foreach (var s in senales) s.AlCambiarSenal += AlCambiarFuente;
        }

        void Start() => Evaluar(false);

        void OnDisable()
        {
            foreach (var s in senales) s.AlCambiarSenal -= AlCambiarFuente;
        }

        void OnValidate()
        {
            for (int i = 0; i < fuentes.Count; i++)
                if (fuentes[i] != null && !(fuentes[i] is ISenal))
                    fuentes[i] = fuentes[i].GetComponent<ISenal>() as MonoBehaviour;
        }

        void AlCambiarFuente(bool _) => Evaluar(true);

        void Evaluar(bool notificar)
        {
            if (mantenerAlActivar && activa) return;
            int activas = 0;
            foreach (var s in senales) if (s.Activa) activas++;
            bool nueva;
            switch (modo)
            {
                case ModoCombinacion.Alguna: nueva = activas > 0; break;
                case ModoCombinacion.Impar: nueva = activas % 2 == 1; break;
                default: nueva = senales.Count > 0 && activas == senales.Count; break;
            }
            if (nueva == activa) return;
            activa = nueva;
            if (!notificar) return;
            if (activa) alActivar.Invoke(); else alDesactivar.Invoke();
            alCambiar.Invoke(activa);
            AlCambiarSenal?.Invoke(activa);
        }
    }
}
