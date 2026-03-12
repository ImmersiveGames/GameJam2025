using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct LevelSelectedEvent : IEvent
    {
        public LevelSelectedEvent(
            SceneRouteId macroRouteId,
            LevelDefinitionAsset levelRef,
            int selectionVersion,
            string reason,
            string levelSignature)
        {
            MacroRouteId = macroRouteId;
            LevelRef = levelRef;
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
        }

        public SceneRouteId MacroRouteId { get; }
        public LevelDefinitionAsset LevelRef { get; }
        public int SelectionVersion { get; }
        public string Reason { get; }
        public string LevelSignature { get; }
    }
}
