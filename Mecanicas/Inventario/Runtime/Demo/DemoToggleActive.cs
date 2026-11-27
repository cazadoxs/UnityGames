using UnityEngine;

namespace Mecanicas.Inventario.Demo
{
    /// <summary>Activa/desactiva un objeto (la luz de la linterna en la demo). Conéctalo a un UnityEvent.</summary>
    [AddComponentMenu("Mecánicas/Inventario/Demo/Alternar activo (demo)")]
    public class DemoToggleActive : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        public void Toggle() { if (target != null) target.SetActive(!target.activeSelf); }
    }
}
