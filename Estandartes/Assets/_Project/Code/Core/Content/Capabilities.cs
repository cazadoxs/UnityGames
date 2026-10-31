using System.Collections.Generic;

namespace Estandartes.Core.Content
{
    /// <summary>Capacidades que activan conductas de IA (Vol. III §20.1). Los mods solo pueden usar las existentes.</summary>
    public static class Capabilities
    {
        public const string Shield = "Shield";
        public const string Pavise = "Pavise";
        public const string AntiCavalryFormation = "AntiCavalryFormation";
        public const string ShieldWall = "ShieldWall";
        public const string Reload = "Reload";
        public const string VolleyFire = "VolleyFire";
        public const string SmokeEmitter = "SmokeEmitter";
        public const string Bayonet = "Bayonet";
        public const string UseCover = "UseCover";
        public const string Mounted = "Mounted";
        public const string HorseArcher = "HorseArcher";
        public const string SiegeCrew = "SiegeCrew";
        public const string Duelist = "Duelist";
        public const string Stakes = "Stakes";
        public const string Javelins = "Javelins";
        public const string Engineer = "Engineer";
        public const string Captain = "Captain";
        public const string StandardBearer = "StandardBearer";
        public const string Musician = "Musician";

        private static readonly HashSet<string> Known = new HashSet<string>
        {
            Shield, Pavise, AntiCavalryFormation, ShieldWall, Reload, VolleyFire, SmokeEmitter, Bayonet, UseCover,
            Mounted, HorseArcher, SiegeCrew, Duelist, Stakes, Javelins, Engineer, Captain, StandardBearer, Musician
        };

        public static bool IsKnown(string capability) => Known.Contains(capability);
        public static IEnumerable<string> All => Known;
    }
}
