using UnityEngine;
#if BK_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BurgerKitchen
{
    /// <summary>
    /// Lee teclado, ratón y mando. Funciona con el Input System nuevo (si está instalado y activo)
    /// o con el Input Manager clásico, sin configurar acciones.
    ///
    /// Teclado y ratón: WASD mover · ratón mirar · E / clic izq. acción · F / clic der. (mantener) trabajar ·
    /// Q / rueda cambiar opción · G soltar · Shift correr · Enter abrir local / siguiente día · F1 ayuda · Esc liberar ratón.
    /// Mando: stick izq. mover · stick der. mirar · A acción · X trabajar · LB/RB cambiar · Y soltar · L3 correr · Start abrir.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Sensibilidad")]
        [Tooltip("Grados por píxel de ratón (Input System).")]
        public float mouseSensitivity = 0.1f;
        [Tooltip("Multiplicador para el Input Manager clásico.")]
        public float legacyMouseSensitivity = 2f;
        [Tooltip("Grados por segundo con el stick derecho.")]
        public float gamepadLookSpeed = 180f;
        public bool invertY;

        [Header("Ratón")]
        public bool lockCursorOnStart = true;

        public Vector2 Move { get; private set; }
        /// <summary>Grados a girar este frame (x = guiñada, y = cabeceo).</summary>
        public Vector2 Look { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool AltHeld { get; private set; }
        public bool AltPressed { get; private set; }
        public int CycleDirection { get; private set; }
        public bool DropPressed { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool StartPressed { get; private set; }
        public bool HelpPressed { get; private set; }

        /// <summary>Si es true (menús abiertos), la jugabilidad no recibe entrada.</summary>
        public static bool GameplayBlocked;

        public bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

        void Start()
        {
            if (lockCursorOnStart) SetCursorLocked(true);
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            Clear();
            ReadDevices();

            if (GameplayBlocked)
            {
                Move = Vector2.zero; Look = Vector2.zero;
                InteractPressed = AltHeld = AltPressed = DropPressed = SprintHeld = false;
                CycleDirection = 0;
                return;
            }

            if (!CursorLocked)
            {
                Look = Vector2.zero;
                // Un clic vuelve a capturar el ratón sin disparar una acción.
                if (InteractPressed) { SetCursorLocked(true); InteractPressed = false; }
            }
        }

        void Clear()
        {
            Move = Vector2.zero; Look = Vector2.zero;
            InteractPressed = AltHeld = AltPressed = DropPressed = SprintHeld = StartPressed = HelpPressed = false;
            CycleDirection = 0;
        }

#if BK_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        void ReadDevices()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;
            Vector2 move = Vector2.zero, look = Vector2.zero;
            int cycle = 0;

            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
                InteractPressed |= kb.eKey.wasPressedThisFrame;
                AltHeld |= kb.fKey.isPressed;
                AltPressed |= kb.fKey.wasPressedThisFrame;
                DropPressed |= kb.gKey.wasPressedThisFrame;
                SprintHeld |= kb.leftShiftKey.isPressed;
                StartPressed |= kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
                HelpPressed |= kb.f1Key.wasPressedThisFrame;
                if (kb.qKey.wasPressedThisFrame) cycle += 1;
                if (kb.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
            }
            if (mouse != null)
            {
                look += mouse.delta.ReadValue() * mouseSensitivity;
                InteractPressed |= mouse.leftButton.wasPressedThisFrame;
                AltHeld |= mouse.rightButton.isPressed;
                AltPressed |= mouse.rightButton.wasPressedThisFrame;
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.01f) cycle += 1;
                else if (scroll < -0.01f) cycle -= 1;
            }
            if (pad != null)
            {
                move += pad.leftStick.ReadValue();
                look += pad.rightStick.ReadValue() * (gamepadLookSpeed * Time.deltaTime);
                InteractPressed |= pad.buttonSouth.wasPressedThisFrame;
                AltHeld |= pad.buttonWest.isPressed;
                AltPressed |= pad.buttonWest.wasPressedThisFrame;
                DropPressed |= pad.buttonNorth.wasPressedThisFrame;
                SprintHeld |= pad.leftStickButton.isPressed;
                StartPressed |= pad.startButton.wasPressedThisFrame;
                HelpPressed |= pad.selectButton.wasPressedThisFrame;
                if (pad.rightShoulder.wasPressedThisFrame) cycle += 1;
                if (pad.leftShoulder.wasPressedThisFrame) cycle -= 1;
            }

            Move = Vector2.ClampMagnitude(move, 1f);
            if (invertY) look.y = -look.y;
            Look = look;
            CycleDirection = cycle > 0 ? 1 : cycle < 0 ? -1 : 0;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        void ReadDevices()
        {
            Move = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            var look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * legacyMouseSensitivity;
            if (invertY) look.y = -look.y;
            Look = look;
            InteractPressed = Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0);
            AltHeld = Input.GetKey(KeyCode.F) || Input.GetMouseButton(1);
            AltPressed = Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(1);
            DropPressed = Input.GetKeyDown(KeyCode.G);
            SprintHeld = Input.GetKey(KeyCode.LeftShift);
            StartPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            HelpPressed = Input.GetKeyDown(KeyCode.F1);
            float scroll = Input.mouseScrollDelta.y;
            int cycle = Input.GetKeyDown(KeyCode.Q) ? 1 : 0;
            if (scroll > 0.01f) cycle += 1; else if (scroll < -0.01f) cycle -= 1;
            CycleDirection = cycle > 0 ? 1 : cycle < 0 ? -1 : 0;
            if (Input.GetKeyDown(KeyCode.Escape)) SetCursorLocked(false);
        }
#else
        void ReadDevices()
        {
            if (!warned)
            {
                warned = true;
                Debug.LogError("[BurgerKitchen] No hay sistema de entrada activo. Instala el paquete Input System " +
                               "o activa el Input Manager en Project Settings > Player > Active Input Handling.");
            }
        }
        bool warned;
#endif
    }
}
