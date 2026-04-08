using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct GameplayStartSnapshot
    {
        public static GameplayStartSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return new GameplayStartSnapshot(
                null,
                evt.LevelRef,
                evt.MacroRouteId,
                evt.MacroRouteRef,
                evt.LocalContentId,
                evt.Reason,
                evt.SelectionVersion,
                evt.LevelSignature);
        }

        public static GameplayStartSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayStartSnapshot),
                    "[FATAL][H1][LevelFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to materialize the gameplay start snapshot.");
            }

            return new GameplayStartSnapshot(
                evt.PhaseDefinitionRef,
                null,
                evt.MacroRouteId,
                evt.MacroRouteRef,
                evt.PhaseDefinitionRef.BuildCanonicalIntroContentId(),
                evt.Reason,
                evt.SelectionVersion,
                evt.SelectionSignature);
        }

        public GameplayStartSnapshot(
            PhaseDefinitionAsset phaseDefinitionRef,
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            LevelRef = levelRef;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            LocalContentId = ResolveLocalContentId(phaseDefinitionRef, levelRef, localContentId);
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = NormalizeSignature(phaseDefinitionRef, levelRef, macroRouteId, reason, levelSignature);
        }

        public GameplayStartSnapshot(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature)
            : this(null, levelRef, macroRouteId, macroRouteRef, localContentId, reason, selectionVersion, levelSignature)
        {
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public LevelDefinitionAsset LevelRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }

        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool HasLevelRef => LevelRef != null;
        public bool HasLocalContentId => !string.IsNullOrWhiteSpace(LocalContentId);
        public bool IsValid => MacroRouteId.IsValid && MacroRouteRef != null && (HasPhaseDefinitionRef || HasLevelRef);

        public static GameplayStartSnapshot Empty => new(
            null,
            null,
            SceneRouteId.None,
            null,
            string.Empty,
            string.Empty,
            0,
            string.Empty);

        public override string ToString()
        {
            return $"phaseRef='{(HasPhaseDefinitionRef ? PhaseDefinitionRef.name : "<none>")}', levelRef='{(HasLevelRef ? LevelRef.name : "<none>")}', routeId='{MacroRouteId}', localContentId='{(HasLocalContentId ? LocalContentId : "<none>")}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', levelSignature='{(string.IsNullOrWhiteSpace(LevelSignature) ? "<none>" : LevelSignature)}'";
        }

        private static string ResolveLocalContentId(LevelDefinitionAsset levelRef, string localContentId)
        {
            return LevelFlowContentDefaults.Normalize(localContentId, levelRef);
        }

        private static string ResolveLocalContentId(PhaseDefinitionAsset phaseDefinitionRef, LevelDefinitionAsset levelRef, string localContentId)
        {
            if (!string.IsNullOrWhiteSpace(localContentId))
            {
                return localContentId.Trim();
            }

            if (phaseDefinitionRef != null)
            {
                return phaseDefinitionRef.BuildCanonicalIntroContentId();
            }

            return LevelFlowContentDefaults.Normalize(localContentId, levelRef);
        }

        private static string NormalizeSignature(
            PhaseDefinitionAsset phaseDefinitionRef,
            LevelDefinitionAsset levelRef,
            SceneRouteId routeId,
            string reason,
            string levelSignature)
        {
            string normalized = Sanitize(levelSignature);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            if (phaseDefinitionRef != null)
            {
                string phaseName = phaseDefinitionRef.name;
                return $"phase:{phaseName}|route:{routeId}|reason:{reason}";
            }

            string levelName = levelRef != null ? levelRef.name : "<null>";
            return $"level:{levelName}|route:{routeId}|reason:{reason}";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
