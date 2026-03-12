using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct GameplayStartSnapshot
    {
        public GameplayStartSnapshot(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            string reason,
            int selectionVersion,
            string levelSignature,
            TransitionStyleId styleId = default)
        {
            LevelRef = levelRef;
            MacroRouteId = macroRouteId;
            StyleId = styleId;
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;

            string normalizedLevelSignature = Sanitize(levelSignature);
            if (string.IsNullOrWhiteSpace(normalizedLevelSignature))
            {
                string levelName = levelRef != null ? levelRef.name : "<null>";
                normalizedLevelSignature = $"level:{levelName}|route:{MacroRouteId}|reason:{Reason}";
            }

            LevelSignature = normalizedLevelSignature;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public TransitionStyleId StyleId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }

        public bool HasLevelRef => LevelRef != null;
        public bool IsValid => MacroRouteId.IsValid;

        public static GameplayStartSnapshot Empty => new(
            null,
            SceneRouteId.None,
            string.Empty,
            0,
            string.Empty,
            TransitionStyleId.None);

        public override string ToString()
        {
            return $"levelRef='{(HasLevelRef ? LevelRef.name : "<none>")}', routeId='{MacroRouteId}', styleId='{StyleId}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', levelSignature='{(string.IsNullOrWhiteSpace(LevelSignature) ? "<none>" : LevelSignature)}'";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
