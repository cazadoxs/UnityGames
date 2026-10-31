using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace BurgerKitchen
{
    /// <summary>
    /// Bandeja de reparto a granel (p. ej. patatas): se vuelca un lote y se sirven raciones en envases vacíos.
    /// Las raciones pueden ponerse frías con el tiempo y entonces hay que tirarlas.
    /// </summary>
    public class BulkPortionStation : MonoBehaviour, IInteractable, IWorldStatusSource
    {
        [Serializable]
        public class PortionRecipe
        {
            public ItemDefinition emptyContainer;
            public ItemDefinition filledResult;
        }

        public string stationName = "Reparto de patatas";
        [Tooltip("Lote que se vuelca (cesta de patatas fritas).")]
        public ItemDefinition bulkInput;
        [Min(1)] public int portionsPerBulk = 5;
        [Min(1)] public int maxPortions = 10;
        public List<PortionRecipe> portionRecipes = new List<PortionRecipe>();
        [Tooltip("Segundos hasta que las raciones se enfrían (0 = nunca).")]
        public float freshDuration = 90f;
        [Tooltip("Objetos que se activan según las raciones que quedan (opcional, de menos a más).")]
        public GameObject[] fillVisuals = new GameObject[0];
        public Vector3 statusOffset = new Vector3(0f, 1.4f, 0f);

        public UnityEvent onRefilled;
        public UnityEvent onPortionServed;

        public int Portions { get; private set; }
        public bool IsStale => freshDuration > 0f && Portions > 0 && freshTimer <= 0f;
        float freshTimer;

        void OnEnable() => WorldStatusRegistry.Register(this);
        void OnDisable() => WorldStatusRegistry.Unregister(this);
        void Start() => RefreshVisuals();

        void Update()
        {
            if (Portions > 0 && freshDuration > 0f && freshTimer > 0f) freshTimer -= Time.deltaTime;
        }

        PortionRecipe RecipeFor(ItemDefinition def)
        {
            foreach (var r in portionRecipes) if (r != null && r.emptyContainer == def) return r;
            return null;
        }

        public bool GetPrompt(InteractionContext ctx, ref InteractionPrompt prompt)
        {
            prompt.Title = stationName;
            prompt.Info = Portions > 0 ? $"Raciones: {Portions}{(IsStale ? " (frías)" : "")}" : "Vacío";
            var held = ctx.Held;
            if (held != null && held.Definition == bulkInput && !held.IsDirty)
                prompt.Primary = Portions >= maxPortions ? null : "Volcar " + held.Definition.DisplayName;
            else if (held != null && RecipeFor(held.Definition) != null && Portions > 0 && !IsStale)
                prompt.Primary = "Llenar " + held.Definition.DisplayName;
            if (IsStale) prompt.Secondary = "Mantener: tirar raciones frías";
            return true;
        }

        public void Interact(InteractionContext ctx)
        {
            var held = ctx.Held;
            if (held == null) return;
            if (held.Definition == bulkInput && !held.IsDirty && Portions < maxPortions)
            {
                if (IsStale) Portions = 0;
                Portions = Mathf.Min(maxPortions, Portions + portionsPerBulk);
                freshTimer = freshDuration;
                held.Consume();
                onRefilled?.Invoke();
                RefreshVisuals();
                return;
            }
            var recipe = RecipeFor(held.Definition);
            if (recipe != null && Portions > 0 && !IsStale)
            {
                held.SetDefinition(recipe.filledResult);
                Portions--;
                onPortionServed?.Invoke();
                RefreshVisuals();
            }
        }

        float discardHold;
        public void InteractAlt(InteractionContext ctx, bool pressedThisFrame, float deltaTime)
        {
            if (!IsStale) return;
            if (pressedThisFrame) discardHold = 0f;
            discardHold += deltaTime;
            if (discardHold >= 0.6f) { Portions = 0; discardHold = 0f; RefreshVisuals(); }
        }

        public void Cycle(InteractionContext ctx, int direction) { }

        void RefreshVisuals()
        {
            if (fillVisuals == null || fillVisuals.Length == 0) return;
            int active = Mathf.CeilToInt(fillVisuals.Length * (Portions / (float)maxPortions));
            for (int i = 0; i < fillVisuals.Length; i++)
                if (fillVisuals[i] != null) fillVisuals[i].SetActive(i < active);
        }

        public void CollectStatus(List<WorldStatus> buffer)
        {
            var pos = transform.TransformPoint(statusOffset);
            if (Portions == 0) return;
            if (IsStale) buffer.Add(new WorldStatus(pos, "Frías", -1f, StatusKind.Danger));
            else buffer.Add(new WorldStatus(pos, $"x{Portions}",
                freshDuration > 0f ? freshTimer / freshDuration : -1f,
                freshDuration > 0f && freshTimer < freshDuration * 0.25f ? StatusKind.Warning : StatusKind.Good));
        }
    }
}
