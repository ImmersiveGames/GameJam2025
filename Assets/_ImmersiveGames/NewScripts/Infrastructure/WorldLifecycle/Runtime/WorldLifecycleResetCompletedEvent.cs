using _ImmersiveGames.NewScripts.Infrastructure.Events;
namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    /// <summary>
    /// COMMAND/Signal: indica que o reset determinístico do WorldLifecycle concluiu.
    /// Usado para liberar start do GameLoop após ScenesReady + Reset.
    /// </summary>
    public readonly struct WorldLifecycleResetCompletedEvent : IEvent
    {
        public WorldLifecycleResetCompletedEvent(string contextSignature, string reason)
        {
            ContextSignature = contextSignature;
            Reason = reason;
        }

        public string ContextSignature { get; }
        public string Reason { get; }

        public override string ToString()
        {
            return $"WorldLifecycleResetCompleted(ContextSignature='{ContextSignature ?? "<null>"}', Reason='{Reason ?? "<null>"}')";
        }
    }
}
