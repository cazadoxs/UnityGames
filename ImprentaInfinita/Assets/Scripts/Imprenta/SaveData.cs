using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Imprenta
{
    [Serializable]
    public class PageData
    {
        public string text;
        public float quality;   // multiplicador de calidad (0,3 – 2)
        public bool golden;
        public bool hand;       // escrita por el jugador (página de autor)
        public double value;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public uint seed;
        public double money;
        public double totalEarned;
        public long sheetsPicked;      // hojas sacadas de la montaña (cada una libera terreno)
        public long pagesTyped;        // a mano
        public long pagesMachine;      // margaritas
        public long pagesSold;
        public long phraseSeq;
        public int goldenCount;
        public int streak, bestStreak;
        public float inspirationLeft;
        public int tutorialStep;
        public bool everError, everJam, act1Done;
        public float playTime;
        public long savedAtUnix;

        public List<string> upKeys = new List<string>();
        public List<int> upLevels = new List<int>();

        public int carriedBlank;
        public List<PageData> carriedPages = new List<PageData>();
        public int deskTray;
        public List<int> margaritaTrays = new List<int>();

        // ---- Actos 2 a 8 ----
        public int act = 1, maxAct = 1;
        public double sheetsAuto;               // hojas sacadas por brazos, grúas, excavadoras...
        public double lifetimeEarned, lifetimePages, editionPages;
        public List<string> prodKeys = new List<string>();
        public List<int> prodCounts = new List<int>();
        public List<string> nodeKeys = new List<string>();
        public List<int> nodeLevels = new List<int>();
        public List<string> objKeys = new List<string>();     // «clave/mejora»
        public List<int> objLevels = new List<int>();
        // Jornada (doc 4.17)
        public int day;
        public float dayTime;                   // segundos desde las 8:00
        public double dayPages, dayEarned, order;
        public bool orderMet, clockedIn, nightPending;
        public int ordersMet, punctualDays;
        public bool urgentDone;
        // Ventanilla
        public double reputation = 1, customersServed;
        // Metajuego
        public List<int> scrolls = new List<int>();
        public List<string> achievements = new List<string>();
        public long ink, inkEver, seals, sealsEver;
        public List<string> inkKeys = new List<string>();
        public List<int> inkLevels = new List<int>();
        public int editions, publishers, hall;
        public List<int> hallsDone = new List<int>();
        public long pagesPC;
        public int pigeons, flyingCaught, blackoutsFixed, inspections;
        public bool heardMountain;
        public float boostLeft;

        public double TotalSheets => sheetsPicked + sheetsAuto;

        static int Get(List<string> keys, List<int> vals, string k) { int i = keys.IndexOf(k); return i < 0 ? 0 : vals[i]; }
        static void Put(List<string> keys, List<int> vals, string k, int v) { int i = keys.IndexOf(k); if (i < 0) { keys.Add(k); vals.Add(v); } else vals[i] = v; }
        public int Count(string k) => Get(prodKeys, prodCounts, k);
        public void SetCount(string k, int v) => Put(prodKeys, prodCounts, k, v);
        public int Node(string k) => Get(nodeKeys, nodeLevels, k);
        public void SetNode(string k, int v) => Put(nodeKeys, nodeLevels, k, v);
        public int Obj(string k, string up) => Get(objKeys, objLevels, k + "/" + up);
        public void SetObj(string k, string up, int v) => Put(objKeys, objLevels, k + "/" + up, v);
        public int InkLevel(string k) => Get(inkKeys, inkLevels, k);
        public void SetInk(string k, int v) => Put(inkKeys, inkLevels, k, v);
        public double ObjLevelMult(string k) => (1 + 0.15 * Obj(k, "vel")) * (1 + 0.1 * Obj(k, "cal")) * (Obj(k, "esp") > 0 ? 2 : 1);

        public void Fix()
        {
            prodKeys ??= new List<string>(); prodCounts ??= new List<int>();
            nodeKeys ??= new List<string>(); nodeLevels ??= new List<int>();
            objKeys ??= new List<string>(); objLevels ??= new List<int>();
            scrolls ??= new List<int>(); achievements ??= new List<string>();
            inkKeys ??= new List<string>(); inkLevels ??= new List<int>();
            hallsDone ??= new List<int>(); upKeys ??= new List<string>(); upLevels ??= new List<int>();
            carriedPages ??= new List<PageData>(); margaritaTrays ??= new List<int>();
            if (act < 1) act = act1Done ? 2 : 1;
            if (maxAct < act) maxAct = act;
            if (reputation < 1) reputation = 1;
            if (lifetimeEarned < totalEarned) lifetimeEarned = totalEarned;
        }

        public static string PathFile => System.IO.Path.Combine(Application.persistentDataPath, "imprenta_save.json");

        public int Level(string key)
        {
            int i = upKeys.IndexOf(key);
            return i < 0 ? 0 : upLevels[i];
        }

        public void SetLevel(string key, int lvl)
        {
            int i = upKeys.IndexOf(key);
            if (i < 0) { upKeys.Add(key); upLevels.Add(lvl); }
            else upLevels[i] = lvl;
        }

        public void Save()
        {
            try
            {
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(PathFile, JsonUtility.ToJson(this, true));
            }
            catch (Exception e) { Debug.LogWarning("No se pudo guardar: " + e.Message); }
        }

        public static SaveData LoadOrNew()
        {
            try
            {
                if (File.Exists(PathFile))
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFile));
                    if (d != null && d.version == 1) { d.Fix(); return d; }
                }
            }
            catch (Exception e) { Debug.LogWarning("Partida dañada, se empieza de cero: " + e.Message); }
            return New();
        }

        public static SaveData New()
        {
            var d = new SaveData { seed = (uint)UnityEngine.Random.Range(1, int.MaxValue) };
            d.Fix();
            return d;
        }

        public static void Delete()
        {
            try { if (File.Exists(PathFile)) File.Delete(PathFile); } catch { }
        }
    }
}
