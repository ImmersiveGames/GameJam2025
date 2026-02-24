namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime
{
    /// <summary>
    /// Evento emitido quando o GameReadinessService publica um novo snapshot.
    /// </summary>
    public readonly struct ReadinessChangedEvent
    {
        public ReadinessSnapshot Snapshot { get; }

        public ReadinessChangedEvent(ReadinessSnapshot snapshot)
        {
            Snapshot = snapshot;
        }

        public override string ToString() => Snapshot.ToString();
    }
}
