using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Rayos de sol polvorientos que entran por los lucernarios (como en los graneros de los vídeos):
    /// láminas translúcidas que siguen la dirección del sol y se apagan al atardecer.
    /// </summary>
    public class Atmosphere : MonoBehaviour
    {
        readonly List<Transform> pivots = new List<Transform>();
        Material beam;
        Light sun;
        float sunMax = 1f;

        public static Atmosphere Create(Transform root)
        {
            var go = Build.Empty("Atmosfera", root, Vector3.zero);
            var a = go.AddComponent<Atmosphere>();
            a.beam = new Material(Build.Glass);
            a.beam.SetFloat("_Smoothness", 0f);
            a.beam.SetColor("_BaseColor", new Color(1f, 0.9f, 0.72f, 0.05f));
            a.beam.SetColor("_EmissionColor", new Color(0.35f, 0.3f, 0.22f));
            a.beam.EnableKeyword("_EMISSION");
            var rnd = new System.Random(5);
            for (float z = WorldBuilder.ZStart; z < WorldBuilder.ZEnd; z += 20f)
            {
                for (int k = 0; k < 2; k++)
                {
                    float x = -30f + k * 60f + (float)rnd.NextDouble() * 14f - 7f;
                    var p = Build.Empty("Rayo", go.transform, new Vector3(x, WorldBuilder.Height, z + 18f)).transform;
                    float w = 5f + (float)rnd.NextDouble() * 6f;
                    // láminas anidadas de menor a mayor: el borde queda difuminado en vez de cortado
                    for (int q = 0; q < 4; q++)
                    {
                        float f = 1f - q * 0.22f;
                        var b = Build.Box("RayoLamina", p, new Vector3(0, -24f, 0), new Vector3(w * f, 48f, 3.4f * f), a.beam, false);
                        b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        b.GetComponent<Renderer>().receiveShadows = false;
                    }
                    a.pivots.Add(p);
                }
            }
            return a;
        }

        void LateUpdate()
        {
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { sun = l; sunMax = Mathf.Max(0.01f, l.intensity); break; }
                if (sun == null) return;
            }
            sunMax = Mathf.Max(sunMax, sun.intensity);
            var dir = sun.transform.forward;
            if (dir.y > -0.15f) dir.y = -0.15f;
            var rot = Quaternion.FromToRotation(Vector3.down, dir.normalized);
            for (int i = 0; i < pivots.Count; i++) pivots[i].rotation = rot;
            float k = Mathf.Clamp01(sun.intensity / sunMax) * (sun.enabled && sun.gameObject.activeInHierarchy ? 1f : 0f);
            beam.SetColor("_BaseColor", new Color(1f, 0.9f, 0.72f, 0.016f * k));
            beam.SetColor("_EmissionColor", new Color(0.1f, 0.085f, 0.06f) * k);
        }
    }
}
