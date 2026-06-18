using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public interface IOperationalRouteAudioPort
    {
        OperationalRouteAudioResult SubmitRouteRevealAudio(OperationalRouteAudioRequest request);
    }

    public readonly struct OperationalRouteAudioRequest
    {
        public OperationalRouteAudioRequest(SessionOperationalRouteCommand routeCommand, string source, string reason)
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

    public enum OperationalRouteAudioResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalRouteAudioResult
    {
        public OperationalRouteAudioResult(
            OperationalRouteAudioResultKind kind,
            string cueType,
            string cueName,
            string reason,
            string detail)
        {
            Kind = kind;
            CueType = cueType.TrimToEmpty();
            CueName = cueName.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalRouteAudioResultKind Kind { get; }
        public string CueType { get; }
        public string CueName { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalRouteAudioResultKind.Completed;
        public bool IsSkipped => Kind == OperationalRouteAudioResultKind.Skipped;

        public static OperationalRouteAudioResult Completed(string cueType, string cueName)
        {
            return new OperationalRouteAudioResult(
                OperationalRouteAudioResultKind.Completed,
                cueType,
                cueName,
                "submitted",
                string.Empty);
        }

        public static OperationalRouteAudioResult Skipped(string reason, string detail)
        {
            return new OperationalRouteAudioResult(
                OperationalRouteAudioResultKind.Skipped,
                string.Empty,
                string.Empty,
                reason,
                detail);
        }
}
}
