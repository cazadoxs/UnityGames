using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mecanicas.Construccion
{
    [Serializable] public class LegendTextEvent : UnityEvent<string> { }

    /// <summary>
    /// Una línea de la leyenda. Úsalo para hacer tu propia plantilla de línea (prefab): asigna una
    /// Image para el color, otra para el icono y el texto. Si usas TextMeshPro, deja "Texto" vacío y
    /// conecta <see cref="onText"/> a TextMeshProUGUI.text en el inspector.
    /// </summary>
    [AddComponentMenu("Mecánicas/Construcción/Línea de leyenda")]
    public class BuildLegendEntryView : MonoBehaviour
    {
        [Tooltip("Texto de UI clásico (opcional).")]
        public Text text;
        [Tooltip("Para TextMeshPro u otro texto: recibe la cadena ya formateada.")]
        public LegendTextEvent onText = new LegendTextEvent();
        [Tooltip("Cuadradito de color (opcional).")]
        public Image swatch;
        [Tooltip("Icono (opcional).")]
        public Image icon;

        private string _last;

        public void SetText(string value)
        {
            if (value == _last) return;
            _last = value;
            if (text != null) text.text = value;
            onText.Invoke(value);
        }

        public void SetSwatch(bool visible, Color color)
        {
            if (swatch == null) return;
            if (swatch.gameObject.activeSelf != visible) swatch.gameObject.SetActive(visible);
            if (visible) swatch.color = color;
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon == null) return;
            bool visible = sprite != null;
            if (icon.gameObject.activeSelf != visible) icon.gameObject.SetActive(visible);
            if (visible) icon.sprite = sprite;
        }
    }
}
