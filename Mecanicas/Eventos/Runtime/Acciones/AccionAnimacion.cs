using System.Collections;
using UnityEngine;

namespace Mecanicas.Eventos
{
    /// <summary>Qué le hace la acción al Animator.</summary>
    public enum TipoAnimacion
    {
        [Tooltip("Dispara un parámetro Trigger.")] Trigger,
        [Tooltip("Pone un parámetro Bool.")] Bool,
        [Tooltip("Pone un parámetro Int.")] Entero,
        [Tooltip("Pone un parámetro Float.")] Decimal,
        [Tooltip("Salta directamente a un estado (con transición suave).")] ReproducirEstado,
        [Tooltip("Reproduce un clip con el componente Animation antiguo (Legacy).")] ClipLegacy
    }

    /// <summary>Lanza una animación: parámetros del Animator, salto a un estado o un clip Legacy.</summary>
    [AddComponentMenu("Mecánicas/Eventos/Acciones/Animación")]
    public class AccionAnimacion : AccionEvento
    {
        [Tooltip("Animator a controlar. Si está vacío se busca en este objeto.")]
        [SerializeField] private Animator animator;

        [SerializeField] private TipoAnimacion tipo = TipoAnimacion.Trigger;

        [Tooltip("Nombre del parámetro (Trigger, Bool, Entero o Decimal).")]
        [SerializeField] private string parametro = "Abrir";

        [SerializeField] private bool valorBool = true;
        [SerializeField] private int valorEntero = 1;
        [SerializeField] private float valorDecimal = 1f;

        [Tooltip("Nombre del estado (Reproducir Estado). Puede incluir la capa: 'Base Layer.Abrir'.")]
        [SerializeField] private string estado = "";

        [Tooltip("Índice de la capa del Animator para Reproducir Estado (-1 = la del estado).")]
        [SerializeField] private int capa = -1;

        [Tooltip("Segundos de mezcla al saltar al estado.")]
        [Min(0f)] [SerializeField] private float transicion = 0.15f;

        [Header("Legacy")]
        [SerializeField] private Animation animacionLegacy;
        [SerializeField] private AnimationClip clipLegacy;

        [Header("Espera")]
        [Tooltip("Segundos que la acción se considera en marcha (para que la secuencia espere a la animación). " +
                 "-1 = la duración del estado o clip (solo Reproducir Estado y Clip Legacy).")]
        [SerializeField] private float duracion = 0f;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            animacionLegacy = GetComponentInChildren<Animation>();
        }

        protected override IEnumerator EjecutarAccion(ContextoEvento contexto)
        {
            if (tipo == TipoAnimacion.ClipLegacy)
            {
                yield return Legacy();
                yield break;
            }

            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"[Eventos] '{name}': AccionAnimacion sin Animator.", this);
                yield break;
            }

            switch (tipo)
            {
                case TipoAnimacion.Trigger:
                    if (TieneParametro(AnimatorControllerParameterType.Trigger)) animator.SetTrigger(parametro);
                    break;
                case TipoAnimacion.Bool:
                    if (TieneParametro(AnimatorControllerParameterType.Bool)) animator.SetBool(parametro, valorBool);
                    break;
                case TipoAnimacion.Entero:
                    if (TieneParametro(AnimatorControllerParameterType.Int)) animator.SetInteger(parametro, valorEntero);
                    break;
                case TipoAnimacion.Decimal:
                    if (TieneParametro(AnimatorControllerParameterType.Float)) animator.SetFloat(parametro, valorDecimal);
                    break;
                case TipoAnimacion.ReproducirEstado:
                    animator.CrossFadeInFixedTime(estado, transicion, capa);
                    break;
            }

            float espera = duracion;
            if (espera < 0f && tipo == TipoAnimacion.ReproducirEstado)
            {
                // El estado nuevo se conoce un frame después del CrossFade.
                yield return null;
                int l = Mathf.Max(0, capa);
                AnimatorStateInfo info = animator.IsInTransition(l)
                    ? animator.GetNextAnimatorStateInfo(l)
                    : animator.GetCurrentAnimatorStateInfo(l);
                espera = info.length / Mathf.Max(0.01f, Mathf.Abs(animator.speed * info.speedMultiplier));
            }

            if (espera > 0f) yield return new WaitForSeconds(espera);
        }

        private IEnumerator Legacy()
        {
            if (animacionLegacy == null) animacionLegacy = GetComponent<Animation>();
            if (animacionLegacy == null || clipLegacy == null)
            {
                Debug.LogWarning($"[Eventos] '{name}': falta el componente Animation o el clip Legacy.", this);
                yield break;
            }

            if (animacionLegacy.GetClip(clipLegacy.name) == null)
                animacionLegacy.AddClip(clipLegacy, clipLegacy.name);
            animacionLegacy.CrossFade(clipLegacy.name, transicion);

            float espera = duracion < 0f ? clipLegacy.length : duracion;
            if (espera > 0f) yield return new WaitForSeconds(espera);
        }

        private bool TieneParametro(AnimatorControllerParameterType t)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                if (p.name == parametro && p.type == t) return true;

            Debug.LogWarning($"[Eventos] '{name}': el Animator de '{animator.name}' no tiene un parámetro {t} llamado '{parametro}'.", this);
            return false;
        }
    }
}
