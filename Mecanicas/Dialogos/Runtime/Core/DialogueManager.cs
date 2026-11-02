using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Cerebro del sistema: recorre las conversaciones, escribe el texto, muestra las respuestas,
    /// aplica variables y lanza eventos. Solo hace falta uno por escena; si no existe, se crea solo
    /// la primera vez que alguien lo usa (con la vista uGUI por defecto).
    ///
    /// API rápida para otros sistemas:
    ///   DialogueManager.Play(dialogo);                       // conversación
    ///   DialogueManager.Play(dialogo, "etiqueta", alTerminar);
    ///   DialogueManager.ShowLine("Guardia", "¡Alto!", 3f);   // frase suelta, no bloquea
    ///   DialogueManager.Stop();
    ///   DialogueManager.IsRunning
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Mecánicas/Diálogos/Dialogue Manager")]
    public class DialogueManager : MonoBehaviour
    {
        [Serializable] public class StringEvent : UnityEvent<string> { }

        // ------------------------------------------------------------------ Configuración

        [Header("Vista")]
        [Tooltip("Vista que dibuja los diálogos. Vacío = se busca en este objeto o se crea la vista uGUI por defecto.")]
        public DialogueViewBase view;

        [Header("Texto")]
        [Tooltip("Letras por segundo del efecto máquina de escribir. 0 = el texto aparece de golpe.")]
        [Min(0f)] public float charactersPerSecond = 45f;
        [Tooltip("Pausa extra (segundos) al escribir . ! ? y saltos de línea.")]
        [Min(0f)] public float punctuationPause = 0.12f;
        [Tooltip("Cada cuántas letras suena el 'blip' del personaje (si tiene).")]
        [Min(1)] public int blipEveryNCharacters = 2;

        [Header("Controles")]
        [Tooltip("Teclas para avanzar (o para terminar de escribir la línea de golpe).")]
        public KeyCode[] advanceKeys = { KeyCode.Space, KeyCode.Return, KeyCode.E, KeyCode.Mouse0 };
        [Tooltip("Permitir elegir respuestas con las teclas 1-9.")]
        public bool numberKeysForChoices = true;
        [Tooltip("Permitir que pulsar avanzar mientras se escribe muestre la línea entera.")]
        public bool allowSkipTyping = true;
        [Tooltip("Segundos tras empezar una conversación o cambiar de línea en los que se ignora el avance (evita saltarse líneas sin querer).")]
        [Min(0f)] public float inputCooldown = 0.2f;

        [Header("Jugador (solo conversaciones que bloquean)")]
        [Tooltip("Liberar y mostrar el cursor mientras hay respuestas para elegir, y restaurarlo al acabar.")]
        public bool manageCursor = true;
        [Tooltip("Componentes que se desactivan durante la conversación: el controlador en primera persona, el script de disparo, etc.")]
        public Behaviour[] disableWhileTalking;

        [Header("Frases sueltas")]
        [Tooltip("Duración por defecto de ShowLine si se pasa 0: base + segundos por letra.")]
        public float subtitleBaseSeconds = 1.5f;
        public float subtitleSecondsPerChar = 0.05f;

        [Header("Audio")]
        [Tooltip("Fuente para las voces. Vacío = se crea una 2D.")]
        public AudioSource voiceSource;

        [Header("Otros")]
        [Tooltip("Mantener el manager al cambiar de escena.")]
        public bool dontDestroyOnLoad;

        [Header("Eventos (Inspector)")]
        public UnityEvent onConversationStarted;
        public UnityEvent onConversationEnded;
        [Tooltip("Se lanza con el nombre del evento de una línea o respuesta.")]
        public StringEvent onDialogueEvent;

        // ------------------------------------------------------------------ Eventos estáticos (código)

        /// <summary>Empieza una conversación.</summary>
        public static event Action<DialogueAsset> DialogueStarted;
        /// <summary>Termina una conversación (también si se interrumpe).</summary>
        public static event Action<DialogueAsset> DialogueEnded;
        /// <summary>Se muestra una línea.</summary>
        public static event Action<DialogueAsset, DialogueNode> LineShown;
        /// <summary>El jugador elige una respuesta.</summary>
        public static event Action<DialogueAsset, DialogueChoice> ChoiceMade;
        /// <summary>Evento con nombre de una línea o respuesta (campo "Event Key").</summary>
        public static event Action<string> NodeEvent;

        /// <summary>Sustituciones de texto: TextTokens["jugador"] = "Álvaro" convierte {jugador} en Álvaro.</summary>
        public static readonly Dictionary<string, string> TextTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------------ Singleton

        static DialogueManager instance;
        static bool quitting;

        /// <summary>El manager de la escena (se crea si no existe).</summary>
        public static DialogueManager Instance
        {
            get
            {
                if (instance != null || quitting) return instance;
                instance = FindAnyObjectByType<DialogueManager>();
                if (instance == null) instance = new GameObject("DialogueManager").AddComponent<DialogueManager>();
                return instance;
            }
        }

        /// <summary>¿Existe ya un manager? (no lo crea)</summary>
        public static bool HasInstance => instance != null;

        /// <summary>¿Hay una conversación en curso?</summary>
        public static bool IsRunning => instance != null && instance.running != null;
        /// <summary>Conversación en curso (o null).</summary>
        public static DialogueAsset Current => instance != null ? instance.currentAsset : null;

        /// <summary>Empieza una conversación. Devuelve false si ya había otra (salvo interrupt = true).</summary>
        public static bool Play(DialogueAsset dialogue, Action onFinished = null) =>
            Instance != null && Instance.StartDialogue(dialogue, null, onFinished);

        /// <summary>Empieza una conversación en una etiqueta concreta.</summary>
        public static bool Play(DialogueAsset dialogue, string startNodeId, Action onFinished = null, bool interrupt = false) =>
            Instance != null && Instance.StartDialogue(dialogue, startNodeId, onFinished, interrupt);

        /// <summary>Frase suelta tipo subtítulo que no bloquea al jugador. duration 0 = automática según la longitud.</summary>
        public static void ShowLine(string speaker, string text, float duration = 0f, AudioClip voice = null) =>
            Instance?.EnqueueLine(speaker, text, duration, voice, null);

        /// <summary>Frase suelta usando el color del personaje.</summary>
        public static void ShowLine(DialogueActor actor, string text, float duration = 0f, AudioClip voice = null) =>
            Instance?.EnqueueLine(actor != null ? actor.displayName : null, text, duration, voice, actor);

        /// <summary>Corta la conversación en curso.</summary>
        public static void Stop() { if (instance != null) instance.StopDialogue(); }

        // ------------------------------------------------------------------ Estado

        DialogueAsset currentAsset;
        Coroutine running;
        Action currentOnFinished;
        int chosenIndex = -1;
        bool cursorSaved;
        CursorLockMode savedLock;
        bool savedVisible;
        bool[] savedEnabled;
        AudioSource blipSource;

        struct PendingLine { public string speaker; public string text; public float duration; public AudioClip voice; public DialogueActor actor; }
        readonly Queue<PendingLine> lineQueue = new Queue<PendingLine>();
        Coroutine lineRoutine;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[Diálogos] Hay más de un DialogueManager en la escena; se elimina el duplicado.", this);
                Destroy(this);
                return;
            }
            instance = this;
            if (dontDestroyOnLoad) { transform.SetParent(null); DontDestroyOnLoad(gameObject); }
            EnsureView();
            EnsureAudio();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        // Para que todo funcione aunque esté desactivado "Domain Reload" en Enter Play Mode Options.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            quitting = false;
            DialogueStarted = null;
            DialogueEnded = null;
            LineShown = null;
            ChoiceMade = null;
            NodeEvent = null;
            DialogueVariables.ResetAll();
        }
        void OnApplicationQuit() { quitting = true; }

        void EnsureView()
        {
            if (view != null) return;
            view = GetComponentInChildren<DialogueViewBase>();
            if (view != null) return;
            var go = new GameObject("VistaDialogos");
            go.transform.SetParent(transform, false);
            view = go.AddComponent<DialogueViewUGUI>();
        }

        void EnsureAudio()
        {
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
                voiceSource.spatialBlend = 0f;
            }
            blipSource = gameObject.AddComponent<AudioSource>();
            blipSource.playOnAwake = false;
            blipSource.spatialBlend = 0f;
        }

        /// <summary>Muestra/oculta el aviso de interacción en la vista (lo usa DialogueInteractor).</summary>
        public void SetPrompt(string text)
        {
            if (view == null) return;
            if (string.IsNullOrEmpty(text)) view.HidePrompt(); else view.ShowPrompt(text);
        }

        // ------------------------------------------------------------------ Conversaciones

        public bool StartDialogue(DialogueAsset dialogue, string startNodeId = null, Action onFinished = null, bool interrupt = false)
        {
            if (dialogue == null || dialogue.nodes == null || dialogue.nodes.Count == 0)
            {
                Debug.LogWarning("[Diálogos] Se intentó empezar un diálogo vacío o nulo.", this);
                return false;
            }
            if (running != null)
            {
                if (!interrupt) return false;
                StopDialogue();
            }

            int start = string.IsNullOrEmpty(startNodeId) ? dialogue.StartIndex : dialogue.IndexOf(startNodeId);
            if (start < 0)
            {
                Debug.LogWarning($"[Diálogos] '{dialogue.name}' no tiene la etiqueta '{startNodeId}'. Empiezo por el principio.", dialogue);
                start = dialogue.StartIndex;
            }

            currentAsset = dialogue;
            currentOnFinished = onFinished;
            running = StartCoroutine(Run(dialogue, start));
            return true;
        }

        public void StopDialogue()
        {
            if (running == null) return;
            StopCoroutine(running);
            Finish();
        }

        IEnumerator Run(DialogueAsset asset, int index)
        {
            bool blocking = asset.blocksPlayer;
            BeginConversation(blocking);
            yield return null; // no consumir la tecla que abrió la conversación

            int guard = 0;
            while (index >= 0 && index < asset.nodes.Count)
            {
                if (++guard > 10000) { Debug.LogError($"[Diálogos] '{asset.name}' parece un bucle infinito sin líneas visibles.", asset); break; }

                var node = asset.nodes[index];
                if (node == null || !DialogueVariables.Check(node.requiredFlag, node.forbiddenFlag)) { index++; continue; }

                ApplyEffects(node.setFlag, node.clearFlag, node.eventKey);
                var actor = node.actor != null ? node.actor : asset.FindActor(node.speakerName);
                string speaker = actor != null && string.IsNullOrEmpty(node.speakerName) ? actor.displayName : ReplaceTokens(node.speakerName);
                Color color = actor != null ? actor.nameColor : new Color(1f, 0.85f, 0.4f);
                string text = ReplaceTokens(node.text ?? "");

                LineShown?.Invoke(asset, node);
                if (node.voice != null) { voiceSource.Stop(); voiceSource.clip = node.voice; voiceSource.Play(); }

                // Respuestas visibles
                var visible = new List<int>();
                if (node.choices != null)
                    for (int c = 0; c < node.choices.Count; c++)
                        if (node.choices[c] != null && DialogueVariables.Check(node.choices[c].requiredFlag, node.choices[c].forbiddenFlag))
                            visible.Add(c);

                if (!blocking)
                {
                    // Conversación de fondo: subtítulos que avanzan solos.
                    view.ShowSubtitle(speaker, color, text);
                    float wait = node.autoAdvanceDelay > 0f ? node.autoAdvanceDelay : AutoDuration(text);
                    yield return new WaitForSecondsRealtime(wait);
                    if (visible.Count > 0)
                    {
                        Debug.LogWarning($"[Diálogos] '{asset.name}' no bloquea al jugador pero tiene respuestas; se elige la primera automáticamente.", asset);
                        var ch = node.choices[visible[0]];
                        ApplyChoice(asset, ch);
                        index = Resolve(asset, index, ch.next);
                    }
                    else index = Resolve(asset, index, node.next);
                    continue;
                }

                // Escribir el texto
                view.ShowLine(new DialogueLineInfo { speaker = speaker, speakerColor = color, portrait = actor != null ? actor.portrait : null, fullText = text });
                view.SetContinueHint(false);
                yield return TypeText(text, actor);

                if (visible.Count > 0)
                {
                    var labels = new List<string>(visible.Count);
                    foreach (int c in visible) labels.Add(ReplaceTokens(node.choices[c].text));
                    chosenIndex = -1;
                    SetCursorFree(true);
                    view.ShowChoices(labels, i => chosenIndex = i);
                    float cooldown = inputCooldown;
                    while (chosenIndex < 0)
                    {
                        cooldown -= Time.unscaledDeltaTime;
                        if (numberKeysForChoices && cooldown <= 0f)
                        {
                            int n = DialogueInput.NumberKeyDown();
                            if (n >= 0 && n < labels.Count) chosenIndex = n;
                        }
                        yield return null;
                    }
                    view.HideChoices();
                    SetCursorFree(false);
                    var choice = node.choices[visible[chosenIndex]];
                    ApplyChoice(asset, choice);
                    index = Resolve(asset, index, choice.next);
                }
                else
                {
                    view.SetContinueHint(node.autoAdvanceDelay <= 0f);
                    float t = 0f;
                    while (true)
                    {
                        t += Time.unscaledDeltaTime;
                        if (node.autoAdvanceDelay > 0f && t >= node.autoAdvanceDelay) break;
                        if (t >= inputCooldown && DialogueInput.AnyDown(advanceKeys)) break;
                        yield return null;
                    }
                    view.SetContinueHint(false);
                    index = Resolve(asset, index, node.next);
                }
                yield return null; // la tecla que avanzó no cuenta para la línea siguiente
            }

            Finish();
        }

        IEnumerator TypeText(string text, DialogueActor actor)
        {
            int total = RichTextReveal.CountVisible(text);
            if (charactersPerSecond <= 0f || total == 0) { view.SetText(text); yield break; }

            string plain = RichTextReveal.Strip(text);
            float shown = 0f, pause = 0f, elapsed = 0f;
            int lastShown = -1;
            while (shown < total)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                if (allowSkipTyping && elapsed >= inputCooldown && DialogueInput.AnyDown(advanceKeys)) break;

                if (pause > 0f) pause -= dt;
                else shown += dt * charactersPerSecond;

                int n = Mathf.Min(total, Mathf.FloorToInt(shown));
                if (n != lastShown)
                {
                    view.SetText(RichTextReveal.Reveal(text, n));
                    if (n > 0 && n <= plain.Length)
                    {
                        char ch = plain[n - 1];
                        if (ch == '.' || ch == '!' || ch == '?' || ch == '\n' || ch == '…') pause = punctuationPause;
                        if (actor != null && actor.typingBlip != null && !char.IsWhiteSpace(ch) && n % blipEveryNCharacters == 0)
                        {
                            blipSource.pitch = 1f + UnityEngine.Random.Range(-actor.blipPitchVariation, actor.blipPitchVariation);
                            blipSource.PlayOneShot(actor.typingBlip);
                        }
                    }
                    lastShown = n;
                }
                yield return null;
            }
            view.SetText(text);
            yield return null; // si se pulsó para saltar, esa pulsación no avanza también
        }

        int Resolve(DialogueAsset asset, int from, string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return from + 1;
            if (DialogueAsset.IsEnd(target.Trim())) return -1;
            int idx = asset.IndexOf(target.Trim());
            if (idx < 0) Debug.LogWarning($"[Diálogos] '{asset.name}': la etiqueta '{target}' no existe. Termino la conversación.", asset);
            return idx;
        }

        void ApplyChoice(DialogueAsset asset, DialogueChoice choice)
        {
            ChoiceMade?.Invoke(asset, choice);
            ApplyEffects(choice.setFlag, choice.clearFlag, choice.eventKey);
        }

        void ApplyEffects(string set, string clear, string evt)
        {
            if (!string.IsNullOrWhiteSpace(set)) DialogueVariables.Set(set);
            if (!string.IsNullOrWhiteSpace(clear)) DialogueVariables.Clear(clear);
            if (!string.IsNullOrWhiteSpace(evt)) RaiseEvent(evt.Trim());
        }

        /// <summary>Lanza un evento con nombre como si lo hubiera lanzado una línea.</summary>
        public void RaiseEvent(string key)
        {
            NodeEvent?.Invoke(key);
            onDialogueEvent?.Invoke(key);
        }

        void BeginConversation(bool blocking)
        {
            lineQueue.Clear();
            if (lineRoutine != null) { StopCoroutine(lineRoutine); lineRoutine = null; }
            view.HideSubtitle();
            view.HidePrompt();
            if (blocking)
            {
                view.Open();
                if (disableWhileTalking != null)
                {
                    savedEnabled = new bool[disableWhileTalking.Length];
                    for (int i = 0; i < disableWhileTalking.Length; i++)
                    {
                        var b = disableWhileTalking[i];
                        if (b == null) continue;
                        savedEnabled[i] = b.enabled;
                        b.enabled = false;
                    }
                }
            }
            onConversationStarted?.Invoke();
            DialogueStarted?.Invoke(currentAsset);
        }

        void Finish()
        {
            var asset = currentAsset;
            var cb = currentOnFinished;
            running = null;
            currentAsset = null;
            currentOnFinished = null;

            view.Close();
            view.HideSubtitle();
            if (voiceSource) voiceSource.Stop();
            SetCursorFree(false);
            if (disableWhileTalking != null && savedEnabled != null)
            {
                for (int i = 0; i < disableWhileTalking.Length && i < savedEnabled.Length; i++)
                    if (disableWhileTalking[i] != null) disableWhileTalking[i].enabled = savedEnabled[i];
            }
            savedEnabled = null;

            onConversationEnded?.Invoke();
            DialogueEnded?.Invoke(asset);
            cb?.Invoke();
            if (lineQueue.Count > 0 && lineRoutine == null && running == null) lineRoutine = StartCoroutine(RunLines());
        }

        void SetCursorFree(bool free)
        {
            if (!manageCursor) return;
            if (free)
            {
                if (!cursorSaved) { savedLock = Cursor.lockState; savedVisible = Cursor.visible; cursorSaved = true; }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (cursorSaved)
            {
                Cursor.lockState = savedLock;
                Cursor.visible = savedVisible;
                cursorSaved = false;
            }
        }

        // ------------------------------------------------------------------ Frases sueltas

        void EnqueueLine(string speaker, string text, float duration, AudioClip voice, DialogueActor actor)
        {
            if (string.IsNullOrEmpty(text)) return;
            lineQueue.Enqueue(new PendingLine { speaker = speaker, text = text, duration = duration, voice = voice, actor = actor });
            if (lineRoutine == null && running == null) lineRoutine = StartCoroutine(RunLines());
        }

        IEnumerator RunLines()
        {
            while (lineQueue.Count > 0 && running == null)
            {
                var l = lineQueue.Dequeue();
                string text = ReplaceTokens(l.text);
                Color c = l.actor != null ? l.actor.nameColor : new Color(1f, 0.85f, 0.4f);
                view.ShowSubtitle(ReplaceTokens(l.speaker), c, text);
                if (l.voice != null) voiceSource.PlayOneShot(l.voice);
                float d = l.duration > 0f ? l.duration : (l.voice != null ? Mathf.Max(l.voice.length, AutoDuration(text)) : AutoDuration(text));
                yield return new WaitForSecondsRealtime(d);
                view.HideSubtitle();
                yield return new WaitForSecondsRealtime(0.15f);
            }
            lineRoutine = null;
        }

        /// <summary>Borra las frases sueltas pendientes y oculta la actual.</summary>
        public void ClearLines()
        {
            lineQueue.Clear();
            if (lineRoutine != null) { StopCoroutine(lineRoutine); lineRoutine = null; }
            if (view) view.HideSubtitle();
        }

        float AutoDuration(string text) => subtitleBaseSeconds + RichTextReveal.CountVisible(text) * subtitleSecondsPerChar;

        static string ReplaceTokens(string s)
        {
            if (string.IsNullOrEmpty(s) || TextTokens.Count == 0 || s.IndexOf('{') < 0) return s;
            foreach (var kv in TextTokens) s = s.Replace("{" + kv.Key + "}", kv.Value);
            return s;
        }
    }
}
