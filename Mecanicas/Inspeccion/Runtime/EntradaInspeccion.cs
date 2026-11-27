using UnityEngine;
#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mecanicas.Inspeccion
{
    /// <summary>
    /// Lee los controles del sistema de agarre e inspección.
    ///
    /// Funciona sin configurar nada con el paquete Input System (si está instalado y activo en
    /// Project Settings > Player > Active Input Handling) o con el Input Manager clásico.
    /// Cada acción se puede reasignar desde el Inspector: rutas de Input System (por ejemplo
    /// "&lt;Keyboard&gt;/e" o "&lt;Gamepad&gt;/buttonWest") y teclas KeyCode para el Input Manager clásico.
    ///
    /// Si tu proyecto ya tiene su propio sistema de entrada, puedes escribir a las propiedades
    /// desde fuera: activa "Entrada Externa" y rellena los valores con <see cref="EstablecerExterna"/>.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    [AddComponentMenu("Mecánicas/Inspección/Entrada Inspección")]
    public class EntradaInspeccion : MonoBehaviour
    {
        [Header("Input Manager clásico (teclas)")]
        [Tooltip("Coger un objeto o soltarlo si ya lo tienes en la mano.")]
        public KeyCode teclaAgarrar = KeyCode.E;
        [Tooltip("Empezar o terminar la inspección del objeto.")]
        public KeyCode teclaInspeccionar = KeyCode.R;
        [Tooltip("Mantener para cargar el lanzamiento y soltar para lanzar.")]
        public KeyCode teclaLanzar = KeyCode.Mouse0;
        [Tooltip("Salir de la inspección (además de la tecla de inspeccionar).")]
        public KeyCode teclaSalir = KeyCode.Q;
        [Tooltip("Mantener para rotar el objeto inspeccionado si 'Rotar Solo Con Boton' está activo.")]
        public KeyCode teclaRotar = KeyCode.Mouse1;
        [Tooltip("Multiplicador de los ejes 'Mouse X' y 'Mouse Y' (grados por unidad de eje).")]
        public float sensibilidadRatonClasica = 3f;

        [Header("Input System (rutas de controles)")]
        public string[] rutasAgarrar = { "<Keyboard>/e", "<Gamepad>/buttonWest" };
        public string[] rutasInspeccionar = { "<Keyboard>/r", "<Gamepad>/buttonNorth" };
        public string[] rutasLanzar = { "<Mouse>/leftButton", "<Gamepad>/rightTrigger" };
        public string[] rutasSalir = { "<Keyboard>/q", "<Gamepad>/buttonEast" };
        public string[] rutasRotar = { "<Mouse>/rightButton", "<Gamepad>/leftTrigger" };
        [Tooltip("Grados por píxel de ratón.")]
        public float sensibilidadRaton = 0.3f;
        [Tooltip("Grados por segundo con el stick derecho del mando.")]
        public float velocidadStick = 180f;
        [Tooltip("Unidades de zoom por segundo con la cruceta del mando.")]
        public float velocidadZoomMando = 2f;

        [Header("Entrada externa")]
        [Tooltip("Si está activo, este componente no lee dispositivos: otro script rellena los valores con EstablecerExterna().")]
        public bool entradaExterna;

        /// <summary>Se pulsó este frame la tecla de agarrar / soltar.</summary>
        public bool AgarrarPulsado { get; private set; }
        /// <summary>Se pulsó este frame la tecla de inspeccionar.</summary>
        public bool InspeccionarPulsado { get; private set; }
        /// <summary>La tecla de lanzar se pulsó este frame.</summary>
        public bool LanzarPulsado { get; private set; }
        /// <summary>La tecla de lanzar está mantenida.</summary>
        public bool LanzarMantenido { get; private set; }
        /// <summary>La tecla de lanzar se soltó este frame.</summary>
        public bool LanzarSoltado { get; private set; }
        /// <summary>Se pulsó este frame la tecla de salir de la inspección.</summary>
        public bool SalirPulsado { get; private set; }
        /// <summary>La tecla de rotar está mantenida.</summary>
        public bool RotarMantenido { get; private set; }
        /// <summary>Grados a rotar este frame (x = horizontal, y = vertical).</summary>
        public Vector2 Rotacion { get; private set; }
        /// <summary>Zoom este frame (positivo = acercar).</summary>
        public float Zoom { get; private set; }

#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        InputAction _agarrar, _inspeccionar, _lanzar, _salir, _rotar;
        InputAction _raton, _stick, _rueda, _cruceta;

        static InputAction CrearBoton(string nombre, string[] rutas)
        {
            var accion = new InputAction(nombre, InputActionType.Button);
            if (rutas != null)
                foreach (var ruta in rutas)
                    if (!string.IsNullOrEmpty(ruta)) accion.AddBinding(ruta);
            return accion;
        }

        void OnEnable()
        {
            _agarrar = CrearBoton("Agarrar", rutasAgarrar);
            _inspeccionar = CrearBoton("Inspeccionar", rutasInspeccionar);
            _lanzar = CrearBoton("Lanzar", rutasLanzar);
            _salir = CrearBoton("Salir", rutasSalir);
            _rotar = CrearBoton("Rotar", rutasRotar);
            _raton = new InputAction("Raton", InputActionType.Value, "<Mouse>/delta");
            _stick = new InputAction("Stick", InputActionType.Value, "<Gamepad>/rightStick");
            _rueda = new InputAction("Rueda", InputActionType.Value, "<Mouse>/scroll/y");
            _cruceta = new InputAction("Cruceta", InputActionType.Value, "<Gamepad>/dpad/y");
            foreach (var a in Todas()) a.Enable();
        }

        void OnDisable()
        {
            foreach (var a in Todas())
            {
                if (a == null) continue;
                a.Disable();
                a.Dispose();
            }
        }

        InputAction[] Todas() => new[] { _agarrar, _inspeccionar, _lanzar, _salir, _rotar, _raton, _stick, _rueda, _cruceta };
#endif

        void Update()
        {
            if (entradaExterna) return;

#if MEC_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            AgarrarPulsado = _agarrar.WasPressedThisFrame();
            InspeccionarPulsado = _inspeccionar.WasPressedThisFrame();
            LanzarPulsado = _lanzar.WasPressedThisFrame();
            LanzarMantenido = _lanzar.IsPressed();
            LanzarSoltado = _lanzar.WasReleasedThisFrame();
            SalirPulsado = _salir.WasPressedThisFrame();
            RotarMantenido = _rotar.IsPressed();
            Vector2 rot = _raton.ReadValue<Vector2>() * sensibilidadRaton;
            rot += _stick.ReadValue<Vector2>() * (velocidadStick * Time.unscaledDeltaTime);
            Rotacion = rot;
            float rueda = _rueda.ReadValue<float>();
            Zoom = Mathf.Sign(rueda) * (Mathf.Abs(rueda) > 0.01f ? 1f : 0f) * 0.1f
                 + _cruceta.ReadValue<float>() * velocidadZoomMando * Time.unscaledDeltaTime;
#elif ENABLE_LEGACY_INPUT_MANAGER
            AgarrarPulsado = Input.GetKeyDown(teclaAgarrar);
            InspeccionarPulsado = Input.GetKeyDown(teclaInspeccionar);
            LanzarPulsado = Input.GetKeyDown(teclaLanzar);
            LanzarMantenido = Input.GetKey(teclaLanzar);
            LanzarSoltado = Input.GetKeyUp(teclaLanzar);
            SalirPulsado = Input.GetKeyDown(teclaSalir);
            RotarMantenido = Input.GetKey(teclaRotar);
            Rotacion = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * sensibilidadRatonClasica;
            float rueda = Input.mouseScrollDelta.y;
            Zoom = Mathf.Abs(rueda) > 0.01f ? Mathf.Sign(rueda) * 0.1f : 0f;
#endif
        }

        /// <summary>
        /// Rellena la entrada desde otro sistema (activa antes "Entrada Externa").
        /// Llama a este método una vez por frame, antes de que se ejecute el agarrador.
        /// </summary>
        public void EstablecerExterna(bool agarrarPulsado, bool inspeccionarPulsado, bool lanzarPulsado,
            bool lanzarMantenido, bool lanzarSoltado, bool salirPulsado, bool rotarMantenido,
            Vector2 rotacionGrados, float zoom)
        {
            AgarrarPulsado = agarrarPulsado;
            InspeccionarPulsado = inspeccionarPulsado;
            LanzarPulsado = lanzarPulsado;
            LanzarMantenido = lanzarMantenido;
            LanzarSoltado = lanzarSoltado;
            SalirPulsado = salirPulsado;
            RotarMantenido = rotarMantenido;
            Rotacion = rotacionGrados;
            Zoom = zoom;
        }
    }
}
