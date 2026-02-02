using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.NewScripts.Lifecycle.World.Runtime
{
    /// <summary>
    /// COMMAND/Signal: indica que o reset determinístico do WorldLifecycle concluiu.
    /// Usado para liberar start do GameLoop após ScenesReady + Reset.
    ///
    /// Ownership (canônico):
    /// - Publisher (produção): driver de integração WorldLifecycle/SceneFlow (ex.: WorldLifecycleSceneFlowResetDriver).
    /// - Consumidores (produção): WorldLifecycleResetCompletionGate (SceneFlow) e GameLoopSceneFlowCoordinator (GameLoop).
    /// </summary>
    public readonly struct WorldLifecycleResetCompletedEvent : IEvent
    {
        public WorldLifecycleResetCompletedEvent(string contextSignature, string reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string ContextSignature { get; }
        public string Reason { get; }

        public override string ToString()
        {
            return $"WorldLifecycleResetCompleted(ContextSignature='{ContextSignature}', Reason='{Reason}')";
        }
    }
}
