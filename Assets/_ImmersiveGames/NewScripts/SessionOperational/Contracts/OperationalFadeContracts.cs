using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum OperationalFadeDirection
    {
        Unknown = 0,
        CloseCurtain = 1,
        OpenCurtain = 2,
    }

    public enum OperationalFadeResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public interface IOperationalFadePort
    {
        Task<OperationalFadeResult> ExecuteAsync(OperationalFadeRequest request);
    }

    public readonly struct OperationalFadeRequest
    {
        public OperationalFadeRequest(
            SessionOperationalRouteCommand routeCommand,
            OperationalFadeDirection direction,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            Direction = direction;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public OperationalFadeDirection Direction { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RouteCommand.IsValid && Direction != OperationalFadeDirection.Unknown;
}

    public readonly struct OperationalFadeResult
    {
        public OperationalFadeResult(
            OperationalFadeResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            OperationalFadeDirection direction,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            Direction = direction;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalFadeResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public OperationalFadeDirection Direction { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalFadeResultKind.Completed;
        public bool IsSkipped => Kind == OperationalFadeResultKind.Skipped;
        public bool IsAccepted => IsCompleted || IsSkipped;

        public static OperationalFadeResult Completed(SessionOperationalRouteCommand command, OperationalFadeDirection direction, string reason, string detail)
        {
            return new OperationalFadeResult(
                OperationalFadeResultKind.Completed,
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                direction,
                reason,
                detail);
        }

        public static OperationalFadeResult Skipped(SessionOperationalRouteCommand command, OperationalFadeDirection direction, string reason, string detail)
        {
            return new OperationalFadeResult(
                OperationalFadeResultKind.Skipped,
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                direction,
                reason,
                detail);
        }
}
}
