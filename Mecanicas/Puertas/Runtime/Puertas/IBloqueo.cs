namespace Mecanicas.Puertas
{
    /// <summary>
    /// Cualquier cosa que impide abrir una <see cref="Puerta"/>: cerradura con llave, candado,
    /// pestillo, panel numérico o una señal (palanca, botón...). Una puerta se abre solo si
    /// ninguno de sus bloqueos está activo.
    /// Para crear un bloqueo propio, implementa esta interfaz en un MonoBehaviour y añádelo a la lista de la puerta.
    /// </summary>
    public interface IBloqueo
    {
        /// <summary>True mientras impide abrir.</summary>
        bool Bloqueado { get; }

        /// <summary>Texto que se enseña al intentar abrir, por ejemplo "Cerrada con llave".</summary>
        string Motivo { get; }

        /// <summary>
        /// True si ahora mismo impide CERRAR la puerta (p. ej. un pestillo echado con la puerta abierta choca con el marco).
        /// </summary>
        bool ImpideCerrar { get; }

        /// <summary>
        /// La puerta lo llama cuando el jugador la usa estando bloqueada. Si el bloqueo puede abrirse
        /// desde la propia puerta (una cerradura cuando llevas la llave), hazlo aquí y devuelve true.
        /// </summary>
        bool IntentarDesbloquearDesdePuerta(Interactor interactor, out string mensaje);
    }
}
