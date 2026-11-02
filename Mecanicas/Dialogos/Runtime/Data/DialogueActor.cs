using UnityEngine;

namespace Mecanicas.Dialogos
{
    /// <summary>
    /// Ficha de un personaje que habla: nombre, color del nombre y retrato opcional.
    /// Crear con: clic derecho en Project > Create > Mecánicas > Diálogos > Personaje.
    /// </summary>
    [CreateAssetMenu(menuName = "Mecánicas/Diálogos/Personaje", fileName = "NuevoPersonaje", order = 1)]
    public class DialogueActor : ScriptableObject
    {
        [Tooltip("Nombre que aparece en la caja de diálogo. También se usa para enlazar con las líneas escritas como 'Nombre: texto'.")]
        public string displayName = "Personaje";

        [Tooltip("Color con el que se pinta el nombre.")]
        public Color nameColor = new Color(1f, 0.85f, 0.4f);

        [Tooltip("Retrato opcional que se muestra a la izquierda de la caja.")]
        public Sprite portrait;

        [Tooltip("Sonido opcional que suena con cada letra del efecto máquina de escribir (estilo 'blip').")]
        public AudioClip typingBlip;

        [Tooltip("Variación aleatoria de tono del blip (0 = sin variación).")]
        [Range(0f, 0.5f)] public float blipPitchVariation = 0.1f;
    }
}
