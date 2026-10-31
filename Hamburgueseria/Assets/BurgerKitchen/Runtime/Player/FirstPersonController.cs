using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Movimiento en primera persona con CharacterController: andar, correr, gravedad y mirar con ratón/stick.
    /// Jerarquía esperada: Player (este script) → CameraPivot (a la altura de los ojos) → Main Camera.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public PlayerInputReader input;
        [Tooltip("Transform que cabecea (arriba/abajo). Normalmente el padre de la cámara o la propia cámara.")]
        public Transform cameraPivot;

        [Header("Movimiento")]
        public float walkSpeed = 3.2f;
        public float sprintSpeed = 5.2f;
        public float acceleration = 25f;
        public float gravity = -20f;

        [Header("Mirar")]
        public float minPitch = -85f;
        public float maxPitch = 85f;

        [Header("Balanceo de cámara")]
        public bool headBob = true;
        public float bobAmplitude = 0.035f;
        public float bobFrequency = 9f;

        CharacterController controller;
        Vector3 planarVelocity;
        float verticalVelocity;
        float pitch;
        float bobTime;
        Vector3 pivotBaseLocalPos;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (cameraPivot == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraPivot = cam.transform;
            }
            if (cameraPivot != null)
            {
                pivotBaseLocalPos = cameraPivot.localPosition;
                pitch = NormalizeAngle(cameraPivot.localEulerAngles.x);
            }
        }

        void Update()
        {
            if (input == null) return;
            float dt = Time.deltaTime;

            // Mirar
            Vector2 look = input.Look;
            transform.Rotate(0f, look.x, 0f, Space.Self);
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // Mover
            Vector2 m = input.Move;
            Vector3 wish = (transform.right * m.x + transform.forward * m.y);
            float speed = input.SprintHeld ? sprintSpeed : walkSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, wish * speed, acceleration * dt);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);

            // Balanceo
            if (headBob && cameraPivot != null)
            {
                float s = planarVelocity.magnitude;
                if (s > 0.1f && controller.isGrounded) bobTime += dt * bobFrequency * (s / walkSpeed);
                else bobTime = Mathf.MoveTowards(bobTime, Mathf.Round(bobTime / Mathf.PI) * Mathf.PI, dt * 4f);
                cameraPivot.localPosition = pivotBaseLocalPos + new Vector3(0f, Mathf.Sin(bobTime * 2f) * bobAmplitude, 0f);
            }
        }

        static float NormalizeAngle(float a)
        {
            a %= 360f;
            return a > 180f ? a - 360f : a;
        }
    }
}
