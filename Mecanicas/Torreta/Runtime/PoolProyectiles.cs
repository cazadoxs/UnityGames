using System.Collections.Generic;
using UnityEngine;

namespace Mecanicas.Torretas
{
    /// <summary>
    /// Reutiliza las balas en lugar de crearlas y destruirlas cada vez (crear objetos en mitad
    /// del juego provoca tirones). Hay una reserva por cada prefab de bala distinto.
    /// </summary>
    public static class PoolProyectiles
    {
        static readonly Dictionary<GameObject, Stack<Proyectil>> libres = new Dictionary<GameObject, Stack<Proyectil>>();
        static Transform contenedor;

        /// <summary>Saca una bala del pool (o crea una nueva), colocada y activada.</summary>
        public static Proyectil Obtener(GameObject prefab, Vector3 posicion, Quaternion rotacion)
        {
            if (prefab == null) return null;
            if (!libres.TryGetValue(prefab, out var pila))
            {
                pila = new Stack<Proyectil>();
                libres[prefab] = pila;
            }

            Proyectil p = null;
            while (pila.Count > 0 && p == null) p = pila.Pop(); // salta las que se destruyeron al cambiar de escena

            if (p == null)
            {
                GameObject go = Object.Instantiate(prefab, posicion, rotacion, Contenedor());
                go.name = prefab.name;
                p = go.GetComponent<Proyectil>();
                if (p == null)
                {
                    Debug.LogWarning($"[Torreta] El prefab de bala '{prefab.name}' no tiene el componente Proyectil; se añade uno.", prefab);
                    p = go.AddComponent<Proyectil>();
                }
                p.PrefabOrigen = prefab;
            }
            else
            {
                p.transform.SetPositionAndRotation(posicion, rotacion);
            }
            p.gameObject.SetActive(true);
            return p;
        }

        /// <summary>Guarda la bala para reutilizarla.</summary>
        public static void Devolver(Proyectil p)
        {
            if (p == null) return;
            if (p.PrefabOrigen == null) { Object.Destroy(p.gameObject); return; }
            p.gameObject.SetActive(false);
            if (!libres.TryGetValue(p.PrefabOrigen, out var pila))
            {
                pila = new Stack<Proyectil>();
                libres[p.PrefabOrigen] = pila;
            }
            pila.Push(p);
        }

        /// <summary>Crea balas por adelantado para evitar el primer tirón.</summary>
        public static void Precalentar(GameObject prefab, int cantidad)
        {
            var temp = new List<Proyectil>(cantidad);
            for (int i = 0; i < cantidad; i++) temp.Add(Obtener(prefab, Vector3.zero, Quaternion.identity));
            foreach (var p in temp) Devolver(p);
        }

        /// <summary>Olvida todas las balas guardadas.</summary>
        public static void Vaciar()
        {
            libres.Clear();
            contenedor = null;
        }

        static Transform Contenedor()
        {
            if (contenedor == null) contenedor = new GameObject("[Pool de proyectiles]").transform;
            return contenedor;
        }

        // Necesario si "Enter Play Mode Options" desactiva la recarga del dominio.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reiniciar() => Vaciar();
    }
}
