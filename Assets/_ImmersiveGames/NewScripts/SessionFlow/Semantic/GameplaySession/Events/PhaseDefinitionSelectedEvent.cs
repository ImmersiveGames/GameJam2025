using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events
{
    public readonly struct PhaseDefinitionSelectedEvent : IEvent
    {
        public PhaseDefinitionSelectedEvent(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            int selectionVersion,
            string reason)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            SelectionSignature = BuildSelectionSignature(phaseDefinitionRef, macroRouteId, SelectionVersion, Reason);
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public PhaseDefinitionId PhaseId => PhaseDefinitionRef != null ? PhaseDefinitionRef.PhaseId : PhaseDefinitionId.None;
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public int SelectionVersion { get; }
        public string Reason { get; }
        public string SelectionSignature { get; }

        public bool IsValid =>
            PhaseDefinitionRef != null &&
            PhaseId.IsValid &&
            MacroRouteId.IsValid &&
            MacroRouteRef != null;

        public override string ToString()
        {
            string phaseName = PhaseDefinitionRef != null ? PhaseDefinitionRef.name : "<none>";
            string routeName = MacroRouteRef != null ? MacroRouteRef.name : "<none>";
            return $"phaseId='{PhaseId}', phaseRef='{phaseName}', routeId='{MacroRouteId}', routeRef='{routeName}', v='{SelectionVersion}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', selectionSignature='{(string.IsNullOrWhiteSpace(SelectionSignature) ? "<none>" : SelectionSignature)}'";
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

