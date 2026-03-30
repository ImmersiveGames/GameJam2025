using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct LevelSwapLocalAppliedEvent : IEvent
    {
        public LevelSwapLocalAppliedEvent(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature)
        {
            LevelRef = levelRef;
            MacroRouteId = macroRouteId;
            LocalContentId = LevelFlowContentDefaults.Normalize(localContentId, levelRef);
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = string.IsNullOrWhiteSpace(levelSignature) ? string.Empty : levelSignature.Trim();
        }

        public LevelDefinitionAsset LevelRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }
    }
}
