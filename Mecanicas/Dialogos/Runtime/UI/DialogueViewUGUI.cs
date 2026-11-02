using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Vista por defecto hecha con uGUI (Canvas). Si no le asignas referencias, se construye sola
    /// al arrancar con un aspecto neutro. Puedes asignar tus propios objetos (panel, textos, botón
    /// plantilla) para personalizarla sin tocar código, o crear tu propia vista heredando de
    /// <see cref="DialogueViewBase"/>.
    /// </summary>
    [AddComponentMenu("Mecánicas/Diálogos/Vista de diálogo (uGUI)")]
    public class DialogueViewUGUI : DialogueViewBase
    {
        [Header("Referencias (vacío = se crean solas)")]
        [Tooltip("Objeto raíz de la caja de conversación.")]
        public GameObject panel;
        public Text nameText;
        public Text bodyText;
        public Image portraitImage;
        [Tooltip("Indicador de 'continuar' (parpadea cuando la línea ha terminado de escribirse).")]
        public GameObject continueHint;
        [Tooltip("Contenedor de las respuestas (idealmente con un VerticalLayoutGroup).")]
        public RectTransform choicesContainer;
        [Tooltip("Botón que se clona para cada respuesta. Debe tener un Text hijo. Se desactiva al arrancar.")]
        public Button choiceButtonTemplate;
        public Text subtitleText;
        public Text promptText;

        [Header("Aspecto de la vista automática")]
        public Font font;
        public int bodyFontSize = 30;
        public int nameFontSize = 30;
        public int choiceFontSize = 28;
        public Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        public Color textColor = Color.white;
        public Color choiceColor = new Color(0.15f, 0.15f, 0.2f, 0.92f);
        public Color choiceHighlight = new Color(0.35f, 0.3f, 0.12f, 1f);
        [Tooltip("Mostrar '1.', '2.'... delante de cada respuesta (se pueden elegir con las teclas numéricas).")]
        public bool numberChoices = true;

        readonly List<Button> spawnedChoices = new List<Button>();
        Action<int> pendingChoice;
        RectTransform bodyRect;
        float hintTimer;
        bool hintVisible;

        void Awake()
        {
            if (panel == null) BuildDefault();
            if (choiceButtonTemplate != null) choiceButtonTemplate.gameObject.SetActive(false);
            if (bodyText != null) bodyRect = bodyText.rectTransform;
            EnsureEventSystem();
            Close();
            HideSubtitle();
            HidePrompt();
        }

        void Update()
        {
            if (continueHint == null) return;
            if (!hintVisible) { if (continueHint.activeSelf) continueHint.SetActive(false); return; }
            hintTimer += Time.unscaledDeltaTime;
            continueHint.SetActive(Mathf.Repeat(hintTimer, 1f) < 0.65f);
        }

        // ---------------- DialogueViewBase ----------------

        public override void Open() { if (panel) panel.SetActive(true); }

        public override void Close()
        {
            HideChoices();
            SetContinueHint(false);
            if (panel) panel.SetActive(false);
        }

        public override void ShowLine(DialogueLineInfo line)
        {
            if (nameText)
            {
                bool hasName = !string.IsNullOrEmpty(line.speaker);
                nameText.gameObject.SetActive(hasName);
                nameText.text = line.speaker;
                nameText.color = line.speakerColor;
            }
            if (portraitImage)
            {
                portraitImage.gameObject.SetActive(line.portrait != null);
                portraitImage.sprite = line.portrait;
                if (bodyRect != null && portraitImage.transform.IsChildOf(panel.transform))
                {
                    float left = line.portrait != null ? 230f : 30f;
                    bodyRect.offsetMin = new Vector2(left, bodyRect.offsetMin.y);
                    if (nameText) nameText.rectTransform.offsetMin = new Vector2(left, nameText.rectTransform.offsetMin.y);
                }
            }
            if (bodyText) bodyText.text = "";
        }

        public override void SetText(string richText) { if (bodyText) bodyText.text = richText; }

        public override void SetContinueHint(bool visible)
        {
            hintVisible = visible;
            hintTimer = 0f;
            if (continueHint) continueHint.SetActive(visible);
        }

        public override void ShowChoices(IReadOnlyList<string> options, Action<int> onChosen)
        {
            HideChoices();
            pendingChoice = onChosen;
            if (choicesContainer == null || choiceButtonTemplate == null) return;
            choicesContainer.gameObject.SetActive(true);
            for (int i = 0; i < options.Count; i++)
            {
                int index = i;
                var b = Instantiate(choiceButtonTemplate, choicesContainer);
                b.gameObject.SetActive(true);
                var label = b.GetComponentInChildren<Text>(true);
                if (label) label.text = numberChoices && i < 9 ? $"{i + 1}. {options[i]}" : options[i];
                b.onClick.AddListener(() => Choose(index));
                spawnedChoices.Add(b);
            }
            if (spawnedChoices.Count > 0 && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public override void HideChoices()
        {
            foreach (var b in spawnedChoices) if (b) Destroy(b.gameObject);
            spawnedChoices.Clear();
            pendingChoice = null;
            if (choicesContainer) choicesContainer.gameObject.SetActive(false);
        }

        public override void ShowSubtitle(string speaker, Color speakerColor, string text)
        {
            if (!subtitleText) return;
            subtitleText.gameObject.SetActive(true);
            subtitleText.text = string.IsNullOrEmpty(speaker)
                ? text
                : $"<color=#{ColorUtility.ToHtmlStringRGB(speakerColor)}><b>{speaker}:</b></color> {text}";
        }

        public override void HideSubtitle() { if (subtitleText) subtitleText.gameObject.SetActive(false); }

        public override void ShowPrompt(string text)
        {
            if (!promptText) return;
            promptText.gameObject.SetActive(true);
            promptText.text = text;
        }

        public override void HidePrompt() { if (promptText) promptText.gameObject.SetActive(false); }

        void Choose(int index)
        {
            var cb = pendingChoice;
            pendingChoice = null;
            cb?.Invoke(index);
        }

        // ---------------- Construcción automática ----------------

        void BuildDefault()
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("CanvasDialogos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            // Caja principal
            var panelRt = NewRect("Panel", root);
            panelRt.anchorMin = new Vector2(0.12f, 0f);
            panelRt.anchorMax = new Vector2(0.88f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(0f, 250f);
            panelRt.anchoredPosition = new Vector2(0f, 30f);
            var bg = panelRt.gameObject.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;
            panel = panelRt.gameObject;

            var portraitRt = NewRect("Retrato", panelRt);
            portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0f, 0.5f);
            portraitRt.pivot = new Vector2(0f, 0.5f);
            portraitRt.sizeDelta = new Vector2(190f, 190f);
            portraitRt.anchoredPosition = new Vector2(25f, 0f);
            portraitImage = portraitRt.gameObject.AddComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            nameText = NewText("Nombre", panelRt, nameFontSize, TextAnchor.UpperLeft, FontStyle.Bold);
            Stretch(nameText.rectTransform, new Vector2(30f, -60f), new Vector2(-30f, -14f), top: true);

            bodyText = NewText("Texto", panelRt, bodyFontSize, TextAnchor.UpperLeft, FontStyle.Normal);
            bodyText.rectTransform.anchorMin = Vector2.zero;
            bodyText.rectTransform.anchorMax = Vector2.one;
            bodyText.rectTransform.offsetMin = new Vector2(30f, 20f);
            bodyText.rectTransform.offsetMax = new Vector2(-30f, -62f);
            bodyRect = bodyText.rectTransform;

            var hint = NewText("Continuar", panelRt, 26, TextAnchor.LowerRight, FontStyle.Bold);
            hint.text = "▼";
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = hint.rectTransform.pivot = new Vector2(1f, 0f);
            hint.rectTransform.sizeDelta = new Vector2(60f, 40f);
            hint.rectTransform.anchoredPosition = new Vector2(-20f, 12f);
            continueHint = hint.gameObject;

            // Respuestas
            choicesContainer = NewRect("Respuestas", root);
            choicesContainer.anchorMin = choicesContainer.anchorMax = new Vector2(0.5f, 0f);
            choicesContainer.pivot = new Vector2(0.5f, 0f);
            choicesContainer.sizeDelta = new Vector2(900f, 0f);
            choicesContainer.anchoredPosition = new Vector2(0f, 300f);
            var layout = choicesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.LowerCenter;
            var fitter = choicesContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var btnRt = NewRect("PlantillaRespuesta", choicesContainer);
            var btnImg = btnRt.gameObject.AddComponent<Image>();
            btnImg.color = Color.white;
            var le = btnRt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 56f;
            var btn = btnRt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = choiceColor;
            colors.highlightedColor = choiceHighlight;
            colors.selectedColor = choiceHighlight;
            colors.pressedColor = choiceHighlight * 0.8f;
            btn.colors = colors;
            btn.targetGraphic = btnImg;
            var btnText = NewText("Texto", btnRt, choiceFontSize, TextAnchor.MiddleLeft, FontStyle.Normal);
            btnText.rectTransform.anchorMin = Vector2.zero;
            btnText.rectTransform.anchorMax = Vector2.one;
            btnText.rectTransform.offsetMin = new Vector2(24f, 6f);
            btnText.rectTransform.offsetMax = new Vector2(-24f, -6f);
            choiceButtonTemplate = btn;

            // Subtítulos de frases sueltas
            subtitleText = NewText("Subtitulo", root, 30, TextAnchor.LowerCenter, FontStyle.Normal);
            subtitleText.rectTransform.anchorMin = new Vector2(0.15f, 0f);
            subtitleText.rectTransform.anchorMax = new Vector2(0.85f, 0f);
            subtitleText.rectTransform.pivot = new Vector2(0.5f, 0f);
            subtitleText.rectTransform.sizeDelta = new Vector2(0f, 120f);
            subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            var outline = subtitleText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Aviso de interacción
            promptText = NewText("Aviso", root, 28, TextAnchor.MiddleCenter, FontStyle.Bold);
            promptText.rectTransform.anchorMin = promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.42f);
            promptText.rectTransform.sizeDelta = new Vector2(800f, 60f);
            var o2 = promptText.gameObject.AddComponent<Outline>();
            o2.effectColor = new Color(0f, 0f, 0f, 0.9f);
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        Text NewText(string name, Transform parent, int size, TextAnchor align, FontStyle style)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = textColor;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max, bool top)
        {
            rt.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rt.offsetMin = min;
            rt.offsetMax = max;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if MECANICAS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
