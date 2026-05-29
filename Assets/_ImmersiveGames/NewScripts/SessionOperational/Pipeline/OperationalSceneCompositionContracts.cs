using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
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
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid => RouteCommand.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
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
            Reason = Normalize(reason);
            Detail = Normalize(detail);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
