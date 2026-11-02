using System.Collections;
using UnityEngine;

namespace Mecanicas.Torretas.Demo
{
    /// <summary>
    /// Diana de la demo: se mueve por unos puntos (ida y vuelta o en círculo), parpadea al recibir
    /// daño y reaparece unos segundos después de morir.
    /// </summary>
    [RequireComponent(typeof(Salud))]
    [AddComponentMenu("Mecanicas/Torretas/Demo/Objetivo Movil")]
    public class ObjetivoMovil : MonoBehaviour
    {
        public enum Recorrido { IdaYVuelta, Bucle, Circulo }

        public Recorrido recorrido = Recorrido.IdaYVuelta;
        [Tooltip("Puntos en coordenadas del mundo (IdaYVuelta y Bucle).")]
        public Vector3[] puntos = new Vector3[0];
        [Tooltip("Centro y radio para el recorrido Circulo.")]
        public Vector3 centroCirculo;
        public float radioCirculo = 8f;
        [Min(0f)] public float velocidad = 4f;
        [Tooltip("Sube y baja mientras se mueve (para objetivos voladores).")]
        public float amplitudVertical = 0f;
        [Min(0f)] public float tiempoReaparicion = 3f;

        Salud salud;
        Renderer[] renderers;
        Collider[] colliders;
        int indice;
        int sentido = 1;
        float angulo;
        float alturaBase;
        MaterialPropertyBlock bloque;

        void Awake()
        {
            salud = GetComponent<Salud>();
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();
            salud.alMorir.AddListener(AlMorir);
            salud.alRecibirDanio.AddListener(_ => StartCoroutine(Parpadeo()));
            bloque = new MaterialPropertyBlock();
            if (recorrido != Recorrido.Circulo && puntos.Length > 0) transform.position = puntos[0];
            alturaBase = recorrido == Recorrido.Circulo ? centroCirculo.y : transform.position.y;
        }

        void Update()
        {
            if (!salud.EstaVivo) return;
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;

            if (recorrido == Recorrido.Circulo)
            {
                angulo += velocidad / Mathf.Max(0.1f, radioCirculo) * dt;
                pos = centroCirculo + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radioCirculo;
            }
            else if (puntos.Length > 1)
            {
                Vector3 destino = puntos[indice];
                Vector3 actual = new Vector3(pos.x, alturaBase, pos.z);
                actual = Vector3.MoveTowards(actual, destino, velocidad * dt);
                alturaBase = actual.y;
                pos = actual;
                if ((actual - destino).sqrMagnitude < 0.01f) Avanzar();
            }

            pos.y += VaivenActual(Time.time);
            Vector3 mov = pos - transform.position;
            mov.y = 0f;
            if (mov.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(mov);
            transform.position = pos;
        }

        float VaivenActual(float t) => amplitudVertical != 0f ? Mathf.Sin(t * 1.3f) * amplitudVertical : 0f;

        void Avanzar()
        {
            if (recorrido == Recorrido.Bucle) { indice = (indice + 1) % puntos.Length; return; }
            if (indice + sentido >= puntos.Length || indice + sentido < 0) sentido = -sentido;
            indice += sentido;
        }

        void AlMorir() => StartCoroutine(Reaparecer());

        IEnumerator Reaparecer()
        {
            Mostrar(false);
            yield return new WaitForSeconds(tiempoReaparicion);
            salud.Revivir();
            Mostrar(true);
        }

        void Mostrar(bool visible)
        {
            foreach (var r in renderers) if (r != null) r.enabled = visible;
            foreach (var c in colliders) if (c != null) c.enabled = visible;
        }

        IEnumerator Parpadeo()
        {
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(bloque);
                bloque.SetColor("_BaseColor", Color.white);
                bloque.SetColor("_Color", Color.white);
                r.SetPropertyBlock(bloque);
            }
            yield return new WaitForSeconds(0.06f);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(null);
        }
    }
}
