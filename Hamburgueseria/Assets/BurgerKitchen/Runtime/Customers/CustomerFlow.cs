using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Recorrido de los clientes por el local: entrada → cola → caja → zona de espera → salida.
    /// Crea Transforms vacíos en tu escena y asígnalos aquí. queuePoints[0] es el sitio delante de la caja.
    /// </summary>
    public class CustomerFlow : MonoBehaviour
    {
        public Transform spawnPoint;
        public Transform exitPoint;
        [Tooltip("Puestos de la cola. El primero es el que está delante de la caja.")]
        public List<Transform> queuePoints = new List<Transform>();
        [Tooltip("Sitios donde esperan su comida tras pedir.")]
        public List<Transform> waitPoints = new List<Transform>();
        [Tooltip("Hacia dónde miran mientras esperan (mostrador).")]
        public Transform lookAtPoint;
        [Tooltip("Los pedidos se toman solos al llegar a la caja (como un quiosco). Si no, el jugador usa la caja.")]
        public bool autoTakeOrders;

        readonly List<Customer> queue = new List<Customer>();
        readonly List<Customer> waiting = new List<Customer>();

        public int QueueCount => queue.Count;
        public bool HasRoom => queue.Count < Mathf.Max(1, queuePoints.Count);
        public Customer CustomerReadyToOrder =>
            queue.Count > 0 && queue[0] != null && queue[0].CurrentState == Customer.State.ReadyToOrder ? queue[0] : null;

        public int QueueIndexOf(Customer c) => queue.IndexOf(c);

        public bool TryEnqueue(Customer c)
        {
            if (!HasRoom) return false;
            queue.Add(c);
            c.SetQueued();
            c.MoveTo(QueuePosition(queue.Count - 1));
            return true;
        }

        Vector3 QueuePosition(int index)
        {
            if (queuePoints.Count == 0) return transform.position;
            if (index < queuePoints.Count) return queuePoints[index].position;
            return queuePoints[queuePoints.Count - 1].position;
        }

        void RefreshQueue()
        {
            queue.RemoveAll(c => c == null);
            for (int i = 0; i < queue.Count; i++) queue[i].MoveTo(QueuePosition(i));
        }

        internal void OnCustomerReadyToOrder(Customer c)
        {
            if (autoTakeOrders && OrderManager.Instance != null) OrderManager.Instance.TakeOrder(c);
        }

        /// <summary>El cliente ha pedido: sale de la cola y va a esperar.</summary>
        internal void MoveToWaiting(Customer c)
        {
            queue.Remove(c);
            waiting.RemoveAll(w => w == null);
            waiting.Add(c);
            c.MoveTo(WaitPosition(waiting.Count - 1));
            RefreshQueue();
        }

        Vector3 WaitPosition(int index)
        {
            if (waitPoints.Count == 0) return lookAtPoint != null ? lookAtPoint.position + Vector3.back : transform.position;
            var basePos = waitPoints[index % waitPoints.Count].position;
            int lap = index / waitPoints.Count;
            if (lap == 0) return basePos;
            float angle = lap * 2.4f;
            return basePos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.6f;
        }

        /// <summary>El cliente se va (servido o no).</summary>
        internal void Release(Customer c, bool happy)
        {
            queue.Remove(c);
            waiting.Remove(c);
            RefreshQueue();
            c.Leave(exitPoint != null ? exitPoint.position : (spawnPoint != null ? spawnPoint.position : transform.position), happy);
        }

        internal void OnCustomerGaveUp(Customer c)
        {
            if (OrderManager.Instance != null) OrderManager.Instance.CustomerGaveUp(c);
            else Release(c, false);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < queuePoints.Count; i++)
            {
                if (queuePoints[i] == null) continue;
                Gizmos.DrawWireSphere(queuePoints[i].position, 0.25f);
                if (i > 0 && queuePoints[i - 1] != null) Gizmos.DrawLine(queuePoints[i - 1].position, queuePoints[i].position);
            }
            Gizmos.color = Color.yellow;
            foreach (var w in waitPoints) if (w != null) Gizmos.DrawWireSphere(w.position, 0.25f);
            Gizmos.color = Color.green;
            if (spawnPoint != null) Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.4f);
            Gizmos.color = Color.red;
            if (exitPoint != null) Gizmos.DrawWireCube(exitPoint.position, Vector3.one * 0.4f);
        }
    }
}
