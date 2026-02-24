using _ImmersiveGames.NewScripts.Core.Events;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    /// <summary>
    /// Evento canônico publicado quando um ResetWorld inicia.
    /// </summary>
    public readonly struct WorldLifecycleResetStartedEvent : IEvent
    {
        public WorldLifecycleResetStartedEvent(string contextSignature, string reason)
        {
            ContextSignature = contextSignature ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public string ContextSignature { get; }
        public string Reason { get; }

        public override string ToString()
        {
            return $"WorldLifecycleResetStarted(ContextSignature='{ContextSignature}', Reason='{Reason}')";
        }
    }
}


