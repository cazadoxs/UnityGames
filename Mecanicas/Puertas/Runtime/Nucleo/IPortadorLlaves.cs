namespace Mecanicas.Puertas
{
    /// <summary>
    /// Quién guarda las llaves del jugador. <see cref="Llavero"/> es la implementación incluida.
    /// Si tu proyecto tiene su propio inventario, implementa esta interfaz en un componente del jugador
    /// y las cerraduras y candados lo usarán sin más cambios.
    /// </summary>
    public interface IPortadorLlaves
    {
        bool TieneLlave(string idLlave);
        void AnadirLlave(string idLlave, string nombreVisible);
        void QuitarLlave(string idLlave);
    }
}
