using System.Collections.Generic;
using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Hace aparecer clientes mientras el local está abierto, con el ritmo de <see cref="ShiftSettings"/>.
    /// Si no hay prefabs de cliente, crea cápsulas de colores.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public CustomerFlow flow;
        [Tooltip("Prefabs de cliente (con o sin componente Customer; se añade si falta).")]
        public List<GameObject> customerPrefabs = new List<GameObject>();
        public Color[] placeholderColors =
        {
            new Color(0.9f, 0.4f, 0.4f), new Color(0.4f, 0.6f, 0.95f), new Color(0.5f, 0.85f, 0.5f),
            new Color(0.95f, 0.75f, 0.35f), new Color(0.75f, 0.5f, 0.9f),
        };
        [Tooltip("El primer cliente llega a los X segundos de abrir.")]
        public float firstCustomerDelay = 3f;

        float timer;
        KitchenGameManager.ShiftState lastState;

        void Awake()
        {
            if (flow == null) flow = GetComponent<CustomerFlow>();
        }

        void Update()
        {
            var gm = KitchenGameManager.Instance;
            if (gm == null || flow == null) return;
            if (gm.State != lastState)
            {
                lastState = gm.State;
                if (gm.State == KitchenGameManager.ShiftState.Open) timer = firstCustomerDelay;
            }
            if (!gm.IsOpen) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = gm.NextSpawnInterval();
            int max = gm.settings != null ? gm.settings.maxCustomers : 8;
            if (flow.HasRoom && Customer.All.Count < max) Spawn(gm);
        }

        public Customer Spawn(KitchenGameManager gm)
        {
            var origin = flow.spawnPoint != null ? flow.spawnPoint : transform;
            GameObject go;
            if (customerPrefabs.Count > 0)
            {
                var prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
                go = Instantiate(prefab, origin.position, origin.rotation);
            }
            else
            {
                go = new GameObject("Customer");
                go.transform.SetPositionAndRotation(origin.position, origin.rotation);
                var color = placeholderColors.Length > 0 ? placeholderColors[Random.Range(0, placeholderColors.Length)] : Color.gray;
                KitchenUtil.CreatePlaceholder(PrimitiveType.Capsule, new Vector3(0.5f, 1.7f, 0.5f), color, go.transform);
                var face = KitchenUtil.CreatePlaceholder(PrimitiveType.Cube, new Vector3(0.3f, 0.1f, 0.1f), Color.black, go.transform);
                face.transform.localPosition = new Vector3(0f, 1.45f, 0.24f);
            }
            var customer = go.GetComponent<Customer>();
            if (customer == null) customer = go.AddComponent<Customer>();
            customer.Begin(flow, gm.settings != null ? gm.settings.queuePatience : 60f);
            if (!flow.TryEnqueue(customer)) { Destroy(go); return null; }
            return customer;
        }
    }
}
