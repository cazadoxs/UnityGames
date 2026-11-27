using UnityEngine;

namespace Mecanicas.Dialogos.Demo
{
    /// <summary>
    /// Monta la escena de demostración por código al darle a Play (suelo, jugador, NPCs y diálogos),
    /// para que la demo funcione sin prefabs ni materiales externos, en Built-in o URP.
    /// Lee este archivo como ejemplo de cómo usar el sistema desde código.
    /// </summary>
    public class DialogosDemoBootstrap : MonoBehaviour
    {
        // ---------------- Diálogos de la demo, escritos en el formato de texto ----------------

        const string MercaderScript = @"
# Conversación larga con respuestas, variables y un evento.
! inicio saludo
[saludo]
Mercader: ¡Bienvenido de nuevo, {jugador}! [si:conoce_mercader]
-> menu
Mercader: ¡Hola, forastero! Soy Bruno, el mercader del pueblo.
! activar conoce_mercader
Mercader: Vendo de todo un poco... aunque hoy ando <i>muy</i> corto de género.
[menu]
Mercader: ¿En qué puedo ayudarte?
> ¿Qué vendes? -> vende [no:regalo_dado]
> ¿Hay algún trabajo? -> trabajo [no:trabajo_aceptado]
> ¿Qué tal el encargo? -> encargo [si:trabajo_aceptado]
> Nada, adiós. -> despedida
[vende]
Mercader: Solo me queda esta <b>caja misteriosa</b>. Te la regalo, por ser tú.
! evento regalo
Mercader: Mira, la he dejado en la mesa.
! activar regalo_dado
-> menu
[trabajo]
Mercader: Pues sí. La anciana de la plaza necesita a alguien que le haga compañía.
Mercader: ¿Te animas?
> Claro, iré a verla. -> acepta [activa:trabajo_aceptado]
> Ahora no puedo. -> menu
[acepta]
Mercader: ¡Estupendo! Está junto al pozo. Habla con ella cuando puedas.
-> fin
[encargo]
Mercader: ¿Aún no has hablado con la anciana? Está junto al pozo. [no:hablo_anciana]
-> menu
Mercader: La anciana dice que eres <color=#7fd4ff>un encanto</color>. ¡Gracias!
-> menu
[despedida]
Mercader: ¡Vuelve cuando quieras!
";

        const string AncianaScript = @"
# Conversación sin respuestas, con ramas según una variable y líneas que avanzan solas.
Anciana: ¡Ah! ¿Te manda Bruno? Qué detalle. [si:trabajo_aceptado]
-> historia
Anciana: Ay, hola, joven. Estos huesos ya no son lo que eran...
! espera 2.5
Anciana: Si ves al mercader, dile que me debe una visita.
-> fin
[historia]
Anciana: Siéntate, que te cuento por qué el pozo se quedó seco.
! activar hablo_anciana
- La anciana te cuenta una larguísima historia sobre el pueblo.
! espera 3
Anciana: ...y así fue. ¡Gracias por escucharme, {jugador}!
";

        const string CartelPrimeraVez = @"
- <b>DEMO DEL SISTEMA DE DIÁLOGOS</b>
- Mira a un personaje y pulsa <b>E</b> para hablar. Avanza con <b>Espacio</b>, <b>E</b> o <b>clic</b>.
- Las respuestas se eligen con el <b>ratón</b> o con las teclas <b>1-9</b>.
- El <b>guardia</b> dice frases sueltas al pasar a su lado, y la <b>radio</b> emite una conversación de fondo que no te bloquea.
";

        const string CartelRepeticion = @"
- Ya has leído el cartel. (Esta es la conversación de 'repetición' del DialogueStarter.)
";

        const string RadioScript = @"
# Conversación de fondo: blocksPlayer = false, avanza sola como subtítulos.
Locutor: Buenas noches, esto es Radio Pueblo.
Locutora: Hoy hablamos del misterioso pozo seco de la plaza.
Locutor: Dicen que la anciana sabe por qué...
Locutora: Sigan atentos. Y ahora, ¡música!
";

        Material baseMaterial;

