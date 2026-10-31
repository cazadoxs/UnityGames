using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>Resalta un objeto cuando el jugador lo mira (tiñe sus materiales o activa un objeto).</summary>
    public class Highlightable : MonoBehaviour
    {
        public Color highlightColor = new Color(1f, 0.92f, 0.5f);
        [Range(0f, 1f)] public float strength = 0.35f;
        [Tooltip("Opcional: objeto que se activa al resaltar (contorno, flecha...). Si se asigna, no se tiñe.")]
        public GameObject highlightObject;

        readonly List<Renderer> renderers = new List<Renderer>();
        readonly List<Color> baseColors = new List<Color>();
        MaterialPropertyBlock block;
        bool highlighted;

        void Awake()
        {
            if (highlightObject != null) { highlightObject.SetActive(false); return; }
            block = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                // Los objetos que se colocan encima no se tiñen.
                if (r.GetComponentInParent<Item>() != null) continue;
                var m = r.sharedMaterial;
                if (m == null) continue;
                renderers.Add(r);
                baseColors.Add(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor")
                    : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
            }
        }

        public void SetHighlighted(bool on)
        {
            if (on == highlighted) return;
            highlighted = on;
            if (highlightObject != null) { highlightObject.SetActive(on); return; }
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;
                if (on)
                {
                    var c = Color.Lerp(baseColors[i], highlightColor, strength);
                    renderers[i].GetPropertyBlock(block);
                    block.SetColor("_BaseColor", c);
                    block.SetColor("_Color", c);
                    renderers[i].SetPropertyBlock(block);
                }
                else
                {
                    renderers[i].SetPropertyBlock(null);
                }
            }
        }
    }
}
