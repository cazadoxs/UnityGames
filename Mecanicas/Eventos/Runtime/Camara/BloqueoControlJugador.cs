using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>
    /// Registro global de "quién está bloqueando el control del jugador".
    /// Las acciones (mirar con la cámara, diálogos...) llaman a Bloquear/Liberar, y tu controlador
    /// de primera persona consulta <see cref="Bloqueado"/> para dejar de mover la cámara y el cuerpo.
    /// Varias acciones pueden bloquear a la vez: el control vuelve cuando todas lo han liberado.
    /// </summary>
    public static class BloqueoControlJugador
    {
        private static readonly HashSet<object> bloqueadores = new HashSet<object>();

        /// <summary>Se lanza con true al pasar a bloqueado y con false al recuperar el control.</summary>
        public static event Action<bool> Cambio;

        /// <summary>True si algo está bloqueando el control del jugador.</summary>
        public static bool Bloqueado => bloqueadores.Count > 0;

        public static void Bloquear(object quien)
        {
            if (quien == null) return;
            bool antes = Bloqueado;
            bloqueadores.Add(quien);
            if (!antes && Bloqueado) Cambio?.Invoke(true);
        }

        public static void Liberar(object quien)
        {
            if (quien == null) return;
            bool antes = Bloqueado;
            bloqueadores.Remove(quien);
            if (antes && !Bloqueado) Cambio?.Invoke(false);
        }

        /// <summary>Quita todos los bloqueos (por ejemplo al cargar otra escena).</summary>
        public static void LiberarTodo()
        {
            bool antes = Bloqueado;
            bloqueadores.Clear();
            if (antes) Cambio?.Invoke(false);
        }

        // Con "Enter Play Mode Options" sin recarga de dominio los estáticos sobreviven entre partidas.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico()
        {
            bloqueadores.Clear();
            Cambio = null;
        }
    }

    /// <summary>
    /// Impleméntalo en tu controlador de primera persona si quieres que la cámara se quede mirando
    /// hacia donde la dejó un evento (opción "Mantener" de <see cref="AccionMirarCamara"/>).
    /// Sin él, el sistema funciona igual pero la mirada solo puede volver al punto de partida.
    /// </summary>
    public interface IControlCamaraJugador
    {
        /// <summary>Recibe la rotación final de la cámara en el mundo; ajusta tu giro horizontal y vertical a ella.</summary>
        void AplicarRotacionMundo(Quaternion rotacionCamara);
    }
}
