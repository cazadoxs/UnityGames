using System.Collections.Generic;

namespace BurgerKitchen
{
    /// <summary>
    /// Foto de un objeto (lo que es y lo que contiene) independiente de la escena.
    /// Permite comprobar pedidos y probar la lógica sin GameObjects.
    /// </summary>
    public readonly struct ItemSnapshot
    {
        public readonly ItemDefinition Definition;
        public readonly IReadOnlyList<ItemDefinition> Contents;
        public readonly bool IsDirty;

        static readonly ItemDefinition[] Empty = new ItemDefinition[0];

        public ItemSnapshot(ItemDefinition definition, IReadOnlyList<ItemDefinition> contents = null, bool isDirty = false)
        {
            Definition = definition;
            Contents = contents ?? Empty;
            IsDirty = isDirty;
        }
    }
}
