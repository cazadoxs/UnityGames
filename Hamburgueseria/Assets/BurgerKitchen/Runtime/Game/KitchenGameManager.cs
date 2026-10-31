using UnityEngine;

namespace BurgerKitchen
{
    /// <summary>
    /// Bucle de la jornada: preparar → abrir (llegan clientes) → cerrar (se atiende a los que quedan) → resumen → siguiente día.
    /// Lleva el dinero, la valoración y guarda el progreso en PlayerPrefs.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class KitchenGameManager : MonoBehaviour
    {
        public enum ShiftState { BeforeOpen, Open, Closing, DayOver }

        public static KitchenGameManager Instance { get; private set; }

        public ShiftSettings settings;
        public MenuDefinition menu;
        [Tooltip("Abre el local nada más empezar, sin pulsar Enter.")]
        public bool autoOpen;
        [Tooltip("Guarda día, dinero y valoración entre partidas.")]
        public bool saveProgress = true;
        public string saveKey = "BurgerKitchen";
        [Tooltip("Opcional: lector de entrada para Enter (abrir / siguiente día).")]
        public PlayerInputReader input;

        public ShiftState State { get; private set; } = ShiftState.BeforeOpen;
        public int Day { get; private set; } = 1;
        public int Money { get; private set; }
        /// <summary>0-5 estrellas, media móvil de la satisfacción.</summary>
        public float Rating { get; private set; } = 3f;
        public float TimeRemaining { get; private set; }
        public DayStats Today { get; private set; } = new DayStats();
        public bool IsOpen => State == ShiftState.Open;
        /// <summary>La paciencia no baja antes de abrir ni durante el resumen.</summary>
        public bool PausePatience => State == ShiftState.BeforeOpen || State == ShiftState.DayOver;

        void Awake()
        {
            if (Instance != null && Instance != this) Debug.LogWarning("[BurgerKitchen] Hay dos KitchenGameManager en la escena.");
            Instance = this;
            if (input == null) input = FindFirstObjectByType<PlayerInputReader>();
            Money = settings != null ? settings.startingMoney : 0;
            Load();
            TimeRemaining = settings != null ? settings.shiftDuration : 300f;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            KitchenEvents.RaiseMoneyChanged(Money);
            if (autoOpen) OpenShop();
        }

        void Update()
        {
            if (input != null && input.StartPressed)
            {
                if (State == ShiftState.BeforeOpen) OpenShop();
                else if (State == ShiftState.DayOver) NextDay();
            }

            switch (State)
            {
                case ShiftState.Open:
                    TimeRemaining -= Time.deltaTime;
                    if (TimeRemaining <= 0f)
                    {
                        TimeRemaining = 0f;
                        State = ShiftState.Closing;
                        KitchenEvents.Post("¡Cerramos! Atiende a los clientes que quedan", MessageKind.Info);
                    }
                    break;
                case ShiftState.Closing:
                    if (Customer.All.Count == 0) EndDay();
                    break;
            }
        }

        public void OpenShop()
        {
            if (State != ShiftState.BeforeOpen) return;
            Today = new DayStats();
            TimeRemaining = settings != null ? settings.shiftDuration : 300f;
            State = ShiftState.Open;
            KitchenEvents.RaiseDayStarted(Day);
            KitchenEvents.Post($"¡Día {Day}: abrimos!", MessageKind.Good);
        }

        public void EndDay()
        {
            if (State == ShiftState.DayOver) return;
            State = ShiftState.DayOver;
            KitchenEvents.RaiseDayEnded(Day, Today);
            Save(Day + 1);
        }

        public void NextDay()
        {
            if (State != ShiftState.DayOver) return;
            Day++;
            OrderManager.Instance?.ClearAll();
            State = ShiftState.BeforeOpen;
            TimeRemaining = settings != null ? settings.shiftDuration : 300f;
        }

        public float NextSpawnInterval()
        {
            if (settings == null) return 15f;
            float t = 1f - Mathf.Clamp01(TimeRemaining / Mathf.Max(1f, settings.shiftDuration));
            float min = Mathf.Lerp(settings.spawnIntervalStart.x, settings.spawnIntervalEnd.x, t);
            float max = Mathf.Lerp(settings.spawnIntervalStart.y, settings.spawnIntervalEnd.y, t);
            float speed = 1f + Mathf.Max(0, Day - 1) * settings.dailySpawnSpeedup;
            return Random.Range(min, max) / speed;
        }

        // ---------- Dinero y valoración ----------

        public void RegisterSale(int price, int tip, float satisfaction)
        {
            Today.revenue += price;
            Today.tips += tip;
            Today.ordersCompleted++;
            AddSatisfaction(satisfaction);
            AddMoney(price + tip);
        }

        public void RegisterLostCustomer()
        {
            Today.customersLost++;
            AddSatisfaction(settings != null ? settings.lostCustomerSatisfaction : 0f);
        }

        void AddSatisfaction(float s)
        {
            Today.satisfactionSum += s;
            Today.satisfactionCount++;
            Rating = Mathf.Clamp(Mathf.Lerp(Rating, s * 5f, 0.1f), 0f, 5f);
        }

        public void AddMoney(int amount)
        {
            Money += amount;
            KitchenEvents.RaiseMoneyChanged(Money);
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Money < amount) return false;
            Money -= amount;
            Today.spent += amount;
            KitchenEvents.RaiseMoneyChanged(Money);
            return true;
        }

        public void Refund(int amount)
        {
            Money += amount;
            Today.spent -= amount;
            KitchenEvents.RaiseMoneyChanged(Money);
        }

        // ---------- Guardado ----------

        void Load()
        {
            if (!saveProgress || !PlayerPrefs.HasKey(saveKey + ".day")) return;
            Day = Mathf.Max(1, PlayerPrefs.GetInt(saveKey + ".day", 1));
            Money = PlayerPrefs.GetInt(saveKey + ".money", Money);
            Rating = PlayerPrefs.GetFloat(saveKey + ".rating", Rating);
        }

        void Save(int nextDay)
        {
            if (!saveProgress) return;
            PlayerPrefs.SetInt(saveKey + ".day", nextDay);
            PlayerPrefs.SetInt(saveKey + ".money", Money);
            PlayerPrefs.SetFloat(saveKey + ".rating", Rating);
            PlayerPrefs.Save();
        }

        [ContextMenu("Borrar progreso guardado")]
        public void ResetProgress()
        {
            PlayerPrefs.DeleteKey(saveKey + ".day");
            PlayerPrefs.DeleteKey(saveKey + ".money");
            PlayerPrefs.DeleteKey(saveKey + ".rating");
        }
    }
}
