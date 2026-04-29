#nullable enable
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
namespace _ImmersiveGames.NewScripts.SessionFlow.Host.IntroStage.PresenterExecution
{
    public enum GameplayStartReadyIntroStageStatus
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        NoContent = 3
    }

    public enum GameplayStartReadyReasonKind
    {
        Unknown = 0,
        WaitingForIntroStageDone = 1,
        WaitingForActorsOperationalReady = 2,
        ContextMismatch = 3,
        Ready = 4
    }

    public readonly struct GameplayStartReadySnapshot
    {
        public GameplayStartReadySnapshot(
            string sessionSignature,
            SceneRouteId routeId,
            SceneRouteKind routeKind,
            string sceneName,
            string actorSetRef,
            string cycleSignature,
            string phaseSignature,
            string participationSignature,
            GameplayStartReadyIntroStageStatus introStageStatus,
            bool actorsOperationalReady,
            GameplayStartReadyReasonKind readinessReasonKind,
            string readinessReason)
        {
            SessionSignature = Normalize(sessionSignature);
            RouteId = routeId;
            RouteKind = routeKind;
            SceneName = Normalize(sceneName);
            ActorSetRef = Normalize(actorSetRef);
            CycleSignature = Normalize(cycleSignature);
            PhaseSignature = Normalize(phaseSignature);
            ParticipationSignature = Normalize(participationSignature);
            IntroStageStatus = introStageStatus;
            ActorsOperationalReady = actorsOperationalReady;
            ReadinessReasonKind = readinessReasonKind;
            ReadinessReason = NormalizeReason(readinessReason, readinessReasonKind);
        }

        public string SessionSignature { get; }
        public SceneRouteId RouteId { get; }
        public SceneRouteKind RouteKind { get; }
        public string SceneName { get; }
        public string ActorSetRef { get; }
        public string CycleSignature { get; }
        public string PhaseSignature { get; }
        public string PhaseRuntimeSignature => PhaseSignature;
        public string ParticipationSignature { get; }
        public GameplayStartReadyIntroStageStatus IntroStageStatus { get; }
        public bool ActorsOperationalReady { get; }
        public GameplayStartReadyReasonKind ReadinessReasonKind { get; }
        public string ReadinessReason { get; }

        public bool HasCanonicalPayload =>
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            RouteId.IsValid &&
            RouteKind != SceneRouteKind.Unspecified &&
            !string.IsNullOrWhiteSpace(SceneName) &&
            !string.IsNullOrWhiteSpace(ActorSetRef) &&
            !string.IsNullOrWhiteSpace(CycleSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            !string.IsNullOrWhiteSpace(ParticipationSignature);

        public bool IsReady =>
            HasCanonicalPayload &&
            RouteKind == SceneRouteKind.Gameplay &&
            ActorsOperationalReady &&
            IntroStageStatus != GameplayStartReadyIntroStageStatus.Unknown &&
            ReadinessReasonKind == GameplayStartReadyReasonKind.Ready;

        public static GameplayStartReadySnapshot Empty =>
            new(
                string.Empty,
                default,
                SceneRouteKind.Unspecified,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                GameplayStartReadyIntroStageStatus.Unknown,
                false,
                GameplayStartReadyReasonKind.Unknown,
                "waiting_for_gameplay_start_ready");

        private static string Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private static string NormalizeReason(string? value, GameplayStartReadyReasonKind reasonKind)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            return reasonKind switch
            {
                GameplayStartReadyReasonKind.Ready => "IntroStageDone+ActorsOperationalReady",
                GameplayStartReadyReasonKind.WaitingForIntroStageDone => "waiting_for_intro_stage",
                GameplayStartReadyReasonKind.WaitingForActorsOperationalReady => "waiting_for_actors_operational_ready",
                GameplayStartReadyReasonKind.ContextMismatch => "context_mismatch",
                _ => "waiting_for_gameplay_start_ready"
            };
        }
    }
}
