using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.NewScripts.Modules.ResetInterop.Runtime
{
    /// <summary>
    /// COMMAND/Signal: indica que o reset determinístico do WorldReset concluiu.
    /// Usado para liberar start do GameLoop após ScenesReady + Reset.
    ///
    /// Ownership (canônico):
    /// - Publisher (producao): WorldResetService (Lifecycle) / WorldResetOrchestrator (driver publica apenas em SKIP/fallback).
    /// - Consumidores (produção): WorldResetCompletionGate (SceneFlow) e GameLoopSceneFlowCoordinator (GameLoop).
    /// </summary>
    public readonly struct WorldResetCompletedEvent : IEvent
    {
        public WorldResetCompletedEvent(string contextSignature, string reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string ContextSignature { get; }
        public string Reason { get; }

        public override string ToString()
        {
            return $"WorldResetCompletedEvent(ContextSignature='{ContextSignature}', Reason='{Reason}')";
        }
    }
}


