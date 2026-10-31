using System;
using System.Collections.Generic;

namespace BurgerKitchen
{
    /// <summary>Genera pedidos aleatorios a partir de la carta. Lógica pura, sin escena.</summary>
    public static class OrderGenerator
    {
        public static List<ProductDefinition> Generate(MenuDefinition menu, int day, Random rng)
        {
            var result = new List<ProductDefinition>();
            if (menu == null || menu.entries == null) return result;

            int maxItems = Math.Max(1, menu.maxItemsPerOrder + (int)Math.Floor(Math.Max(0, day - 1) * menu.extraItemsPerDay));

            TryAdd(result, menu, day, MenuCourse.Main, menu.mainChance, rng, maxItems);
            if (result.Count > 0) TryAdd(result, menu, day, MenuCourse.Main, menu.secondMainChance, rng, maxItems);
            TryAdd(result, menu, day, MenuCourse.Side, menu.sideChance, rng, maxItems);
            TryAdd(result, menu, day, MenuCourse.Drink, menu.drinkChance, rng, maxItems);
            TryAdd(result, menu, day, MenuCourse.Dessert, menu.dessertChance, rng, maxItems);

            if (result.Count == 0)
            {
                var any = PickWeighted(menu, day, null, rng);
                if (any != null) result.Add(any);
            }
            return result;
        }

        static void TryAdd(List<ProductDefinition> result, MenuDefinition menu, int day, MenuCourse course,
            float chance, Random rng, int maxItems)
        {
            if (result.Count >= maxItems || rng.NextDouble() >= chance) return;
            var p = PickWeighted(menu, day, course, rng);
            if (p != null) result.Add(p);
        }

        public static ProductDefinition PickWeighted(MenuDefinition menu, int day, MenuCourse? course, Random rng)
        {
            double total = 0;
            foreach (var e in menu.entries)
                if (IsAvailable(e, day, course)) total += e.weight;
            if (total <= 0) return null;

            double roll = rng.NextDouble() * total;
            foreach (var e in menu.entries)
            {
                if (!IsAvailable(e, day, course)) continue;
                roll -= e.weight;
                if (roll <= 0) return e.product;
            }
            // Redondeo: devolver el último disponible.
            for (int i = menu.entries.Count - 1; i >= 0; i--)
                if (IsAvailable(menu.entries[i], day, course)) return menu.entries[i].product;
            return null;
        }

        static bool IsAvailable(MenuEntry e, int day, MenuCourse? course)
        {
            return e != null && e.product != null && e.weight > 0 && e.unlockDay <= day
                   && (!course.HasValue || e.product.course == course.Value);
        }
    }
}
