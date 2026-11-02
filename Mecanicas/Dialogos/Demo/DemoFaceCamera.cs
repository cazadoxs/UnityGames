using UnityEngine;

namespace Mecanicas.Dialogos.Demo
{
    /// <summary>Hace que las etiquetas de nombre de la demo miren siempre a la cámara.</summary>
    public class DemoFaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
