using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    public readonly struct GameplayStartSnapshot
    {
        public GameplayStartSnapshot(
            LevelDefinitionAsset levelRef,
            SceneRouteId macroRouteId,
            string localContentId,
            string reason,
            int selectionVersion,
            string levelSignature)
        {
            LevelRef = levelRef;
            MacroRouteId = macroRouteId;
            LocalContentId = ResolveLocalContentId(levelRef, localContentId);
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
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }

        public bool HasLevelRef => LevelRef != null;
        public bool HasLocalContentId => !string.IsNullOrWhiteSpace(LocalContentId);
        public bool IsValid => MacroRouteId.IsValid;

        public static GameplayStartSnapshot Empty => new(
            null,
            SceneRouteId.None,
            string.Empty,
            string.Empty,
            0,
            string.Empty);

        public override string ToString()
        {
            return $"levelRef='{(HasLevelRef ? LevelRef.name : "<none>")}', routeId='{MacroRouteId}', localContentId='{(HasLocalContentId ? LocalContentId : "<none>")}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', levelSignature='{(string.IsNullOrWhiteSpace(LevelSignature) ? "<none>" : LevelSignature)}'";
        }

        private static string ResolveLocalContentId(LevelDefinitionAsset levelRef, string localContentId)
        {
            return LevelFlowContentDefaults.Normalize(localContentId, levelRef);
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
