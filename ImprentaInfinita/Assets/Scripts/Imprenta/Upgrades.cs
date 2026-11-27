using System;
using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    public class UpgradeDef
    {
        public string key, name, desc;
        public double baseCost, growth = 1.6;
        public int maxLevel = 1;
        public Func<SaveData, bool> visible = s => true;
        public Func<SaveData, string> blocked; // devuelve motivo si no se puede comprar aunque sea visible

        public double Cost(int level) => Math.Round(baseCost * Math.Pow(growth, level), 2);
    }

    /// <summary>Todas las mejoras del acto 1 y las cifras derivadas de ellas. Todo el equilibrio está aquí.</summary>
    public static class Econ
    {
        // ---- Terreno ----
        public const double StartArea = 100;     // m² libres al empezar (zona de entrada)
        public const double GoalArea = 400;      // abre El Taller (acto 2)
        public const double AreaPerSheet = 0.1;  // m² que libera cada hoja sacada de la montaña
        public static double FreeArea(SaveData s) => Progress.AreaFor(s.TotalSheets);

        // ---- Valor de las hojas ----
        public const double RecycleValue = 0.05; // hoja en blanco al ducto
        public static double HandPageBase(int chars) => 0.2 + 0.025 * chars;
        public static double MargaritaPageValue(SaveData s) => 1.2 * (1 + 0.25 * s.Level("tinta"));
        public static float MargaritaPeriod(SaveData s) => 14f * Mathf.Pow(0.88f, s.Level("margVel"));
        public static int MargaritaTrayCap(SaveData s) => 20 + 10 * s.Level("margBandeja");
        public const int MaxMargaritas = 4;
        public const float InspirationSeconds = 60f;
        public const int StreakForInspiration = 10;

        // ---- Recogida y transporte ----
        public static float PickSeconds(SaveData s) => s.Level("dedil") > 0 ? 0.9f : 1.5f;
        public static bool NoJams(SaveData s) => s.InkLevel("memoria") > 0;
        public static bool HasGun(SaveData s) => s.Level("pistola") > 0;
        public static float GunSeconds(SaveData s) => new[] { 0.3f, 0.2f, 0.13f, 0.08f }[Mathf.Clamp(s.Level("pistolaPot"), 0, 3)];
        public static float PickRange(SaveData s) => HasGun(s) ? 6f : 3.0f;

        public static int BaseCapacity(SaveData s)
        {
            if (s.Level("caja") > 0) return 100;
            if (s.Level("bandeja") > 0) return 25;
            if (s.Level("carpeta") > 0) return 10;
            return 1;
        }
        public static int Capacity(SaveData s)
        {
            int c = Mathf.RoundToInt(BaseCapacity(s) * (1 + 0.25f * s.Level("capacidad")));
            if (HasGun(s)) c = Mathf.Max(c, 200 + 50 * s.Level("pistolaPot"));
            return Mathf.Max(1, c);
        }
        public static string ContainerName(SaveData s)
        {
            if (HasGun(s)) return "Depósito de la pistola";
            if (s.Level("caja") > 0) return "Caja de folios";
            if (s.Level("bandeja") > 0) return "Bandeja de oficina";
            if (s.Level("carpeta") > 0) return "Carpeta con pinza";
            return "Manos";
        }

        public static int DeskTrayCap(SaveData s) => s.Level("bandejaMaq") > 0 ? 10 * s.Level("bandejaMaq") : 0;

        // ---- Máquina de escribir ----
        public static float KeyCap(SaveData s) => 5.5f + 1.5f * s.Level("muelles"); // pulsaciones por segundo antes de atascarse

        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef { key = "dedil", name = "Dedil de goma", desc = "Coges hojas un 40 % más rápido.", baseCost = 5 },
            new UpgradeDef { key = "bandejaMaq", name = "Bandeja de la máquina", desc = "Deja hojas junto a la máquina: se cargan solas. +10 por nivel.", baseCost = 8, growth = 2.2, maxLevel = 3 },
            new UpgradeDef { key = "carpeta", name = "Carpeta con pinza", desc = "Llevas 10 hojas a la vez.", baseCost = 15, visible = s => s.Level("dedil") > 0 || s.pagesTyped >= 2 },
            new UpgradeDef { key = "cinta", name = "Cinta corta", desc = "Las hojas escritas van solas de la máquina al ducto.", baseCost = 40, visible = s => s.pagesTyped >= 4 },
            new UpgradeDef { key = "muelles", name = "Muelles reforzados", desc = "La máquina aguanta +1,5 pulsaciones por segundo sin atascarse.", baseCost = 25, maxLevel = 8, visible = s => s.everJam || s.pagesTyped >= 6 },
            new UpgradeDef { key = "corrector", name = "Corrector líquido", desc = "Retroceso borra una letra mal escrita (la página pierde poco valor).", baseCost = 30, visible = s => s.everError && s.pagesTyped >= 3 },
            new UpgradeDef { key = "bandeja", name = "Bandeja de oficina", desc = "Llevas 25 hojas.", baseCost = 60, visible = s => s.Level("carpeta") > 0 },
            new UpgradeDef { key = "margarita", name = "Máquina de margarita", desc = "Escribe sola una página cada pocos segundos. Rellena su bandeja con hojas.", baseCost = 50, growth = 1.15, maxLevel = MaxMargaritas, visible = s => s.Level("cinta") > 0 },
            new UpgradeDef { key = "margVel", name = "Margarita: motor", desc = "Las margaritas escriben un 12 % más rápido.", baseCost = 40, maxLevel = 10, visible = s => s.Level("margarita") > 0 },
            new UpgradeDef { key = "tinta", name = "Cinta de tinta nueva", desc = "+25 % de valor en las páginas de las margaritas.", baseCost = 60, maxLevel = 10, visible = s => s.Level("margarita") > 0 },
            new UpgradeDef { key = "margBandeja", name = "Margarita: bandeja grande", desc = "+10 hojas en la bandeja de cada margarita.", baseCost = 45, maxLevel = 5, visible = s => s.Level("margarita") >= 2 },
            new UpgradeDef { key = "capacidad", name = "Recipiente reforzado", desc = "+25 % de capacidad en lo que lleves.", baseCost = 30, growth = 1.9, maxLevel = 5, visible = s => s.Level("carpeta") > 0 },
            new UpgradeDef { key = "caja", name = "Caja de folios", desc = "Llevas 100 hojas.", baseCost = 250, visible = s => s.Level("bandeja") > 0 },
            new UpgradeDef { key = "pistola", name = "Pistola de succión", desc = "Mantén el clic: aspira hojas de la montaña a 6 m a un depósito de 200.", baseCost = 600, visible = s => s.Level("caja") > 0 },
            new UpgradeDef { key = "pistolaPot", name = "Pistola: potencia", desc = "Aspira más rápido y amplía el depósito.", baseCost = 300, growth = 2.0, maxLevel = 3, visible = s => s.Level("pistola") > 0 },
        };

        public static UpgradeDef Get(string key) => All.Find(u => u.key == key);
    }
}
