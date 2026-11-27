using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Tienda
{
    /// <summary>
    /// Monedero sencillo con varias monedas (monedas, gemas, tickets...). Opcionalmente se guarda en PlayerPrefs.
    /// </summary>
    [AddComponentMenu("Mecánicas/Tienda/Monedero")]
    public class Monedero : MonoBehaviour, IMonedero
    {
        [Serializable]
        public class SaldoMoneda
        {
            public string moneda = "monedas";
            public int cantidad = 100;
        }

        [Tooltip("Dinero con el que empieza el jugador.")]
        public List<SaldoMoneda> saldoInicial = new List<SaldoMoneda> { new SaldoMoneda() };

        [Tooltip("Guarda el dinero entre partidas en PlayerPrefs.")]
        public bool guardarEnPlayerPrefs = false;
        [Tooltip("Prefijo de las claves de PlayerPrefs (cámbialo si tienes varios monederos).")]
        public string claveGuardado = "Monedero";

        [Serializable] public class EventoSaldo : UnityEvent<string, int> { }
        [Tooltip("Se llama cada vez que cambia el saldo de una moneda (moneda, saldo nuevo).")]
        public EventoSaldo alCambiarSaldo = new EventoSaldo();

        public event Action<string, int> SaldoCambiado;

        readonly Dictionary<string, int> saldos = new Dictionary<string, int>();
        bool iniciado;

        void Awake() { Iniciar(); }

        void Iniciar()
        {
            if (iniciado) return;
            iniciado = true;
            foreach (var s in saldoInicial)
            {
                if (s == null || string.IsNullOrEmpty(s.moneda)) continue;
                int valor = s.cantidad;
                if (guardarEnPlayerPrefs) valor = PlayerPrefs.GetInt(Clave(s.moneda), s.cantidad);
                saldos[s.moneda] = valor;
            }
        }

        public int Saldo(string moneda)
        {
            Iniciar();
            int v;
            return saldos.TryGetValue(moneda ?? "", out v) ? v : 0;
        }

        /// <summary>Todas las monedas que tiene registradas (para pintar el HUD).</summary>
        public IEnumerable<KeyValuePair<string, int>> Todos()
        {
            Iniciar();
            return saldos;
        }

        public bool Gastar(string moneda, int cantidad)
        {
            if (cantidad < 0) return false;
            if (cantidad == 0) return true;
            int actual = Saldo(moneda);
            if (actual < cantidad) return false;
            Fijar(moneda, actual - cantidad);
            return true;
        }

        public void Anadir(string moneda, int cantidad)
        {
            if (cantidad == 0 || string.IsNullOrEmpty(moneda)) return;
            Fijar(moneda, Mathf.Max(0, Saldo(moneda) + cantidad));
        }

        /// <summary>Para botones de UnityEvent: añade monedas de la moneda por defecto ("monedas").</summary>
        public void AnadirMonedas(int cantidad) { Anadir("monedas", cantidad); }

        public void Fijar(string moneda, int valor)
        {
            Iniciar();
            saldos[moneda] = valor;
            if (guardarEnPlayerPrefs) PlayerPrefs.SetInt(Clave(moneda), valor);
            if (SaldoCambiado != null) SaldoCambiado(moneda, valor);
            alCambiarSaldo.Invoke(moneda, valor);
        }

        string Clave(string moneda) { return claveGuardado + "_" + moneda; }
    }
}
