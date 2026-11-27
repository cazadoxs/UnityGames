using System.Collections;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>Qué hace la acción con el objeto.</summary>
    public enum ModoObjeto
    {
        [Tooltip("Activa un objeto que ya está en la escena (desactivado).")] ActivarExistente,
        [Tooltip("Crea una copia de un prefab.")] InstanciarPrefab,
        [Tooltip("Desactiva un objeto de la escena.")] Desactivar,
        [Tooltip("Destruye un objeto de la escena.")] Destruir,
        [Tooltip("Activa el objeto si está desactivado y al revés.")] Alternar
    }

    /// <summary>Hace aparecer (o desaparecer) un objeto, con un pequeño efecto de escala opcional.</summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Aparecer objeto")]
    public class AccionObjeto : AccionEvento
    {
        [SerializeField] private ModoObjeto modo = ModoObjeto.ActivarExistente;

        [Tooltip("Objeto de la escena (Activar, Desactivar, Destruir, Alternar).")]
        [SerializeField] private GameObject objetivo;

        [Tooltip("Prefab a crear (Instanciar Prefab).")]
        [SerializeField] private GameObject prefab;

        [Tooltip("Dónde aparece el prefab. Vacío = en esta acción.")]
        [SerializeField] private Transform puntoAparicion;

        [Tooltip("El prefab creado queda como hijo del punto de aparición.")]
        [SerializeField] private bool comoHijo = false;

        [Tooltip("Prefab opcional de efecto (partículas, humo...) que se crea en el mismo sitio.")]
        [SerializeField] private GameObject efecto;

        [Tooltip("Segundos tras los que se borra el efecto. 0 = nunca.")]
        [Min(0f)] [SerializeField] private float vidaEfecto = 3f;

        [Header("Animación de escala")]
        [Tooltip("Al aparecer crece desde 0, al desaparecer encoge hasta 0.")]
        [SerializeField] private bool animarEscala = true;

        [Min(0f)] [SerializeField] private float duracionEscala = 0.35f;

        [SerializeField] private AnimationCurve curvaEscala = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3f), new Keyframe(0.7f, 1.1f), new Keyframe(1f, 1f));

        [Header("Después")]
        [Tooltip("Segundos tras los que el objeto que ha aparecido se quita (se desactiva o, si es un prefab, se destruye). 0 = se queda.")]
        [Min(0f)] [SerializeField] private float quitarTras = 0f;

        /// <summary>Último objeto que apareció con esta acción (útil para otros scripts).</summary>
        public GameObject UltimoObjeto { get; private set; }

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            switch (modo)
            {
                case ModoObjeto.ActivarExistente:
                    if (!Comprobar(objetivo, "objetivo")) yield break;
                    yield return Aparecer(objetivo, false);
                    break;

                case ModoObjeto.InstanciarPrefab:
                    if (!Comprobar(prefab, "prefab")) yield break;
                    Transform punto = puntoAparicion != null ? puntoAparicion : transform;
                    GameObject copia = comoHijo
                        ? Instantiate(prefab, punto.position, punto.rotation, punto)
                        : Instantiate(prefab, punto.position, punto.rotation);
                    yield return Aparecer(copia, true);
                    break;

                case ModoObjeto.Desactivar:
                    if (!Comprobar(objetivo, "objetivo")) yield break;
                    yield return Desaparecer(objetivo, false);
                    break;

                case ModoObjeto.Destruir:
                    if (!Comprobar(objetivo, "objetivo")) yield break;
                    yield return Desaparecer(objetivo, true);
                    break;

                case ModoObjeto.Alternar:
                    if (!Comprobar(objetivo, "objetivo")) yield break;
                    if (objetivo.activeSelf) yield return Desaparecer(objetivo, false);
                    else yield return Aparecer(objetivo, false);
                    break;
            }
        }

        private IEnumerator Aparecer(GameObject obj, bool esCopia)
        {
            UltimoObjeto = obj;
            CrearEfecto(obj.transform.position);

            Vector3 escala = obj.transform.localScale;
            obj.SetActive(true);
            if (animarEscala && duracionEscala > 0f)
            {
                for (float t = 0f; t < duracionEscala && obj != null; t += Time.deltaTime)
                {
                    obj.transform.localScale = escala * curvaEscala.Evaluate(t / duracionEscala);
                    yield return null;
                }
                if (obj != null) obj.transform.localScale = escala;
            }

            if (quitarTras > 0f && obj != null)
                LanzarCorrutina(QuitarLuego(obj, esCopia, escala));
        }

        private IEnumerator QuitarLuego(GameObject obj, bool destruir, Vector3 escalaOriginal)
        {
            yield return new WaitForSeconds(quitarTras);
            if (obj == null) yield break;
            yield return Desaparecer(obj, destruir);
            if (!destruir && obj != null) obj.transform.localScale = escalaOriginal;
        }

        private IEnumerator Desaparecer(GameObject obj, bool destruir)
        {
            if (!obj.activeInHierarchy)
            {
                if (destruir) Destroy(obj);
                else obj.SetActive(false);
                yield break;
            }

            CrearEfecto(obj.transform.position);
            Vector3 escala = obj.transform.localScale;
            if (animarEscala && duracionEscala > 0f)
            {
                for (float t = 0f; t < duracionEscala && obj != null; t += Time.deltaTime)
                {
                    obj.transform.localScale = escala * (1f - t / duracionEscala);
                    yield return null;
                }
            }
            if (obj == null) yield break;

            if (destruir) Destroy(obj);
            else
            {
                obj.SetActive(false);
                obj.transform.localScale = escala; // lista para volver a aparecer
            }
        }

        private void CrearEfecto(Vector3 posicion)
        {
            if (efecto == null) return;
            GameObject e = Instantiate(efecto, posicion, Quaternion.identity);
            if (vidaEfecto > 0f) Destroy(e, vidaEfecto);
        }

        private bool Comprobar(Object o, string campo)
        {
            if (o != null) return true;
            Debug.LogWarning($"[Eventos] '{name}': AccionObjeto sin '{campo}'.", this);
            return false;
        }
    }
}
