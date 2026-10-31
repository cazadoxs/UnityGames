using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>Reglas de una jornada: duración, ritmo de clientes, paciencia y dinero.</summary>
    [CreateAssetMenu(menuName = "Burger Kitchen/Shift Settings", fileName = "Shift_Normal")]
    public class ShiftSettings : ScriptableObject
    {
        [Header("Jornada")]
        [Min(10f)] public float shiftDuration = 300f;
        public int startingMoney = 50;

        [Header("Clientes")]
        [Tooltip("Segundos entre clientes al abrir (mín, máx).")]
        public Vector2 spawnIntervalStart = new Vector2(16f, 24f);
        [Tooltip("Segundos entre clientes al final de la jornada (mín, máx).")]
        public Vector2 spawnIntervalEnd = new Vector2(8f, 12f);
        [Tooltip("Cada día los clientes llegan este porcentaje más rápido.")]
        public float dailySpawnSpeedup = 0.08f;
        [Min(1)] public int maxCustomers = 8;

        [Header("Paciencia (segundos)")]
        public float queuePatience = 60f;
        public float basePatience = 45f;
        public float patiencePerItem = 15f;

        [Header("Dinero")]
        [Tooltip("Propina máxima como fracción del precio, si se sirve con la paciencia llena.")]
        [Range(0f, 2f)] public float maxTipFraction = 0.5f;
        [Tooltip("Valoración que pierdes cuando un cliente se va sin comer (0-1).")]
        [Range(0f, 1f)] public float lostCustomerSatisfaction = 0f;
    }
}
