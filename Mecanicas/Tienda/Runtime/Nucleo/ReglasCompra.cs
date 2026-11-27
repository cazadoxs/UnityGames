namespace Mecanicas.Tienda
{
    /// <summary>Por qué falló (o no) una compra.</summary>
    public enum MotivoCompra
    {
        Ok,
        ArticuloNulo,
        SinDinero,
        SinStock,
        LimiteAlcanzado,
        EntregaImposible,
        PagoRechazado,
        EnEspera
    }

    /// <summary>
    /// Reglas puras (sin Unity) para decidir si una compra es posible. Se prueban con tests de EditMode.
    /// </summary>
    public static class ReglasCompra
    {
        /// <param name="saldo">Dinero disponible de la moneda del artículo.</param>
        /// <param name="precio">Precio del artículo.</param>
        /// <param name="stockRestante">Unidades que quedan. -1 = infinitas.</param>
        /// <param name="comprasHechas">Veces que ya se ha comprado.</param>
        /// <param name="maxCompras">Límite de compras. 0 = sin límite.</param>
        public static MotivoCompra Validar(int saldo, int precio, int stockRestante, int comprasHechas, int maxCompras)
        {
            if (maxCompras > 0 && comprasHechas >= maxCompras) return MotivoCompra.LimiteAlcanzado;
            if (stockRestante == 0) return MotivoCompra.SinStock;
            if (precio > 0 && saldo < precio) return MotivoCompra.SinDinero;
            return MotivoCompra.Ok;
        }

        /// <summary>Stock tras una compra (el infinito sigue siendo infinito).</summary>
        public static int StockTrasCompra(int stockRestante)
        {
            return stockRestante < 0 ? stockRestante : System.Math.Max(0, stockRestante - 1);
        }
    }
}
