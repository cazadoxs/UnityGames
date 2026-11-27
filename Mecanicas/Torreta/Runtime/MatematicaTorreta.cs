using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Cálculos puros (sin estado) que usa la torreta: predicción de movimiento, tiro parabólico
    /// y ángulos alrededor de un eje. Están separados para poder probarlos y reutilizarlos.
    /// </summary>
    public static class MatematicaTorreta
    {
        /// <summary>
        /// Calcula dónde estará un objetivo que se mueve a velocidad constante cuando le llegue una
        /// bala recta de velocidad <paramref name="velocidadBala"/>. Devuelve false si la bala no
        /// puede alcanzarlo (el objetivo huye más rápido que la bala); en ese caso devuelve su
        /// posición actual.
        /// </summary>
        public static bool PuntoInterceptacion(Vector3 origen, Vector3 posObjetivo, Vector3 velObjetivo,
            float velocidadBala, out Vector3 punto, out float tiempo)
        {
            Vector3 d = posObjetivo - origen;
            float a = Vector3.Dot(velObjetivo, velObjetivo) - velocidadBala * velocidadBala;
            float b = 2f * Vector3.Dot(velObjetivo, d);
            float c = Vector3.Dot(d, d);
            tiempo = -1f;

            if (Mathf.Abs(a) < 1e-5f)
            {
                // Objetivo y bala casi igual de rápidos: la ecuación es lineal.
                if (Mathf.Abs(b) > 1e-5f) tiempo = -c / b;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc >= 0f)
                {
                    float raiz = Mathf.Sqrt(disc);
                    float t1 = (-b - raiz) / (2f * a);
                    float t2 = (-b + raiz) / (2f * a);
                    float tMin = Mathf.Min(t1, t2), tMax = Mathf.Max(t1, t2);
                    tiempo = tMin > 0f ? tMin : tMax;
                }
            }

            if (tiempo <= 0f)
            {
                punto = posObjetivo;
                tiempo = velocidadBala > 0f ? d.magnitude / velocidadBala : 0f;
                return false;
            }
            punto = posObjetivo + velObjetivo * tiempo;
            return true;
        }

        /// <summary>
        /// Ángulo de lanzamiento (grados sobre la horizontal) para que una bala de velocidad v
        /// con gravedad g (positiva) recorra una distancia horizontal x y suba una altura y.
        /// arcoAlto = true elige la parábola alta (de mortero). Devuelve false si no llega.
        /// </summary>
        public static bool AnguloBalistico(float x, float y, float v, float g, bool arcoAlto, out float anguloGrados)
        {
            anguloGrados = 0f;
            if (g <= 0f || v <= 0f) { anguloGrados = Mathf.Atan2(y, x) * Mathf.Rad2Deg; return true; }
            if (x < 1e-4f) { anguloGrados = y >= 0f ? 90f : -90f; return true; }

            float v2 = v * v;
            float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);
            if (disc < 0f) return false;
            float raiz = Mathf.Sqrt(disc);
            float tan = arcoAlto ? (v2 + raiz) / (g * x) : (v2 - raiz) / (g * x);
            anguloGrados = Mathf.Atan(tan) * Mathf.Rad2Deg;
            return true;
        }

        /// <summary>
        /// Dirección en la que hay que disparar desde <paramref name="origen"/> para que una bala con
        /// gravedad caiga en <paramref name="destino"/>. Si no llega, apunta a 45° (máximo alcance).
        /// </summary>
        public static bool DireccionBalistica(Vector3 origen, Vector3 destino, float v, float g, bool arcoAlto,
            out Vector3 direccion, out float tiempoVuelo)
        {
            Vector3 delta = destino - origen;
            Vector3 horizontal = new Vector3(delta.x, 0f, delta.z);
            float x = horizontal.magnitude;
            Vector3 dirH = x > 1e-4f ? horizontal / x : Vector3.forward;

            bool llega = AnguloBalistico(x, delta.y, v, g, arcoAlto, out float ang);
            if (!llega) ang = 45f;
            float rad = ang * Mathf.Deg2Rad;
            direccion = (dirH * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad)).normalized;
            float vh = v * Mathf.Cos(rad);
            tiempoVuelo = vh > 1e-4f ? x / vh : 0f;
            return llega;
        }

        /// <summary>
        /// Ángulo con signo (grados) que hay que girar <paramref name="referencia"/> alrededor de
        /// <paramref name="eje"/> para que apunte hacia <paramref name="direccion"/> (ambos proyectados
        /// en el plano perpendicular al eje).
        /// </summary>
        public static float AnguloEnEje(Vector3 referencia, Vector3 direccion, Vector3 eje)
        {
            Vector3 a = Vector3.ProjectOnPlane(referencia, eje);
            Vector3 b = Vector3.ProjectOnPlane(direccion, eje);
            if (a.sqrMagnitude < 1e-8f || b.sqrMagnitude < 1e-8f) return 0f;
            return Vector3.SignedAngle(a, b, eje);
        }

        /// <summary>Lleva un ángulo cualquiera al rango [-180, 180].</summary>
        public static float NormalizarAngulo(float a)
        {
            a = Mathf.Repeat(a + 180f, 360f) - 180f;
            return a;
        }

        /// <summary>Desvía una dirección al azar dentro de un cono de <paramref name="grados"/>.</summary>
        public static Vector3 AplicarDispersion(Vector3 direccion, float grados)
        {
            if (grados <= 0f) return direccion.normalized;
            Quaternion giro = Quaternion.LookRotation(direccion);
            Vector2 c = Random.insideUnitCircle * Mathf.Tan(grados * Mathf.Deg2Rad);
            return (giro * new Vector3(c.x, c.y, 1f)).normalized;
        }
    }
}
