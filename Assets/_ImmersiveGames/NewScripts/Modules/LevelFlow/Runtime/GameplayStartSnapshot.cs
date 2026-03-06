using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct GameplayStartSnapshot
    {
        public GameplayStartSnapshot(
            LevelDefinitionAsset levelRef,
            SceneRouteId routeId,
            string reason,
            int selectionVersion,
            string levelSignature,
            TransitionStyleId styleId = default)
        {
            LevelRef = levelRef;
            RouteId = routeId;
            StyleId = styleId;
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;

            string normalizedLevelSignature = Sanitize(levelSignature);
            if (string.IsNullOrWhiteSpace(normalizedLevelSignature))
            {
                string levelName = levelRef != null ? levelRef.name : "<null>";
                normalizedLevelSignature = $"level:{levelName}|route:{RouteId}|reason:{Reason}";
            }

            LevelSignature = normalizedLevelSignature;
        }

        public LevelDefinitionAsset LevelRef { get; }
        public SceneRouteId RouteId { get; }
        public TransitionStyleId StyleId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }

        public bool HasLevelRef => LevelRef != null;
        public bool IsValid => RouteId.IsValid;

        [System.Obsolete("Legacy compatibility only. Canonical flow uses LevelRef.")]
        public LevelId LevelId => HasLevelRef ? LevelId.FromName(LevelRef.name) : LevelId.None;

        [System.Obsolete("Legacy compatibility only. Canonical flow does not use contentId.")]
        public string ContentId => string.Empty;

        [System.Obsolete("Legacy compatibility only. Canonical flow uses LevelRef.")]
        public bool HasLevelId => HasLevelRef;

        [System.Obsolete("Legacy compatibility only. Canonical flow does not use contentId.")]
        public bool HasContentId => false;

        public static GameplayStartSnapshot Empty => new(
            null,
            SceneRouteId.None,
            string.Empty,
            0,
            string.Empty,
            TransitionStyleId.None);

        public override string ToString()
        {
            return $"levelRef='{(HasLevelRef ? LevelRef.name : "<none>")}', routeId='{RouteId}', styleId='{StyleId}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', levelSignature='{(string.IsNullOrWhiteSpace(LevelSignature) ? "<none>" : LevelSignature)}'";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
