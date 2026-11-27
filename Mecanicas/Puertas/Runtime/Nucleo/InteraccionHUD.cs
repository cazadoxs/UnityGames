using UnityEngine;

namespace Mecanicas.Puertas
{
    /// <summary>
    /// HUD mínimo sin dependencias (OnGUI): punto de mira, texto de la acción y mensajes.
    /// Si tu proyecto tiene su propia interfaz, no uses este componente y escucha
    /// <see cref="Interactor.AlCambiarObjetivo"/>, <see cref="Interactor.TextoObjetivo"/> y <see cref="Interactor.AlMostrarMensaje"/>.
    /// </summary>
    [AddComponentMenu("Mecánicas/Puertas/HUD de interacción")]
    public class InteraccionHUD : MonoBehaviour
    {
        [SerializeField] Interactor interactor;
        [SerializeField] bool mostrarPuntoMira = true;
        [SerializeField] Color colorPunto = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] Color colorPuntoObjetivo = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] int tamanoTexto = 20;
        [SerializeField] Color colorTexto = Color.white;
        [Tooltip("Formato del texto de acción. {0} = tecla, {1} = acción.")]
        [SerializeField] string formato = "[{0}] {1}";

        GUIStyle estilo;
        GUIStyle estiloSombra;

        void Awake()
        {
            if (interactor == null) interactor = GetComponent<Interactor>();
            if (interactor == null) interactor = FindFirstObjectByType<Interactor>();
        }

        void OnGUI()
        {
            if (interactor == null) return;
            if (estilo == null)
            {
                estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = tamanoTexto, wordWrap = true };
                estiloSombra = new GUIStyle(estilo);
            }
            estilo.normal.textColor = colorTexto;
            estiloSombra.normal.textColor = new Color(0f, 0f, 0f, 0.8f);

            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            bool hayObjetivo = interactor.Objetivo != null || interactor.Arrastrando;

            if (mostrarPuntoMira)
            {
                float t = hayObjetivo ? 8f : 4f;
                var previo = GUI.color;
                GUI.color = hayObjetivo ? colorPuntoObjetivo : colorPunto;
                GUI.DrawTexture(new Rect(cx - t * 0.5f, cy - t * 0.5f, t, t), Texture2D.whiteTexture);
                GUI.color = previo;
            }

            if (hayObjetivo && !string.IsNullOrEmpty(interactor.TextoObjetivo))
            {
                string txt = interactor.Arrastrando ? interactor.TextoObjetivo : string.Format(formato, Entrada.NombreTeclaInteractuar, interactor.TextoObjetivo);
                Etiqueta(new Rect(cx - 300f, cy + 20f, 600f, 40f), txt);
            }

            if (!string.IsNullOrEmpty(interactor.Mensaje) && Time.time < interactor.MensajeHasta)
                Etiqueta(new Rect(cx - 350f, Screen.height * 0.72f, 700f, 60f), interactor.Mensaje);
        }

        void Etiqueta(Rect r, string texto)
        {
            var s = r; s.x += 1.5f; s.y += 1.5f;
            GUI.Label(s, texto, estiloSombra);
            GUI.Label(r, texto, estilo);
        }
    }
}
