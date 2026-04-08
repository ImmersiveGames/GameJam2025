using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public readonly struct PhaseDefinitionSelectedEvent : IEvent
    {
        public PhaseDefinitionSelectedEvent(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            int selectionVersion,
            string reason,
            LevelDefinitionAsset compatLevelRef = null)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            CompatLevelRef = compatLevelRef;
            SelectionSignature = BuildSelectionSignature(phaseDefinitionRef, macroRouteId, SelectionVersion, Reason);
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public PhaseDefinitionId PhaseId => PhaseDefinitionRef != null ? PhaseDefinitionRef.PhaseId : PhaseDefinitionId.None;
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public int SelectionVersion { get; }
        public string Reason { get; }
        public LevelDefinitionAsset CompatLevelRef { get; }
        public string SelectionSignature { get; }

        public bool IsValid =>
            PhaseDefinitionRef != null &&
            PhaseId.IsValid &&
            MacroRouteId.IsValid &&
            MacroRouteRef != null;

        public bool HasCompatLevelRef => CompatLevelRef != null;

        public override string ToString()
        {
            string phaseName = PhaseDefinitionRef != null ? PhaseDefinitionRef.name : "<none>";
            string routeName = MacroRouteRef != null ? MacroRouteRef.name : "<none>";
            return $"phaseId='{PhaseId}', phaseRef='{phaseName}', routeId='{MacroRouteId}', routeRef='{routeName}', v='{SelectionVersion}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', compatLevelRef='{(CompatLevelRef != null ? CompatLevelRef.name : "<none>")}', selectionSignature='{(string.IsNullOrWhiteSpace(SelectionSignature) ? "<none>" : SelectionSignature)}'";
        }

        private static string BuildSelectionSignature(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            int selectionVersion,
            string reason)
        {
            string phaseId = phaseDefinitionRef != null ? phaseDefinitionRef.PhaseId.Value : "<no-phase>";
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "<none>" : reason.Trim();
            return $"{phaseId}|{macroRouteId}|{selectionVersion}|{normalizedReason}";
        }
    }
}
