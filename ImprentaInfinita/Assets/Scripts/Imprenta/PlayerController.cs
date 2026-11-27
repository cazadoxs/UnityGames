using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Imprenta
{
    public interface IInteractable
    {
        /// <summary>Texto de ayuda al mirar el objeto; null si no hay nada que hacer.</summary>
        string Prompt(Game g);
        void Interact(Game g);
    }

    /// <summary>Primera persona: andar, mirar, coger hojas de la montaña y usar objetos (clic izquierdo o E).</summary>
    public class PlayerController : MonoBehaviour
    {
        public Camera cam;
        public float sensitivity = 0.12f;
        public float walkSpeed = 4.2f, runSpeed = 7.5f;
        public float Stamina = 1f;
        bool winded;
        float baseFov = -1;
        CharacterController cc;
        float yaw, pitch, vy;
        float pickTimer, lastPick = -10f;
        Transform hand;
        readonly List<GameObject> handSheets = new List<GameObject>();
        GameObject containerVis;
        string containerShown;
        Vector3 bob;
        float bobT;

        public IInteractable Focus { get; private set; }
        public bool LookingAtMountain { get; private set; }
        public Vector3 MountainPoint { get; private set; }
        public float PickProgress => pickTimer;
        public float Pitch => pitch;

        void Awake()
        {
            cc = gameObject.AddComponent<CharacterController>();
            cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.875f, 0); cc.stepOffset = 0.3f;
            var camGo = new GameObject("Camara");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.62f, 0);
            cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f; cam.farClipPlane = 400f; cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            var data = camGo.GetComponent<UniversalAdditionalCameraData>() ?? camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.tag = "MainCamera";

            hand = Build.Empty("Mano", camGo.transform, new Vector3(0.28f, -0.3f, 0.5f), new Vector3(-55, -10, 0)).transform;
        }

        public void SetLook(float newYaw, float newPitch) { yaw = newYaw; pitch = newPitch; ApplyLook(); }

        public void Teleport(Vector3 pos, float newYaw)
        {
            cc.enabled = false; transform.position = pos; cc.enabled = true;
            yaw = newYaw; pitch = 0; ApplyLook();
        }

        void ApplyLook()
        {
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        void Update()
        {
            var g = Game.I;
            if (g == null) return;
            UpdateHandVisual(g);
            if (g.Mode != GameMode.Walking) { Focus = null; LookingAtMountain = false; pickTimer = 0; return; }

            var kb = Keyboard.current; var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue() * sensitivity;
                yaw += d.x; pitch = Mathf.Clamp(pitch - d.y, -85, 85);
                ApplyLook();
            }
            Vector3 input = Vector3.zero;
            bool run = false, jump = false;
            if (kb != null)
            {
                if (kb.wKey.isPressed) input.z += 1;
                if (kb.sKey.isPressed) input.z -= 1;
                if (kb.dKey.isPressed) input.x += 1;
                if (kb.aKey.isPressed) input.x -= 1;
                run = kb.leftShiftKey.isPressed;
                jump = kb.spaceKey.wasPressedThisFrame;
            }
            if (input.sqrMagnitude > 1) input.Normalize();
            // aliento: correr lo gasta, andar o estar quieto lo recupera (con un respiro si se agota)
            bool moving = input.sqrMagnitude > 0.01f;
            if (run && moving && Stamina > 0 && !winded) { Stamina -= Time.deltaTime / 6f; if (Stamina <= 0) { Stamina = 0; winded = true; } }
            else { run = false; Stamina = Mathf.Min(1f, Stamina + Time.deltaTime / (moving ? 7f : 3.5f)); if (winded && Stamina > 0.35f) winded = false; }
            if (winded) run = false;
            float fovTarget = baseFov < 0 ? (baseFov = cam.fieldOfView) : baseFov;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, run && moving ? fovTarget + 6f : fovTarget, Time.deltaTime * 6f);
            Vector3 move = transform.TransformDirection(input) * (run ? runSpeed : walkSpeed);
            // La montaña no tiene colisiones por columna: se bloquea con su campo de alturas
            var m = g.mountain;
            Vector3 p = transform.position, step = move * Time.deltaTime;
            if (m.HeightAt(p.x + step.x * 4, p.z) > p.y + 0.45f) move.x = 0;
            if (m.HeightAt(p.x, p.z + step.z * 4) > p.y + 0.45f) move.z = 0;
            if (cc.isGrounded) { vy = -1f; if (jump) vy = 4.2f; }
            vy += Physics.gravity.y * Halls.Gravity(g.S.hall) * Time.deltaTime;
            move.y = vy;
            cc.Move(move * Time.deltaTime);
            // balanceo suave de la cámara al andar
            float speed = new Vector2(move.x, move.z).magnitude;
            bobT += Time.deltaTime * speed * 1.8f;
            bob = Vector3.Lerp(bob, speed > 0.1f ? new Vector3(Mathf.Cos(bobT) * 0.015f, Mathf.Abs(Mathf.Sin(bobT)) * 0.025f, 0) : Vector3.zero, Time.deltaTime * 8);
            cam.transform.localPosition = new Vector3(0, 1.62f, 0) + bob;

            Interactions(g, mouse, kb);
        }

        void Interactions(Game g, Mouse mouse, Keyboard kb)
        {
            Focus = null; LookingAtMountain = false;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            float objDist = 99;
            if (Physics.Raycast(ray, out var hit, 3.2f, ~(1 << Build.AutoColliderLayer), QueryTriggerInteraction.Collide))
            {
                Focus = hit.collider.GetComponentInParent<IInteractable>();
                objDist = hit.distance;
                // la ladera tiene colisión física, pero se recoge con la marcha propia de la montaña
                if (Focus == null && hit.collider.transform.IsChildOf(g.mountain.transform)) objDist = 99;
            }
            float range = Econ.PickRange(g.S);
            if (Focus == null && g.mountain.Raycast(ray.origin, ray.direction, Mathf.Min(range, objDist), out var mp))
            {
                LookingAtMountain = true; MountainPoint = mp;
            }

            bool clickDown = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (kb != null && kb.eKey.wasPressedThisFrame);
            bool clickHeld = (mouse != null && mouse.leftButton.isPressed) || (kb != null && kb.eKey.isPressed);

            if (Focus != null && clickDown) { Focus.Interact(g); return; }

            if (LookingAtMountain && clickHeld)
            {
                if (g.CarryCount >= Econ.Capacity(g.S)) { if (clickDown) { g.Toast("Vas lleno: " + Econ.ContainerName(g.S) + " (" + Econ.Capacity(g.S) + ")"); Sfx.Play("deny", 0.5f); } pickTimer = 0; return; }
                float need = Econ.HasGun(g.S) ? Econ.GunSeconds(g.S) : Econ.PickSeconds(g.S);
                // El primer clic coge al momento; mantener pulsado repite al ritmo de la herramienta
                if (Time.time - lastPick >= need)
                {
                    lastPick = Time.time;
                    g.PickSheet(MountainPoint);
                    g.SpawnFlyingSheet(MountainPoint, hand.position, 0.25f);
                    // al tirar de una hoja se sueltan otras que resbalan por la ladera
                    if (UnityEngine.Random.value < 0.45f)
                    {
                        var dirOut = (cam.transform.position - MountainPoint); dirOut.y = 0; dirOut.Normalize();
                        Paper.Spawn(MountainPoint + Vector3.up * 0.15f + dirOut * 0.1f, UnityEngine.Random.rotation, dirOut * UnityEngine.Random.Range(0.3f, 1.2f) + Vector3.up * 0.5f,
                            UnityEngine.Random.value < 0.3f ? new PageData { text = Phrases.Get(UnityEngine.Random.Range(0, 2000000), 91u, 0, out _), quality = 1, value = Econ.RecycleValue * 4 } : null);
                    }
                }
                pickTimer = Mathf.Clamp01((Time.time - lastPick) / need);
            }
            else pickTimer = 0;
        }

        public Vector3 HandPosition => hand.position;
        public void SetFov(float f) { baseFov = f; cam.fieldOfView = f; }

        // al andar entre hojas sueltas se levantan y se arrastran
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic) return;
            var v = hit.controller.velocity; v.y = 0;
            rb.WakeUp();
            rb.AddForce(v * 0.0012f + Vector3.up * 0.0006f * v.magnitude, ForceMode.Impulse);
        }

        void UpdateHandVisual(Game g)
        {
            string cname = Econ.ContainerName(g.S);
            if (cname != containerShown)
            {
                containerShown = cname;
                if (containerVis) Destroy(containerVis);
                containerVis = null;
                var dark = Build.Mat(Build.Hex("#2b2b2b"), 0.4f, 0.4f);
                switch (cname)
                {
                    case "Carpeta con pinza":
                        containerVis = Build.Box("Carpeta", hand, new Vector3(0, -0.006f, 0), new Vector3(0.24f, 0.008f, 0.33f), Build.Mat(Build.Hex("#7b4a2b"), 0.3f), false);
                        Build.Box("Pinza", containerVis.transform, new Vector3(0, 0.8f, 0.42f), new Vector3(0.4f, 1.2f, 0.08f), Build.Mat(Build.Steel, 0.7f, 1f), false);
                        break;
                    case "Bandeja de oficina":
                        containerVis = Build.Box("Bandeja", hand, new Vector3(0, -0.01f, 0), new Vector3(0.26f, 0.01f, 0.34f), dark, false);
                        Build.Box("Borde", containerVis.transform, new Vector3(0, 3, 0.5f), new Vector3(1, 6, 0.05f), dark, false);
                        Build.Box("Borde", containerVis.transform, new Vector3(0.5f, 3, 0), new Vector3(0.05f, 6, 1), dark, false);
                        Build.Box("Borde", containerVis.transform, new Vector3(-0.5f, 3, 0), new Vector3(0.05f, 6, 1), dark, false);
                        break;
                    case "Caja de folios":
                        containerVis = Build.Box("Caja", hand, new Vector3(0, 0.05f, 0), new Vector3(0.25f, 0.12f, 0.33f), Build.Mat(Build.Hex("#b08a55"), 0.15f), false);
                        break;
                    case "Depósito de la pistola":
                        containerVis = Build.Empty("Pistola", hand, Vector3.zero);
                        // orientada respecto a la cámara (la mano va inclinada para enseñar las hojas)
                        containerVis.transform.localRotation = Quaternion.Inverse(hand.localRotation) * Quaternion.Euler(-10, 198, 0); // el modelo se exportó con la boca hacia atrás
                        containerVis.transform.localPosition = Quaternion.Inverse(hand.localRotation) * new Vector3(0.02f, -0.02f, 0.1f);
                        containerVis.transform.localScale = Vector3.one * 0.8f;
                        var yellow = Build.Mat(Build.Safety, 0.55f);
                        if (Build.Model("Pistola", containerVis.transform, Vector3.zero, null, n =>
                                n == "Plastico" ? yellow : n == "Cromo" ? Build.Chrome : n == "Goma" ? Build.Rubber : n == "Cristal" ? Build.Glass :
                                n == "Teclas" ? Build.Mat(Build.Hex("#c0392b"), 0.5f) : n == "Etiqueta" ? Build.Mat(Build.Hex("#1d1d1d"), 0.4f) : null, false) == null)
                        {
                            Build.Box("Cuerpo", containerVis.transform, new Vector3(0, 0, 0), new Vector3(0.09f, 0.1f, 0.25f), yellow, false);
                            Build.Cyl("Boca", containerVis.transform, new Vector3(0, 0.01f, 0.2f), 0.07f, 0.2f, Build.Mat(Build.SteelDark, 0.6f, 0.9f), false, new Vector3(90, 0, 0));
                            Build.Box("Empuñadura", containerVis.transform, new Vector3(0, -0.09f, -0.06f), new Vector3(0.05f, 0.12f, 0.05f), dark, false, new Vector3(15, 0, 0));
                        }
                        break;
                }
            }
            // Pila visible de hojas (en blanco abajo, escritas encima)
            int total = g.CarryCount;
            int show = Mathf.Min(total, 40);
            while (handSheets.Count < show)
            {
                var s = Build.Box("HojaMano", hand, Vector3.zero, new Vector3(0.21f, 0.0025f, 0.297f), Build.Mat(Build.Paper, 0.2f), false);
                handSheets.Add(s);
            }
            bool gun = containerShown == "Depósito de la pistola";
            for (int i = 0; i < handSheets.Count; i++)
            {
                bool on = i < show && !gun && g.Mode == GameMode.Walking;
                handSheets[i].SetActive(on);
                if (!on) continue;
                float frac = total <= 40 ? i : i * (total / 40f);
                handSheets[i].transform.localPosition = new Vector3(Mathf.Sin(i * 1.7f) * 0.004f, frac * 0.0012f + 0.002f, Mathf.Cos(i * 2.3f) * 0.004f);
                handSheets[i].transform.localEulerAngles = new Vector3(0, Mathf.Sin(i * 3.1f) * 4f, 0);
                bool typed = i >= show - Mathf.Min(show, g.S.carriedPages.Count);
                handSheets[i].GetComponent<MeshRenderer>().sharedMaterial = typed ? Build.Mat(Build.Hex("#e9e2cf"), 0.2f) : Build.Mat(Build.Paper, 0.2f);
            }
            if (containerVis) containerVis.SetActive(g.Mode == GameMode.Walking);
            // La hoja de arriba enseña lo que lleva escrito
            bool showText = !gun && g.Mode == GameMode.Walking && g.S.carriedPages.Count > 0 && show > 0;
            if (showText)
            {
                if (handText == null)
                    handText = Build.Text("TextoHoja", hand, Vector3.zero, "", 0.0105f, new Color(0.1f, 0.1f, 0.12f), TextAnchor.UpperCenter, new Vector3(90, 0, 0));
                var top = handSheets[show - 1].transform;
                handText.transform.localPosition = top.localPosition + new Vector3(0, 0.0016f, 0.125f);
                handText.transform.localEulerAngles = new Vector3(90, top.localEulerAngles.y, 0);
                var last = g.S.carriedPages[g.S.carriedPages.Count - 1];
                if (last != shownPage) { shownPage = last; handText.text = LooseSheet.Wrap(last.text ?? "", 30); }
            }
            if (handText != null) handText.gameObject.SetActive(showText);
        }
        TextMesh handText;
        PageData shownPage;
    }
}
