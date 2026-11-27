using UnityEngine;

namespace Mecanicas.Patrulla.Demo
{
    /// <summary>Da un color distinto a cada coche.</summary>
    public class ColorAlAzar : MonoBehaviour
    {
        void Start()
        {
            var r = transform.Find("Carrocería");
            if (r != null) DemoPatrulla.Pintar(r.gameObject, Color.HSVToRGB(Random.value, 0.75f, 0.9f));
        }
    }
}
