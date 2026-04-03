using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public readonly struct GameplaySessionContextSnapshot
    {
        public static GameplaySessionContextSnapshot FromLevelSelectedEvent(LevelSelectedEvent evt)
        {
            return new GameplaySessionContextSnapshot(
                evt.MacroRouteId,
                evt.MacroRouteRef,
                evt.Reason,
                evt.SelectionVersion);
        }

        public GameplaySessionContextSnapshot(
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string reason,
            int selectionVersion)
        {
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            SessionSignature = BuildSessionSignature(MacroRouteId, SelectionVersion, Reason);
        }

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
            return $"routeId='{MacroRouteId}', routeRef='{(MacroRouteRef != null ? MacroRouteRef.name : "<none>")}', v='{SelectionVersion}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', signature='{(string.IsNullOrWhiteSpace(SessionSignature) ? "<none>" : SessionSignature)}'";
        }

        private static string BuildSessionSignature(SceneRouteId routeId, int selectionVersion, string reason)
        {
            return $"{routeId}|{selectionVersion}|{reason ?? string.Empty}";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
