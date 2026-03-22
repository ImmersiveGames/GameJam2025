using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneComposition
{
    public readonly struct SceneCompositionRequest
    {
        public SceneCompositionRequest(
            SceneCompositionScope scope,
            string reason,
            string correlationId,
            LevelDefinitionAsset previousLevelRef,
            LevelDefinitionAsset targetLevelRef)
        {
            Scope = scope;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? string.Empty : correlationId.Trim();
            PreviousLevelRef = previousLevelRef;
            TargetLevelRef = targetLevelRef;
        }

        public SceneCompositionScope Scope { get; }
        public string Reason { get; }
        public string CorrelationId { get; }
        public LevelDefinitionAsset PreviousLevelRef { get; }
        public LevelDefinitionAsset TargetLevelRef { get; }

        public bool IsClearRequest => TargetLevelRef == null;
    }
}
