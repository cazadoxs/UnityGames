using UnityEngine;

namespace Mecanicas.Patrulla.Demo
{
    /// <summary>Copia la línea horneada de la ruta a un LineRenderer para verla en el Game view.</summary>
    [RequireComponent(typeof(RutaDePuntos), typeof(LineRenderer))]
    public class DibujarRutaEnJuego : MonoBehaviour
    {
        RutaDePuntos ruta;
        LineRenderer linea;

        void OnEnable()
        {
            ruta = GetComponent<RutaDePuntos>();
            linea = GetComponent<LineRenderer>();
            ruta.AlReconstruir += Actualizar;
            Actualizar();
        }

        void OnDisable() { if (ruta != null) ruta.AlReconstruir -= Actualizar; }

        void Actualizar()
        {
            var m = ruta.Muestras;
            linea.positionCount = m.Count;
            for (int i = 0; i < m.Count; i++) linea.SetPosition(i, m[i] + Vector3.up * 0.06f);
        }
    }
}
