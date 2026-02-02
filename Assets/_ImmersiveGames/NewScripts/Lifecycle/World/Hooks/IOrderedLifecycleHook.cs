namespace _ImmersiveGames.NewScripts.Lifecycle.World.Hooks
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
