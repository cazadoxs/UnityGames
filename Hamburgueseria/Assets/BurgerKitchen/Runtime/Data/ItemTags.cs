using System;

namespace BurgerKitchen
{
    /// <summary>
    /// Etiquetas que describen qué es un objeto. Los contenedores (papel de hamburguesa,
    /// bandeja, tarrina) aceptan objetos según estas etiquetas.
    /// Para añadir una categoría nueva basta con añadir un valor aquí (siguiente potencia de 2).
    /// </summary>
    [Flags]
    public enum ItemTags
    {
        None = 0,
        Bun = 1 << 0,
        Patty = 1 << 1,
        Topping = 1 << 2,
        Sauce = 1 << 3,
        /// <summary>Se puede apilar dentro de una hamburguesa.</summary>
        BurgerLayer = 1 << 4,
        Side = 1 << 5,
        Drink = 1 << 6,
        Dessert = 1 << 7,
        DessertTopping = 1 << 8,
        /// <summary>Envoltorio o envase de un producto terminado (papel de hamburguesa...).</summary>
        Packaging = 1 << 9,
        Tray = 1 << 10,
        /// <summary>Lote a granel que se reparte en raciones (cesta de patatas).</summary>
        Bulk = 1 << 11,
        RawFood = 1 << 12,
        Waste = 1 << 13,
    }
}
