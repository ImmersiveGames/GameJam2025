namespace _ImmersiveGames.NewScripts.Infrastructure.SceneComposition
{
    public readonly struct SceneCompositionResult
    {
        public SceneCompositionResult(
            bool success,
            SceneCompositionScope scope,
            string reason,
            string correlationId,
            int scenesAdded,
            int scenesRemoved)
        {
            Success = success;
            Scope = scope;
            Reason = reason ?? string.Empty;
            CorrelationId = correlationId ?? string.Empty;
            ScenesAdded = scenesAdded;
            ScenesRemoved = scenesRemoved;
        }

        public bool Success { get; }
        public SceneCompositionScope Scope { get; }
        public string Reason { get; }
        public string CorrelationId { get; }
        public int ScenesAdded { get; }
        public int ScenesRemoved { get; }
    }
}
