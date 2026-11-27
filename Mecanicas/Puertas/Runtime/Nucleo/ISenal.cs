using System;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// Algo que puede estar activo o inactivo: palanca, botón, interruptor, panel numérico, combinador...
    /// Lo usan <see cref="BloqueoPorSenal"/> y <see cref="CombinadorSenales"/> para encadenar mecanismos.
    /// </summary>
    public interface ISenal
    {
        bool Activa { get; }
        /// <summary>Se lanza con el nuevo estado cada vez que cambia.</summary>
        event Action<bool> AlCambiarSenal;
    }
}
