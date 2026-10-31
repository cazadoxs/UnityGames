namespace BurgerKitchen
{
    /// <summary>Reglas para combinar dos objetos (meter carne en el pan, la hamburguesa en la bandeja...).</summary>
    public static class ItemTransfer
    {
        public static bool CanCombine(Item held, Item target)
        {
            if (held == null || target == null || held == target) return false;
            return (target.Container != null && target.Container.CanAccept(held))
                   || (held.Container != null && held.Container.CanAccept(target));
        }

        /// <summary>Mete uno dentro del otro, en el sentido que sea posible.</summary>
        public static bool TryCombine(Item held, Item target)
        {
            if (held == null || target == null || held == target) return false;
            if (target.Container != null && target.Container.TryAdd(held)) return true;
            if (held.Container != null && held.Container.TryAdd(target)) return true;
            return false;
        }

        public static string DescribeCombine(Item held, Item target)
        {
            if (target.Container != null && target.Container.CanAccept(held))
                return $"Poner {held.Definition.DisplayName} en {target.Definition.DisplayName}";
            return $"Añadir {target.Definition.DisplayName}";
        }
    }
}
