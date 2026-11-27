using System;
using UnityEngine;

namespace Mecanicas.Construccion
{
    /// <summary>Acciones del sistema de construcción (para mostrar sus teclas en la leyenda).</summary>
    public enum BuildAction
    {
        ToggleBuild,
        Place,
        Cancel,
        RotateLeft,
        RotateRight,
        SwitchMode,
        Next,
        Previous,
        Demolish,
        ToggleLegend
    }

    /// <summary>Teclas del sistema de construcción. Se editan en el inspector del BuildSystem.</summary>
    [Serializable]
    public class BuildInputBindings
    {
        [Tooltip("Entrar y salir del modo construcción.")]
        public KeyCode toggleBuild = KeyCode.B;
        [Tooltip("Colocar el objeto (o demoler en modo demoler).")]
        public KeyCode place = KeyCode.Mouse0;
        [Tooltip("Salir del modo construcción (o del modo demoler).")]
        public KeyCode cancel = KeyCode.Mouse1;
        [Tooltip("Tecla alternativa para cancelar.")]
        public KeyCode cancelAlt = KeyCode.Escape;
        public KeyCode rotateLeft = KeyCode.Q;
        public KeyCode rotateRight = KeyCode.E;
        [Tooltip("Girar también con la rueda del ratón.")]
        public bool rotateWithScroll = true;
        [Tooltip("Cambiar entre cuadrícula y libre.")]
        public KeyCode switchMode = KeyCode.G;
        [Tooltip("Siguiente objeto del catálogo.")]
        public KeyCode next = KeyCode.Tab;
        [Tooltip("Objeto anterior del catálogo.")]
        public KeyCode previous = KeyCode.None;
        [Tooltip("Las teclas 1-9 eligen directamente el objeto del catálogo.")]
        public bool numberKeysSelect = true;
        [Tooltip("Activar o desactivar el modo demoler.")]
        public KeyCode demolish = KeyCode.X;
        [Tooltip("Mostrar u ocultar la leyenda.")]
        public KeyCode toggleLegend = KeyCode.H;

        public KeyCode Get(BuildAction action)
        {
            switch (action)
            {
                case BuildAction.ToggleBuild: return toggleBuild;
                case BuildAction.Place: return place;
                case BuildAction.Cancel: return cancel;
                case BuildAction.RotateLeft: return rotateLeft;
                case BuildAction.RotateRight: return rotateRight;
                case BuildAction.SwitchMode: return switchMode;
                case BuildAction.Next: return next;
                case BuildAction.Previous: return previous;
                case BuildAction.Demolish: return demolish;
                case BuildAction.ToggleLegend: return toggleLegend;
                default: return KeyCode.None;
            }
        }
    }
}
