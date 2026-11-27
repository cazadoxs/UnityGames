using UnityEngine;

namespace Mecanicas.Inventario.Demo
{
    /// <summary>Hambre de ejemplo para la demo: sube con el tiempo y baja al comer.</summary>
    [AddComponentMenu("Mecánicas/Inventario/Demo/Hambre (demo)")]
    public class DemoHunger : MonoBehaviour
    {
        [SerializeField] private float hunger = 60f;
        [SerializeField] private float perSecond = 1f;

        public float Hunger => hunger;

        public void Eat(float amount) => hunger = Mathf.Max(0f, hunger - amount);

        private void Update() => hunger = Mathf.Min(100f, hunger + perSecond * Time.deltaTime);

        private void OnGUI()
        {
            GUI.Label(new Rect(20, 20, 600, 30), "Hambre: " + hunger.ToString("0") + " / 100  (come manzanas)");
        }
    }
}
