using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public interface IOperationalSceneCompositionPort
    {
        System.Threading.Tasks.Task<OperationalSceneCompositionResult> ApplyAsync(OperationalSceneCompositionRequest request);
    }

    public readonly struct OperationalSceneCompositionRequest
    {
        public OperationalSceneCompositionRequest(
            SessionOperationalRouteCommand routeCommand,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteCommand.IsValid;
}

    public enum OperationalSceneCompositionResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalSceneCompositionResult
    {
        public OperationalSceneCompositionResult(
            OperationalSceneCompositionResultKind kind,
            SessionOperationalRouteCompletedFact completionFact,
            string reason,
            string detail)
        {
            Kind = kind;
            CompletionFact = completionFact;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalSceneCompositionResultKind Kind { get; }
        public SessionOperationalRouteCompletedFact CompletionFact { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalSceneCompositionResultKind.Completed && CompletionFact.IsValid;

        public static OperationalSceneCompositionResult Completed(SessionOperationalRouteCompletedFact completionFact, string reason, string detail)
        {
            return new OperationalSceneCompositionResult(
                OperationalSceneCompositionResultKind.Completed,
                completionFact,
                reason,
                detail);
        }

        public static OperationalSceneCompositionResult Failed(string reason, string detail)
        {
            return new OperationalSceneCompositionResult(
                OperationalSceneCompositionResultKind.Failed,
                default,
                reason,
                detail);
        }
}
}
