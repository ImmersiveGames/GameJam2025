namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Define prioridade de execução para hooks de lifecycle; menor valor roda primeiro.
    /// Valor padrão é 0 quando não implementado.
    /// </summary>
    public interface IOrderedLifecycleHook
    {
        int Order { get; }
    }
}
