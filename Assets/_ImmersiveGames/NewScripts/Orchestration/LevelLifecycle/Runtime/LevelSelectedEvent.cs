using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct LevelSelectedEvent : IEvent
    {
        public LevelSelectedEvent(
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            LevelDefinitionAsset levelRef,
            int selectionVersion,
            string localContentId,
            string reason,
            string levelSignature)
        {
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            LevelRef = levelRef;
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LocalContentId = LevelFlowContentDefaults.Normalize(localContentId, levelRef);
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
        }

        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public LevelDefinitionAsset LevelRef { get; }
        public int SelectionVersion { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public string LevelSignature { get; }
    }
}
