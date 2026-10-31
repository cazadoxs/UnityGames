using UnityEngine;

namespace BurgerKitchen
{
    public static class ItemFactory
    {
        /// <summary>Capa en la que se crean los objetos (-1 = la capa por defecto).</summary>
        public static int ItemLayer = -1;

        public static Item Spawn(ItemDefinition definition, Vector3 position, Quaternion rotation)
        {
            if (definition == null)
            {
                Debug.LogError("[BurgerKitchen] Se ha intentado crear un objeto sin ItemDefinition.");
                return null;
            }
            var go = new GameObject("Item_" + definition.DisplayName);
            go.transform.SetPositionAndRotation(position, rotation);
            if (ItemLayer >= 0) go.layer = ItemLayer;
            var item = go.AddComponent<Item>();
            item.Initialize(definition);
            return item;
        }
    }
}
