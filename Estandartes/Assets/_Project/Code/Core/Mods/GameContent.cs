using Estandartes.Core.Maps;
using Estandartes.Core.Strategy;

namespace Estandartes.Core.Mods
{
    /// <summary>Configuración del cargador con todos los tipos de contenido del juego.</summary>
    public static class GameContent
    {
        /// <summary>Versión del juego para comprobar la compatibilidad de los mods.</summary>
        public static readonly SemVer GameVersion = new SemVer(0, 1, 0);

        public static ModLoader CreateLoader()
        {
            var loader = new ModLoader();
            loader.RegisterFolder("data/htn", typeof(HtnDomainDefinition), changesRules: true);
            loader.RegisterFolder("scenarios", typeof(ScenarioDefinition));
            loader.AddValidator(new HtnReferenceValidator());
            loader.AddValidator(new ScenarioReferenceValidator());
            return loader;
        }
    }
}
