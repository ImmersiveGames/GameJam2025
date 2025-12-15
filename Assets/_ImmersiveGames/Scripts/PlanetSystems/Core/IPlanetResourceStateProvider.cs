namespace _ImmersiveGames.Scripts.PlanetSystems.Core
{
    /// <summary>
    /// Contrato para qualquer objeto que forneça acesso ao estado
    /// de recurso de um planeta.
    ///
    /// A ideia é permitir que sistemas de UI, missões, etc. conversem
    /// com o módulo de recurso sem depender diretamente da classe
    /// concreta PlanetsMaster.
    /// </summary>
    public interface IPlanetResourceStateProvider
    {
        /// <summary>
        /// Componente responsável pelo estado de recurso deste planeta.
        /// Pode ser nulo se o objeto não tiver recurso configurado.
        /// </summary>
        PlanetResourceState ResourceState { get; }
    }
}