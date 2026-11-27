using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>
    /// Subtítulos de emergencia: muestran las frases en la parte baja de la pantalla cuando no hay
    /// ningún sistema de diálogos registrado. No necesita Canvas ni TextMeshPro.
    /// Se crea solo la primera vez que hace falta; si quieres cambiar su aspecto, ponlo tú en la escena.
    /// </summary>
    [AddComponentMenu("Mecánicas/Eventos/Subtítulos simples")]
    [DisallowMultipleComponent]
    public class SubtitulosSimples : MonoBehaviour
    {
        [Header("Aspecto")]
        [Min(8)] [SerializeField] private int tamanoLetra = 26;
        [SerializeField] private Color colorTexto = Color.white;
        [SerializeField] private Color colorHablante = new Color(1f, 0.82f, 0.35f);
        [SerializeField] private Color colorFondo = new Color(0f, 0f, 0f, 0.6f);
        [Tooltip("Ancho máximo de la caja, en fracción de la pantalla.")]
        [Range(0.2f, 1f)] [SerializeField] private float anchoMaximo = 0.7f;
        [Tooltip("Separación del borde inferior, en fracción de la pantalla.")]
        [Range(0f, 0.5f)] [SerializeField] private float margenInferior = 0.08f;

        [Header("Ritmo")]
        [Tooltip("Segundos por carácter cuando la frase tiene duración automática.")]
        [Min(0f)] [SerializeField] private float segundosPorCaracter = 0.06f;
        [Min(0.5f)] [SerializeField] private float duracionMinima = 1.8f;
        [Tooltip("Permite pasar de frase con clic izquierdo, Espacio o Intro.")]
        [SerializeField] private bool permitirSaltar = true;

        private static SubtitulosSimples instancia;

        private class Peticion
        {
            public IReadOnlyList<LineaDialogo> lineas;
            public Action alTerminar;
        }

        private readonly Queue<Peticion> cola = new Queue<Peticion>();
        private LineaDialogo actual;
        private bool saltar;
        private bool reproduciendo;
        private AudioSource fuenteVoz;
        private GUIStyle estiloTexto;

        /// <summary>True mientras se muestra alguna frase.</summary>
        public bool Reproduciendo => reproduciendo;

        /// <summary>Devuelve los subtítulos de la escena, creándolos si no existen.</summary>
        public static SubtitulosSimples Obtener()
        {
            if (instancia != null) return instancia;
            instancia = FindFirstObjectByType<SubtitulosSimples>();
            if (instancia == null)
                instancia = new GameObject("Subtitulos (Eventos)").AddComponent<SubtitulosSimples>();
            return instancia;
        }

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(this);
                return;
            }
            instancia = this;
            fuenteVoz = gameObject.AddComponent<AudioSource>();
            fuenteVoz.playOnAwake = false;
            fuenteVoz.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            if (instancia == this) instancia = null;
        }

        /// <summary>Pone en cola unas frases. alTerminar se llama al acabar la última.</summary>
        public void Mostrar(IReadOnlyList<LineaDialogo> lineas, Action alTerminar = null)
        {
            if (lineas == null || lineas.Count == 0)
            {
                alTerminar?.Invoke();
                return;
            }

            cola.Enqueue(new Peticion { lineas = lineas, alTerminar = alTerminar });
            if (!reproduciendo) StartCoroutine(Reproducir());
        }

        /// <summary>Atajo para una sola frase.</summary>
        public void Mostrar(string hablante, string texto, float duracion = 0f)
        {
            Mostrar(new[] { new LineaDialogo { hablante = hablante, texto = texto, duracion = duracion } });
        }

        /// <summary>Pasa a la siguiente frase.</summary>
        public void Saltar() => saltar = true;

        private IEnumerator Reproducir()
        {
            reproduciendo = true;
            while (cola.Count > 0)
            {
                Peticion p = cola.Dequeue();
                foreach (LineaDialogo linea in p.lineas)
                {
                    if (linea == null) continue;
                    actual = linea;
                    saltar = false;

                    float dur = linea.duracion;
                    if (linea.voz != null)
                    {
                        fuenteVoz.clip = linea.voz;
                        fuenteVoz.Play();
                        if (dur <= 0f) dur = linea.voz.length + 0.3f;
                    }
                    if (dur <= 0f)
                        dur = Mathf.Max(duracionMinima, (linea.texto ?? "").Length * segundosPorCaracter);

                    // Un frame de margen para que el clic que lanzó la frase no la salte.
                    yield return null;
                    for (float t = 0f; t < dur && !saltar; t += Time.deltaTime)
                        yield return null;

                    if (fuenteVoz.isPlaying) fuenteVoz.Stop();
                }
                actual = null;
                p.alTerminar?.Invoke();
            }
            reproduciendo = false;
        }

        private void OnGUI()
        {
            if (actual == null) return;

            if (permitirSaltar && Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Space || Event.current.keyCode == KeyCode.Return))
                saltar = true;
            if (permitirSaltar && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                saltar = true;

            if (estiloTexto == null)
            {
                estiloTexto = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    richText = true
                };
            }
            estiloTexto.fontSize = tamanoLetra;
            estiloTexto.normal.textColor = colorTexto;

            string hablante = string.IsNullOrEmpty(actual.hablante)
                ? ""
                : $"<color=#{ColorUtility.ToHtmlStringRGB(colorHablante)}><b>{actual.hablante}:</b></color> ";
            GUIContent contenido = new GUIContent(hablante + actual.texto);

            float ancho = Screen.width * anchoMaximo;
            float alto = estiloTexto.CalcHeight(contenido, ancho - 40f) + 24f;
            Rect caja = new Rect((Screen.width - ancho) * 0.5f, Screen.height * (1f - margenInferior) - alto, ancho, alto);

            Color previo = GUI.color;
            GUI.color = colorFondo;
            GUI.DrawTexture(caja, Texture2D.whiteTexture);
            GUI.color = previo;
            GUI.Label(new Rect(caja.x + 20f, caja.y + 12f, caja.width - 40f, caja.height - 24f), contenido, estiloTexto);
        }
    }
}
