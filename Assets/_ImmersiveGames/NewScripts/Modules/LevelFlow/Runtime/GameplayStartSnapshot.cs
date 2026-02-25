using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime
{
    /// <summary>
    /// Snapshot canônico do último start de gameplay.
    /// Serve como base para restart sem depender de lookup reverso Route->Level.
    /// </summary>
    public readonly struct GameplayStartSnapshot
    {
        public GameplayStartSnapshot(
            LevelId levelId,
            SceneRouteId routeId,
            TransitionStyleId styleId,
            string contentId,
            string reason,
            int selectionVersion,
            string levelSignature)
        {
            LevelId = levelId;
            RouteId = routeId;
            StyleId = styleId;
            ContentId = Sanitize(contentId);
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            LevelSignature = Sanitize(levelSignature);
        }

        public LevelId LevelId { get; }
        public SceneRouteId RouteId { get; }
        public TransitionStyleId StyleId { get; }
        public string ContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string LevelSignature { get; }

        // Compatibilidade temporária: manter API antiga sem alterar comportamento.
        // LevelSignature != MacroSignature (macro virá em etapa futura separada).
        [System.Obsolete("Use LevelSignature. ContextSignature no snapshot representa assinatura de nível por compatibilidade.")]
        public string ContextSignature => LevelSignature;

        public bool HasLevelId => LevelId.IsValid;
        public bool HasContentId => !string.IsNullOrWhiteSpace(ContentId);
        public bool IsValid => RouteId.IsValid;

        public static GameplayStartSnapshot Empty => new(
            LevelId.None,
            SceneRouteId.None,
            TransitionStyleId.None,
            string.Empty,
            string.Empty,
            0,
            string.Empty);

        public override string ToString()
        {
            return $"levelId='{(HasLevelId ? LevelId.ToString() : "<none>")}', routeId='{RouteId}', styleId='{StyleId}', contentId='{(HasContentId ? ContentId : "<none>")}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', levelSignature='{(string.IsNullOrWhiteSpace(LevelSignature) ? "<none>" : LevelSignature)}'";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
