using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Llavero sencillo del jugador. Ponlo en el objeto del jugador (o en la cámara).
    /// Si ya tienes un inventario, puedes sustituirlo implementando <see cref="IPortadorLlaves"/>.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/Llavero del jugador")]
    public class Llavero : MonoBehaviour, IPortadorLlaves
    {
        [Serializable]
        public struct EntradaLlave
        {
            public string id;
            public string nombre;
        }

        [Tooltip("Llaves con las que empieza el jugador.")]
        [SerializeField] List<EntradaLlave> llavesIniciales = new List<EntradaLlave>();

        readonly Dictionary<string, string> llaves = new Dictionary<string, string>();

        /// <summary>Se lanza al añadir o quitar llaves.</summary>
        public event Action AlCambiar;

        public IReadOnlyDictionary<string, string> Llaves => llaves;

        void Awake()
        {
            foreach (var e in llavesIniciales)
                if (!string.IsNullOrEmpty(e.id)) llaves[e.id] = string.IsNullOrEmpty(e.nombre) ? e.id : e.nombre;
        }

        public bool TieneLlave(string idLlave) => !string.IsNullOrEmpty(idLlave) && llaves.ContainsKey(idLlave);

        public void AnadirLlave(string idLlave, string nombreVisible)
        {
            if (string.IsNullOrEmpty(idLlave)) return;
            llaves[idLlave] = string.IsNullOrEmpty(nombreVisible) ? idLlave : nombreVisible;
            AlCambiar?.Invoke();
        }

        public void QuitarLlave(string idLlave)
        {
            if (llaves.Remove(idLlave)) AlCambiar?.Invoke();
        }

        /// <summary>Versión para UnityEvent: añade una llave cuyo nombre visible es el propio id.</summary>
        public void AnadirLlave(string idLlave) => AnadirLlave(idLlave, idLlave);
    }
}
