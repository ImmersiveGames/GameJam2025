using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    public readonly struct GameplayStartSnapshot
    {
        public static GameplayStartSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayStartSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to materialize the gameplay start snapshot.");
            }

            return new GameplayStartSnapshot(
                evt.PhaseDefinitionRef,
                evt.MacroRouteId,
                evt.MacroRouteRef,
                PhaseDefinitionId.BuildCanonicalIntroContentId(evt.PhaseDefinitionRef.PhaseId),
                evt.Reason,
                evt.SelectionVersion,
                evt.SelectionSignature);
        }

        public GameplayStartSnapshot(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string phaseSignature)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            LocalContentId = ResolveLocalContentId(phaseDefinitionRef, localContentId);
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            PhaseSignature = NormalizeSignature(phaseDefinitionRef, macroRouteId, reason, phaseSignature);
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string PhaseSignature { get; }

        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool HasLocalContentId => !string.IsNullOrWhiteSpace(LocalContentId);
        public bool IsValid => MacroRouteId.IsValid && MacroRouteRef != null && HasPhaseDefinitionRef;

        public static GameplayStartSnapshot Empty => new(
            null,
            SceneRouteId.None,
            null,
            string.Empty,
            string.Empty,
            0,
            string.Empty);

        public override string ToString()
        {
            return $"phaseRef='{(HasPhaseDefinitionRef ? PhaseDefinitionRef.name : "<none>")}', routeId='{MacroRouteId}', localContentId='{(HasLocalContentId ? LocalContentId : "<none>")}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', phaseSignature='{(string.IsNullOrWhiteSpace(PhaseSignature) ? "<none>" : PhaseSignature)}'";
        }

        private static string ResolveLocalContentId(PhaseDefinitionAsset phaseDefinitionRef, string localContentId)
        {
            if (!string.IsNullOrWhiteSpace(localContentId))
            {
                return localContentId.Trim();
            }

            if (phaseDefinitionRef != null)
            {
                return PhaseDefinitionId.BuildCanonicalIntroContentId(phaseDefinitionRef.PhaseId);
            }

            return string.Empty;
        }

        private static string NormalizeSignature(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId routeId,
            string reason,
            string phaseSignature)
        {
            string normalized = Sanitize(phaseSignature);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            if (phaseDefinitionRef != null)
            {
                string phaseName = phaseDefinitionRef.name;
                return $"phase:{phaseName}|route:{routeId}|reason:{reason}";
            }

            return $"phase:<none>|route:{routeId}|reason:{reason}";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
