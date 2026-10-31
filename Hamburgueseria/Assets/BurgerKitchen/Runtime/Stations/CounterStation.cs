namespace BurgerKitchen
{
    /// <summary>Encimera: dejar objetos y montar encima (hamburguesas, bandejas).</summary>
    public class CounterStation : Station
    {
        protected override void Awake()
        {
            if (stationName == "Estación") stationName = "Encimera";
            base.Awake();
        }
    }
}
