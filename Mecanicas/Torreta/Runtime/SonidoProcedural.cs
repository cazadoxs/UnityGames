using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Sonidos generados por código para que la torreta suene sin necesidad de importar audio.
    /// Son sustitutos: en el juego final asigna tus propios AudioClip en TorretaEfectos.
    /// </summary>
    public static class SonidoProcedural
    {
        const int Frecuencia = 44100;
        static AudioClip disparo, impacto, motor, pitido, recarga;

        /// <summary>Estampido corto: ruido que se apaga rápido más un golpe grave.</summary>
        public static AudioClip Disparo()
        {
            if (disparo != null) return disparo;
            var r = new System.Random(1);
            disparo = Crear("Disparo (generado)", 0.28f, (t, i) =>
            {
                float ruido = (float)(r.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 28f);
                float golpe = Mathf.Sin(2f * Mathf.PI * (110f - 60f * t) * t) * Mathf.Exp(-t * 14f);
                return Mathf.Clamp((ruido * 0.7f + golpe * 0.6f), -1f, 1f) * 0.9f;
            });
            return disparo;
        }

        /// <summary>Chasquido metálico de impacto.</summary>
        public static AudioClip Impacto()
        {
            if (impacto != null) return impacto;
            var r = new System.Random(2);
            impacto = Crear("Impacto (generado)", 0.15f, (t, i) =>
            {
                float ruido = (float)(r.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 45f);
                float metal = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-t * 35f) * 0.3f;
                return (ruido * 0.6f + metal) * 0.8f;
            });
            return impacto;
        }

        /// <summary>Zumbido de motor en bucle (dura un número entero de ciclos para que no "salte").</summary>
        public static AudioClip Motor()
        {
            if (motor != null) return motor;
            motor = Crear("Motor (generado)", 1f, (t, i) =>
            {
                float s = Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.25f
                        + Mathf.Sin(2f * Mathf.PI * 270f * t) * 0.12f;
                return s * 0.5f;
            });
            return motor;
        }

        /// <summary>Pitido de "objetivo fijado".</summary>
        public static AudioClip Pitido()
        {
            if (pitido != null) return pitido;
            pitido = Crear("Pitido (generado)", 0.12f, (t, i) =>
            {
                float env = Mathf.Min(1f, t * 200f) * Mathf.Min(1f, (0.12f - t) * 200f);
                return Mathf.Sin(2f * Mathf.PI * 1300f * t) * env * 0.4f;
            });
            return pitido;
        }

        /// <summary>Dos clics mecánicos de recarga.</summary>
        public static AudioClip Recarga()
        {
            if (recarga != null) return recarga;
            var r = new System.Random(3);
            recarga = Crear("Recarga (generado)", 0.45f, (t, i) =>
            {
                float a = t < 0.05f ? Mathf.Exp(-t * 90f) : 0f;
                float t2 = t - 0.3f;
                float b = t2 > 0f && t2 < 0.06f ? Mathf.Exp(-t2 * 80f) : 0f;
                return (float)(r.NextDouble() * 2.0 - 1.0) * (a + b) * 0.7f;
            });
            return recarga;
        }

        static AudioClip Crear(string nombre, float segundos, System.Func<float, int, float> muestra)
        {
            int n = Mathf.CeilToInt(segundos * Frecuencia);
            var datos = new float[n];
            for (int i = 0; i < n; i++) datos[i] = muestra(i / (float)Frecuencia, i);
            var clip = AudioClip.Create(nombre, n, 1, Frecuencia, false);
            clip.SetData(datos, 0);
            return clip;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() { disparo = impacto = motor = pitido = recarga = null; }
    }
}
