namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks
{
    /// <summary>
    /// Define prioridade de execução para hooks de lifecycle; menor valor roda primeiro.
    /// Valor padrão é 0 quando não implementado.
    /// </summary>
    public interface IWorldLifecycleHookOrdered
    {
        int Order { get; }
    }
}

