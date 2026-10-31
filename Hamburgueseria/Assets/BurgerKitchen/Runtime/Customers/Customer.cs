using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace BurgerKitchen
{
    /// <summary>
    /// Cliente: entra, hace cola, pide en la caja, espera su comida y se va (contento o enfadado).
    /// Se mueve con NavMeshAgent si lo tiene y hay NavMesh; si no, en línea recta entre puntos.
    /// Si tiene Animator, recibe el parámetro float "Speed".
    /// </summary>
    public class Customer : MonoBehaviour, IWorldStatusSource
    {
        public enum State { Arriving, InQueue, ReadyToOrder, WaitingForFood, Leaving }

        public static readonly List<Customer> All = new List<Customer>();

        [Header("Movimiento (sin NavMesh)")]
        public float moveSpeed = 1.6f;
        public float turnSpeed = 540f;
        public float arriveDistance = 0.12f;
        [Tooltip("Altura de la etiqueta de paciencia.")]
        public float statusHeight = 2.1f;

        public State CurrentState { get; private set; }
        public Order Order { get; private set; }
        public float Patience { get; private set; }
        public float MaxPatience { get; private set; }
        public float PatienceFraction => MaxPatience > 0f ? Mathf.Clamp01(Patience / MaxPatience) : 1f;
        public bool LeftHappy { get; private set; }

        CustomerFlow flow;
        NavMeshAgent agent;
        Animator animator;
        Vector3 target;
        bool hasTarget;
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        public bool HasArrived
        {
            get
            {
                if (!hasTarget) return true;
                if (UsingAgent) return !agent.pathPending && agent.remainingDistance <= Mathf.Max(arriveDistance, agent.stoppingDistance);
                var d = target - transform.position; d.y = 0f;
                return d.magnitude <= arriveDistance;
            }
        }

        bool UsingAgent => agent != null && agent.enabled && agent.isOnNavMesh;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
        }

        void OnEnable() { All.Add(this); WorldStatusRegistry.Register(this); }
        void OnDisable() { All.Remove(this); WorldStatusRegistry.Unregister(this); }

        public void Begin(CustomerFlow customerFlow, float queuePatience)
        {
            flow = customerFlow;
            MaxPatience = Patience = queuePatience;
            CurrentState = State.Arriving;
        }

        public void MoveTo(Vector3 position)
        {
            target = position;
            hasTarget = true;
            if (UsingAgent) agent.SetDestination(position);
        }

        internal void SetQueued() => CurrentState = State.InQueue;

        internal void AssignOrder(Order order, float patience)
        {
            Order = order;
            MaxPatience = Patience = patience;
            CurrentState = State.WaitingForFood;
        }

        internal void Leave(Vector3 exit, bool happy)
        {
            LeftHappy = happy;
            CurrentState = State.Leaving;
            MoveTo(exit);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 velocity = Vector3.zero;
            if (hasTarget && !UsingAgent && !HasArrived)
            {
                var d = target - transform.position; d.y = 0f;
                var step = Vector3.ClampMagnitude(d, moveSpeed * dt);
                transform.position += step;
                velocity = step / Mathf.Max(dt, 1e-5f);
                if (d.sqrMagnitude > 1e-4f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d.normalized), turnSpeed * dt);
            }
            else if (UsingAgent) velocity = agent.velocity;
            if (animator != null) animator.SetFloat(SpeedHash, velocity.magnitude);

            switch (CurrentState)
            {
                case State.InQueue:
                    if (flow != null && flow.QueueIndexOf(this) == 0 && HasArrived)
                    {
                        CurrentState = State.ReadyToOrder;
                        flow.OnCustomerReadyToOrder(this);
                    }
                    TickPatience(dt);
                    break;
                case State.ReadyToOrder:
                case State.WaitingForFood:
                    if (HasArrived && flow != null && flow.lookAtPoint != null) FaceTowards(flow.lookAtPoint.position, dt);
                    TickPatience(dt);
                    break;
                case State.Leaving:
                    if (HasArrived) Destroy(gameObject);
                    break;
            }
        }

        void FaceTowards(Vector3 p, float dt)
        {
            var d = p - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d.normalized), turnSpeed * dt);
        }

        void TickPatience(float dt)
        {
            if (KitchenGameManager.Instance != null && KitchenGameManager.Instance.PausePatience) return;
            Patience -= dt;
            if (Patience <= 0f && flow != null) flow.OnCustomerGaveUp(this);
        }

        public void CollectStatus(List<WorldStatus> buffer)
        {
            if (CurrentState == State.Leaving || CurrentState == State.Arriving) return;
            float f = PatienceFraction;
            var kind = f > 0.5f ? StatusKind.Good : f > 0.25f ? StatusKind.Warning : StatusKind.Danger;
            string label = CurrentState == State.WaitingForFood && Order != null ? "#" + Order.Id
                         : CurrentState == State.ReadyToOrder ? "¡Quiero pedir!" : "";
            buffer.Add(new WorldStatus(transform.position + Vector3.up * statusHeight, label, f, kind));
        }
    }
}
