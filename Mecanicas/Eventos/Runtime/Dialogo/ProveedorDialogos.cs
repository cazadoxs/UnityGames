using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>Una frase de diálogo simple.</summary>
    [Serializable]
    public class LineaDialogo
    {
        [Tooltip("Quién habla. Puede ir vacío.")]
        public string hablante = "";

        [TextArea(2, 5)] public string texto = "";

        [Tooltip("Segundos en pantalla. 0 = automático según la longitud del texto (o la duración de la voz).")]
        [Min(0f)] public float duracion = 0f;

        [Tooltip("Voz o sonido opcional que suena con la frase.")]
        public AudioClip voz;
    }

    /// <summary>
    /// Contrato para que cualquier sistema de diálogos (el tuyo, el de Mecanicas/Dialogos o uno de la Asset Store)
    /// reciba los diálogos que lanzan los eventos, sin que este paquete dependa de él.
    /// Impleméntalo en un componente y regístralo con <see cref="ProveedorDialogos.Registrar"/>.
    /// </summary>
    public interface IProveedorDialogo
    {
        /// <summary>
        /// Muestra un diálogo. <paramref name="dialogo"/> es el asset que pusiste en la acción (puede ser null);
        /// <paramref name="lineas"/> son las frases escritas en la acción (puede estar vacía).
        /// Llama a <paramref name="alTerminar"/> cuando el diálogo acabe, para que el evento continúe.
        /// </summary>
        void Reproducir(UnityEngine.Object dialogo, IReadOnlyList<LineaDialogo> lineas, Action alTerminar);
    }

    /// <summary>Registro del sistema de diálogos que está activo. Si no hay ninguno se usan subtítulos simples.</summary>
    public static class ProveedorDialogos
    {
        /// <summary>Proveedor actual, o null si no hay ninguno registrado.</summary>
        public static IProveedorDialogo Actual { get; private set; }

        public static void Registrar(IProveedorDialogo proveedor) => Actual = proveedor;

        /// <summary>Quita el proveedor solo si es el que está registrado.</summary>
        public static void Quitar(IProveedorDialogo proveedor)
        {
            if (Actual == proveedor) Actual = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstatico() => Actual = null;
    }
}
