using UnityEngine;

namespace Mecanicas.Construccion.Demo
{
    /// <summary>
    /// Monedero de ejemplo: conecta el coste de construir con un contador de monedas y añade la
    /// etiqueta {dinero} a la leyenda. Sustitúyelo por tu propio sistema de economía heredando de
    /// <see cref="BuildCostProvider"/>.
    /// </summary>
    public class DemoWallet : BuildCostProvider
    {
        [SerializeField] private int money = 300;
        [Tooltip("Monedas que se ganan cada segundo (para poder seguir probando).")]
        [SerializeField] private float incomePerSecond = 5f;
        [SerializeField] private BuildLegend legend;

        private float _income;

        public int Money => money;

        private void OnEnable()
        {
            if (legend != null) legend.ResolveToken += ResolveToken;
        }

        private void OnDisable()
        {
            if (legend != null) legend.ResolveToken -= ResolveToken;
        }

        private string ResolveToken(string token) => token == "dinero" ? money.ToString() : null;

        private void Update()
        {
            _income += incomePerSecond * Time.deltaTime;
            if (_income >= 1f)
            {
                int whole = Mathf.FloorToInt(_income);
                money += whole;
                _income -= whole;
            }
        }

        public override bool CanAfford(BuildableDefinition definition, int cost) => money >= cost;

        public override void Spend(BuildableDefinition definition, int cost) => money -= cost;

        public override void Refund(BuildableDefinition definition, int amount) => money += amount;
    }
}
