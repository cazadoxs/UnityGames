using System;

namespace Mecanicas.Construccion
{
    /// <summary>Forma de colocar un objeto.</summary>
    public enum PlacementMode
    {
        /// <summary>Encaja en las celdas de un <see cref="BuildGrid"/>.</summary>
        Grid = 0,
        /// <summary>Se coloca donde apunta el jugador, sin superponerse a nada.</summary>
        Free = 1
    }

    /// <summary>Modos que permite un objeto construible.</summary>
    [Flags]
    public enum AllowedPlacementModes
    {
        Grid = 1 << 0,
        Free = 1 << 1,
        Both = Grid | Free
    }

    /// <summary>Resultado de comprobar una posición. Todo lo distinto de <see cref="Valid"/> es un motivo de rechazo.</summary>
    public enum PlacementResult
    {
        Valid = 0,
        /// <summary>No se apunta a ningún suelo ni cuadrícula.</summary>
        NoSurface,
        /// <summary>La huella se sale de la cuadrícula.</summary>
        OutsideGrid,
        /// <summary>Alguna celda ya está ocupada o bloqueada.</summary>
        CellsOccupied,
        /// <summary>La caja del objeto choca con otro objeto o con un obstáculo.</summary>
        Overlapping,
        /// <summary>El suelo está demasiado inclinado.</summary>
        TooSteep,
        /// <summary>La posición está más lejos de lo permitido.</summary>
        TooFar,
        /// <summary>Está dentro de una zona prohibida o fuera de una zona permitida.</summary>
        OutsideBuildZone,
        /// <summary>No hay dinero o recursos suficientes.</summary>
        CannotAfford,
        /// <summary>El objeto no admite el modo actual (cuadrícula o libre).</summary>
        ModeNotAllowed,
        /// <summary>Lo ha rechazado una regla personalizada (<see cref="BuildRule"/>).</summary>
        CustomRule
    }

    /// <summary>Qué está haciendo el sistema de construcción.</summary>
    public enum BuildState
    {
        Inactive = 0,
        Placing = 1,
        Demolishing = 2
    }
}
