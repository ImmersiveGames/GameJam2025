using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext
{
    public readonly struct GameplaySessionContextSnapshot
    {
        public static GameplaySessionContextSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return new GameplaySessionContextSnapshot(
                evt.PhaseDefinitionRef,
                evt.MacroRouteId,
                evt.MacroRouteRef,
                evt.Reason,
                evt.SelectionVersion);
        }

        public GameplaySessionContextSnapshot(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string reason,
            int selectionVersion)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            PhaseId = phaseDefinitionRef != null ? phaseDefinitionRef.PhaseId : PhaseDefinitionId.None;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            SessionSignature = BuildSessionSignature(MacroRouteId, PhaseId, SelectionVersion, Reason);
        }

        public GameplaySessionContextSnapshot(
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string reason,
            int selectionVersion)
        {
            PhaseDefinitionRef = null;
            PhaseId = PhaseDefinitionId.None;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            SessionSignature = BuildSessionSignature(MacroRouteId, PhaseId, SelectionVersion, Reason);
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public PhaseDefinitionId PhaseId { get; }
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string SessionSignature { get; }

        public bool IsValid => MacroRouteId.IsValid && MacroRouteRef != null;
        public bool HasSessionSignature => !string.IsNullOrWhiteSpace(SessionSignature);

        public static GameplaySessionContextSnapshot Empty => new(
            SceneRouteId.None,
            null,
            string.Empty,
            0);

        public override string ToString()
        {
            return $"phaseId='{PhaseId}', phaseRef='{(PhaseDefinitionRef != null ? PhaseDefinitionRef.name : "<none>")}', routeId='{MacroRouteId}', routeRef='{(MacroRouteRef != null ? MacroRouteRef.name : "<none>")}', v='{SelectionVersion}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', signature='{(string.IsNullOrWhiteSpace(SessionSignature) ? "<none>" : SessionSignature)}'";
        }

        private static string BuildSessionSignature(SceneRouteId routeId, PhaseDefinitionId phaseId, int selectionVersion, string reason)
        {
            string normalizedPhaseId = phaseId.IsValid ? phaseId.Value : "<no-phase>";
            return $"{normalizedPhaseId}|{routeId}|{selectionVersion}|{reason ?? string.Empty}";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

