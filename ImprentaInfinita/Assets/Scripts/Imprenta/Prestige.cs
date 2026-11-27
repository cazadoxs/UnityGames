using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Las tres capas de prestigio (doc 6.3): Nueva edición (tinta dorada), Editorial (sellos)
    /// y Biblioteca infinita (naves nuevas con reglas propias).
    /// </summary>
    public static class Prestige
    {
        public static long EditionGain(SaveData s) => Progress.InkFor(s.editionPages);
        public static bool CanEdition(SaveData s) => s.act >= 4 && EditionGain(s) >= 1;

        public static long PublisherGain(SaveData s) => System.Math.Max(0, Progress.SealsFor(s.inkEver + EditionGain(s)) - s.sealsEver);
        public static bool CanPublisher(SaveData s) => s.act >= 7 && PublisherGain(s) >= 1;

        public static bool CanLibrary(SaveData s) => s.act >= 8 && s.publishers >= 1;

        /// <summary>Lo que sobrevive a cualquier prestigio: metajuego, estadísticas de por vida y ajustes.</summary>
        static SaveData Carry(SaveData s, bool keepInk, bool keepSeals)
        {
            var n = SaveData.New();
            n.lifetimeEarned = s.lifetimeEarned; n.lifetimePages = s.lifetimePages;
            n.achievements = new List<string>(s.achievements);
            n.bestStreak = s.bestStreak; n.goldenCount = s.goldenCount; n.phraseSeq = s.phraseSeq; n.seed = s.seed;
            n.punctualDays = s.punctualDays; n.ordersMet = s.ordersMet; n.pagesPC = s.pagesPC;
            n.pigeons = s.pigeons; n.flyingCaught = s.flyingCaught; n.blackoutsFixed = s.blackoutsFixed; n.inspections = s.inspections; n.heardMountain = s.heardMountain;
            n.maxAct = s.maxAct; n.editions = s.editions; n.publishers = s.publishers; n.hall = s.hall; n.hallsDone = new List<int>(s.hallsDone);
            n.seals = s.seals; n.sealsEver = s.sealsEver;
            n.everError = s.everError; n.everJam = s.everJam; n.playTime = s.playTime;
            n.tutorialStep = 5;
            if (keepInk) { n.ink = s.ink; n.inkEver = s.inkEver; }
            for (int i = 0; i < s.inkKeys.Count; i++)
            {
                var node = Progress.Ink(s.inkKeys[i]);
                if (node == null) continue;
                if (node.layer == 1 && !keepInk) continue;
                if (node.layer == 2 && !keepSeals) continue;
                n.SetInk(s.inkKeys[i], s.inkLevels[i]);
            }
            return n;
        }

        /// <summary>Lo que dan las mejoras permanentes al empezar una vuelta.</summary>
        static void ApplyStart(SaveData n, SaveData old)
        {
            if (n.InkLevel("arranque") > 0) { n.SetLevel("dedil", 1); n.SetLevel("carpeta", 1); n.SetLevel("cinta", 1); }
            if (n.InkLevel("pistolaIni") > 0) { n.SetLevel("carpeta", 1); n.SetLevel("bandeja", 1); n.SetLevel("caja", 1); n.SetLevel("pistola", 1); }
            if (n.InkLevel("herencia") > 0) { n.SetCount("brazo", 5); n.SetCount("mecanografo", 10); }
            int cap = n.InkLevel("capital");
            if (cap > 0) n.money = 1000 * System.Math.Pow(10, cap - 1);
            if (n.InkLevel("pergaminosFijos") > 0) n.scrolls = new List<int>(old.scrolls);
            if (n.InkLevel("selloActos") > 0) n.sheetsAuto = Progress.SheetsForArea(1200);
        }

        public static void DoEdition(Game g)
        {
            var s = g.S;
            long gain = EditionGain(s);
            if (!CanEdition(s)) return;
            var n = Carry(s, true, true);
            n.ink += gain; n.inkEver += gain;
            n.editions = s.editions + 1;
            ApplyStart(n, s);
            Sfx.Play("prestige", 1f);
            g.LoadNewRun(n);
        }

        public static void DoPublisher(Game g)
        {
            var s = g.S;
            if (!CanPublisher(s)) return;
            long gain = PublisherGain(s);
            var n = Carry(s, false, true);
            n.seals = s.seals + gain; n.sealsEver = s.sealsEver + gain;
            n.publishers = s.publishers + 1;
            n.editions = 0;
            if (s.InkLevel("selloTinta") > 0) { n.ink = (long)((s.ink + EditionGain(s)) * 0.1); n.inkEver = n.ink; }
            ApplyStart(n, s);
            Sfx.Play("prestige", 1f);
            g.LoadNewRun(n);
        }

        public static void DoLibrary(Game g, int hall)
        {
            var s = g.S;
            if (!CanLibrary(s) || hall < 1 || hall > 4 || s.hallsDone.Contains(hall)) return;
            var n = Carry(s, true, true);
            n.hall = hall;
            ApplyStart(n, s);
            Sfx.Play("prestige", 1f);
            g.LoadNewRun(n);
        }

        public static bool BuyInk(SaveData s, InkNode node)
        {
            int lvl = s.InkLevel(node.key);
            if (lvl >= node.maxLevel) return false;
            if (node.layer == 1) { if (s.ink < node.cost) return false; s.ink -= node.cost; }
            else { if (s.seals < node.cost) return false; s.seals -= node.cost; }
            s.SetInk(node.key, lvl + 1);
            return true;
        }
    }
}
