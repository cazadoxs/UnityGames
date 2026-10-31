using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>
    /// Estación que transforma objetos con recetas (<see cref="ProcessRecipe"/>). Con la misma clase se montan:
    /// <list type="bullet">
    /// <item>Plancha / freidora: Automatic + WhenPlaced (se cocina solo y se quema si lo dejas).</item>
    /// <item>Tabla de cortar / grifo de refrescos / máquina de helados: HoldToWork (mantener F).</item>
    /// <item>Batidora: Automatic + Manual con varias recetas para el mismo vaso (Q elige sabor, F empieza).</item>
    /// </list>
    /// Añade un <see cref="MachineMaintenance"/> al mismo objeto para limpiezas y averías.
    /// </summary>
    public class ProcessingStation : Station
    {
        public enum WorkMode
        {
            /// <summary>Avanza solo con el tiempo.</summary>
            Automatic,
            /// <summary>Avanza solo mientras el jugador mantiene la acción secundaria.</summary>
            HoldToWork,
        }

        public enum StartMode
        {
            /// <summary>Empieza en cuanto se deja el objeto (si solo hay una receta posible).</summary>
            WhenPlaced,
            /// <summary>Hay que pulsar la acción secundaria para empezar.</summary>
            Manual,
        }

        [Header("Proceso")]
        public List<ProcessRecipe> recipes = new List<ProcessRecipe>();
        public WorkMode workMode = WorkMode.Automatic;
        public StartMode startMode = StartMode.WhenPlaced;
        [Tooltip("Solo deja colocar objetos que tengan receta aquí.")]
        public bool onlyAcceptRecipeInputs = true;
        [Tooltip("Al terminar sigue con la siguiente receta (p. ej. hecha → quemada).")]
        public bool chainRecipes = true;
        [Tooltip("Multiplicador de velocidad (mejoras).")]
        [Min(0.01f)] public float speedMultiplier = 1f;

        [Header("Eventos (sonidos, partículas...)")]
        public UnityEvent onProcessStarted;
        public UnityEvent onProcessCompleted;
        public UnityEvent onOvercooked;
        public UnityEvent onFlipNeeded;
        [Tooltip("True mientras hay algo procesándose (para activar humo, sonido de fritura...).")]
        public UnityEvent<bool> onWorkingChanged;

        class SlotState
        {
            public ProcessRecipe Recipe;
            public float Progress;
            public int Selection;
            public bool Running;
            public bool AwaitingFlip;
            public bool Flipped;
            public bool WorkedThisFrame;

            public void Reset()
            {
                Recipe = null; Progress = 0f; Selection = 0; Running = false;
                AwaitingFlip = false; Flipped = false; WorkedThisFrame = false;
            }
        }

        SlotState[] states;
        MachineMaintenance maintenance;
        readonly Dictionary<ItemDefinition, List<ProcessRecipe>> byInput = new Dictionary<ItemDefinition, List<ProcessRecipe>>();
        static readonly List<ProcessRecipe> NoRecipes = new List<ProcessRecipe>();
        bool wasWorking;

        public MachineMaintenance Maintenance => maintenance;
        bool Operational => maintenance == null || maintenance.IsOperational;

        protected override void Awake()
        {
            base.Awake();
            states = new SlotState[slots.Length];
            for (int i = 0; i < states.Length; i++) states[i] = new SlotState();
            maintenance = GetComponent<MachineMaintenance>();
            RebuildRecipeIndex();
        }

        /// <summary>Llamar si se cambian las recetas en tiempo de ejecución (mejoras, desbloqueos).</summary>
        public void RebuildRecipeIndex()
        {
            byInput.Clear();
            foreach (var r in recipes)
            {
                if (r == null || r.input == null || r.output == null) continue;
                if (!byInput.TryGetValue(r.input, out var list)) byInput[r.input] = list = new List<ProcessRecipe>();
                list.Add(r);
            }
        }

        List<ProcessRecipe> RecipesFor(ItemDefinition def)
            => def != null && byInput.TryGetValue(def, out var l) ? l : NoRecipes;

        public override bool CanPlace(Item item)
        {
            if (item == null) return false;
            if (!onlyAcceptRecipeInputs) return true;
            return RecipesFor(item.Definition).Count > 0;
        }

        protected override void OnItemPlaced(Slot slot)
        {
            var st = states[slot.Index];
            st.Reset();
            var options = RecipesFor(slot.Item.Definition);
            if (options.Count == 0) return;
            st.Recipe = options[0];
            if (workMode == WorkMode.Automatic && startMode == StartMode.WhenPlaced && options.Count == 1)
                Begin(st);
        }

        protected override void OnItemTaken(Slot slot, Item item) => states[slot.Index].Reset();

        void Begin(SlotState st)
        {
            st.Running = true;
            onProcessStarted?.Invoke();
        }

        void Update()
        {
            bool working = false;
            for (int i = 0; i < slots.Length; i++)
            {
                var st = states[i];
                if (slots[i].IsEmpty || st.Recipe == null) continue;
                if (workMode == WorkMode.Automatic && st.Running)
                {
                    Advance(slots[i], Time.deltaTime);
                    working |= st.Running && Operational && !st.AwaitingFlip;
                }
                else if (st.WorkedThisFrame) working = true;
                st.WorkedThisFrame = false;
            }
            if (working != wasWorking)
            {
                wasWorking = working;
                onWorkingChanged?.Invoke(working);
            }
        }

        void Advance(Slot slot, float dt)
        {
            var st = states[slot.Index];
            if (st.Recipe == null || !Operational || st.AwaitingFlip) return;

            st.Progress += dt * speedMultiplier / Mathf.Max(0.01f, st.Recipe.duration);

            if (workMode == WorkMode.Automatic && st.Recipe.flipAt > 0f && !st.Flipped && st.Progress >= st.Recipe.flipAt)
            {
                st.Progress = st.Recipe.flipAt;
                st.AwaitingFlip = true;
                onFlipNeeded?.Invoke();
                return;
            }
            if (st.Progress >= 1f) Complete(slot);
        }

        void Complete(Slot slot)
        {
            var st = states[slot.Index];
            var recipe = st.Recipe;
            slot.Item.SetDefinition(recipe.output);
            if (maintenance != null) maintenance.RegisterUse();
            if (recipe.isOvercook) onOvercooked?.Invoke(); else onProcessCompleted?.Invoke();

            st.Reset();
            var next = RecipesFor(recipe.output);
            if (next.Count == 0) return;
            st.Recipe = next[0];
            if (chainRecipes && workMode == WorkMode.Automatic && next.Count == 1)
                st.Running = true;
        }

        // ---------- Interacción ----------

        public override bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            prompt.Title = stationName;
            bool any = SurfacePrompt(ctx, ref prompt);

            if (maintenance != null && maintenance.NeedsService)
            {
                prompt.Secondary = "Mantener: " + maintenance.ActionLabel;
                prompt.Info = maintenance.IsBroken ? "Averiada" : "Necesita " + maintenance.serviceLabel.ToLowerInvariant();
                if (maintenance.ServiceProgress > 0f) prompt.Progress = maintenance.ServiceProgress;
                return true;
            }

            var slot = NearestSlot(ctx.Point, true);
            if (slot == null) return any;
            var st = states[slot.Index];
            if (st.Recipe == null) return any;

            var options = RecipesFor(slot.Item.Definition);
            if (st.AwaitingFlip) prompt.Secondary = st.Recipe.flipLabel;
            else if (workMode == WorkMode.HoldToWork) prompt.Secondary = "Mantener: " + st.Recipe.actionLabel;
            else if (!st.Running) prompt.Secondary = st.Recipe.actionLabel;

            if (options.Count > 1 && st.Progress <= 0f && !st.Running)
                prompt.Cycle = "Opción: " + st.Recipe.OptionLabel;
            if (st.Progress > 0f) prompt.Progress = st.Progress;
            return true;
        }

        public override void Interact(InteractionContext ctx) => SurfaceInteract(ctx);

        public override void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime)
        {
            if (maintenance != null && maintenance.NeedsService)
            {
                if (maintenance.Service(deltaTime))
                    KitchenEvents.Post(stationName + " lista", MessageKind.Good);
                return;
            }

            var slot = NearestSlot(ctx.Point, true);
            if (slot == null) return;
            var st = states[slot.Index];
            if (st.Recipe == null) return;

            if (st.AwaitingFlip)
            {
                if (pressedThisFrame) { st.AwaitingFlip = false; st.Flipped = true; }
                return;
            }

            if (workMode == WorkMode.HoldToWork)
            {
                if (st.Progress <= 0f) onProcessStarted?.Invoke();
                st.WorkedThisFrame = true;
                Advance(slot, deltaTime);
            }
            else if (!st.Running && pressedThisFrame && Operational)
            {
                Begin(st);
            }
        }

        public override void Cycle(InteractionContext ctx, int direction)
        {
            var slot = NearestSlot(ctx.Point, true);
            if (slot == null) return;
            var st = states[slot.Index];
            if (st.Running || st.Progress > 0f) return;
            var options = RecipesFor(slot.Item.Definition);
            if (options.Count < 2) return;
            st.Selection = ((st.Selection + direction) % options.Count + options.Count) % options.Count;
            st.Recipe = options[st.Selection];
        }

        public override void CollectStatus(List<WorldStatus> buffer)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                var st = states[i];
                if (slots[i].IsEmpty || st.Recipe == null) continue;
                var pos = StatusPosition(slots[i]);
                if (st.AwaitingFlip)
                    buffer.Add(new WorldStatus(pos, "¡" + st.Recipe.flipLabel + "!", st.Progress, StatusKind.Warning));
                else if (st.Running || st.Progress > 0f)
                {
                    var kind = st.Recipe.isOvercook ? StatusKind.Danger : StatusKind.Normal;
                    var label = st.Recipe.isOvercook ? "¡Se quema!" : st.Recipe.actionLabel;
                    buffer.Add(new WorldStatus(pos, label, st.Progress, kind));
                }
                else if (slots[i].Item.Definition.isFailedState)
                    buffer.Add(new WorldStatus(pos, "Estropeado", -1f, StatusKind.Danger));
            }
        }
    }
}