        void Awake()
        {
            DialogueManager.TextTokens["jugador"] = "viajero";

            // Suelo y luz
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Suelo";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            baseMaterial = ground.GetComponent<Renderer>().sharedMaterial;
            Paint(ground, new Color(0.35f, 0.42f, 0.3f));

            if (FindAnyObjectByType<Light>() == null)
            {
                var light = new GameObject("Sol").AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Jugador
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(0f, 1f, -6f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            var camGo = new GameObject("Camara") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<DialogueInteractor>();
            var fps = player.AddComponent<DemoFirstPersonController>();
            fps.cameraPivot = camGo.transform;
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (cam.gameObject != camGo) cam.gameObject.SetActive(false);

            // Manager (opcional: se crearía solo, pero así le decimos qué desactivar)
            var manager = new GameObject("DialogueManager").AddComponent<DialogueManager>();
            manager.disableWhileTalking = new Behaviour[] { fps };

            // Personajes con color propio
            var bruno = Actor("Mercader", new Color(1f, 0.75f, 0.3f));
            var anciana = Actor("Anciana", new Color(0.85f, 0.6f, 1f));

            // NPC 1: mercader (conversación larga con respuestas)
            var mercader = Npc("Mercader", new Vector3(-4f, 0f, 2f), new Color(0.8f, 0.5f, 0.2f));
            var dMercader = DialogueAsset.FromScript(MercaderScript, "Mercader");
            dMercader.actors.Add(bruno);
            mercader.GetComponent<DialogueStarter>().dialogue = dMercader;
            var mesa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesa.name = "Mesa";
            mesa.transform.position = new Vector3(-2.5f, 0.4f, 2f);
            mesa.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
            Paint(mesa, new Color(0.45f, 0.3f, 0.2f));
            // Evento "regalo" escrito en el diálogo → aparece una caja en la mesa
            var listener = mercader.AddComponent<DialogueEventListener>();
            var entry = new DialogueEventListener.Entry { key = "regalo", response = new UnityEngine.Events.UnityEvent() };
            entry.response.AddListener(() => SpawnGift(mesa.transform.position + Vector3.up * 1.2f));
            listener.events.Add(entry);

            // NPC 2: anciana (sin respuestas, ramas por variable)
            var vieja = Npc("Anciana", new Vector3(4f, 0f, 2f), new Color(0.6f, 0.4f, 0.7f));
            var dAnciana = DialogueAsset.FromScript(AncianaScript, "Anciana");
            dAnciana.actors.Add(anciana);
            vieja.GetComponent<DialogueStarter>().dialogue = dAnciana;
            var pozo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pozo.name = "Pozo";
            pozo.transform.position = new Vector3(5.5f, 0.4f, 3.5f);
            pozo.transform.localScale = new Vector3(1.4f, 0.4f, 1.4f);
            Paint(pozo, new Color(0.5f, 0.5f, 0.55f));

            // Cartel (narrador + diálogo de repetición)
            var cartel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cartel.name = "Cartel";
            cartel.transform.position = new Vector3(0f, 1.2f, -3f);
            cartel.transform.localScale = new Vector3(1.6f, 1f, 0.1f);
            Paint(cartel, new Color(0.6f, 0.45f, 0.25f));
            var sStarter = cartel.AddComponent<DialogueStarter>();
            sStarter.dialogue = DialogueAsset.FromScript(CartelPrimeraVez, "Cartel");
            sStarter.repeatDialogue = DialogueAsset.FromScript(CartelRepeticion, "CartelRepeticion");
            var sInter = cartel.AddComponent<DialogueInteractable>();
            sInter.promptText = "[{tecla}] Leer el cartel";
            Label(cartel.transform, "Cartel", 0.9f);

            // NPC 3: guardia (frases sueltas al pasar cerca)
            var guardia = Npc("Guardia", new Vector3(0f, 0f, 7f), new Color(0.3f, 0.35f, 0.6f), talkable: false);
            var zona = new GameObject("ZonaGuardia");
            zona.transform.position = new Vector3(0f, 1f, 7f);
            var box = zona.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(5f, 2f, 3f);
            var line = zona.AddComponent<DialogueLine>();
            line.speakerName = "Guardia";
            line.lines = new[] { "¡Alto! ...ah, eres tú. Sigue.", "Nada que ver por aquí.", "Dicen que el pozo está maldito." };
            line.mode = DialogueLine.Mode.UnaCadaVezEnOrden;
            line.sayOnTriggerEnter = true;
            line.cooldown = 2f;
            Floor(zona.transform.position, new Vector3(5f, 3f), new Color(0.3f, 0.35f, 0.6f));

            // Radio (conversación de fondo que no bloquea)
            var radio = GameObject.CreatePrimitive(PrimitiveType.Cube);
            radio.name = "Radio";
            radio.transform.position = new Vector3(-6f, 0.25f, -4f);
            radio.transform.localScale = new Vector3(0.6f, 0.5f, 0.3f);
            Paint(radio, new Color(0.15f, 0.15f, 0.15f));
            Label(radio.transform, "Radio", 0.6f);
            var zonaRadio = new GameObject("ZonaRadio");
            zonaRadio.transform.position = new Vector3(-6f, 1f, -4f);
            var box2 = zonaRadio.AddComponent<BoxCollider>();
            box2.isTrigger = true;
            box2.size = new Vector3(3f, 2f, 3f);
            var dRadio = DialogueAsset.FromScript(RadioScript, "Radio");
            dRadio.blocksPlayer = false;
            var rStarter = zonaRadio.AddComponent<DialogueStarter>();
            rStarter.dialogue = dRadio;
            rStarter.playOnTriggerEnter = true;
            Floor(zonaRadio.transform.position, new Vector3(3f, 3f), new Color(0.2f, 0.2f, 0.2f));

            DialogueManager.ShowLine("", "WASD para moverte, ratón para mirar. Lee el cartel que tienes delante (E).", 6f);
        }

        // ---------------- Utilidades de la demo ----------------

        static DialogueActor Actor(string name, Color color)
        {
            var a = ScriptableObject.CreateInstance<DialogueActor>();
            a.name = a.displayName = name;
            a.nameColor = color;
            return a;
        }

        GameObject Npc(string name, Vector3 pos, Color color, bool talkable = true)
        {
            var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = name;
            npc.transform.position = pos + Vector3.up;
            npc.transform.LookAt(new Vector3(0f, npc.transform.position.y, -6f));
            Paint(npc, color);
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Nariz";
            Destroy(head.GetComponent<Collider>());
            head.transform.SetParent(npc.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.6f, 0.45f);
            head.transform.localScale = new Vector3(0.15f, 0.15f, 0.2f);
            Paint(head, color * 0.7f);
            Label(npc.transform, name, 1.35f);
            if (talkable)
            {
                npc.AddComponent<DialogueStarter>();
                npc.AddComponent<DialogueInteractable>().displayName = name;
            }
            return npc;
        }

        void SpawnGift(Vector3 pos)
        {
            var gift = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gift.name = "CajaMisteriosa";
            gift.transform.position = pos;
            gift.transform.localScale = Vector3.one * 0.4f;
            Paint(gift, new Color(1f, 0.85f, 0.2f));
            gift.AddComponent<Rigidbody>();
        }

        void Floor(Vector3 center, Vector3 size, Color color)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Cube);
            q.name = "MarcaZona";
            Destroy(q.GetComponent<Collider>());
            q.transform.position = new Vector3(center.x, 0.01f, center.z);
            q.transform.localScale = new Vector3(size.x, 0.02f, size.y);
            Paint(q, color);
        }

        static void Label(Transform target, string text, float height)
        {
            var go = new GameObject("Etiqueta_" + text);
            go.transform.position = target.position + Vector3.up * height;
            go.transform.localScale = Vector3.one * 0.05f;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 60;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.AddComponent<DemoFaceCamera>();
        }

        void Paint(GameObject go, Color c)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var m = new Material(baseMaterial != null ? baseMaterial : r.sharedMaterial);
            m.color = c;
            r.sharedMaterial = m;
        }
    }
}
