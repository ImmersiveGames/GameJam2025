using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum OperationalInputModeRequestResultKind
    {
        Unknown = 0,
        Submitted = 1,
        Failed = 2,
    }

    public readonly struct OperationalInputModeRequest
    {
        public OperationalInputModeRequest(
            SessionOperationalIdentity identity,
            SessionOperationalInputModeKind initialInputMode,
            SessionOperationalInputPolicy inputPolicy,
            string routeClass)
        {
            Identity = identity;
            InitialInputMode = initialInputMode;
            InputPolicy = inputPolicy;
            RouteClass = routeClass.TrimToEmpty();
        }

        public SessionOperationalIdentity Identity { get; }
        public SessionOperationalInputModeKind InitialInputMode { get; }
        public SessionOperationalInputPolicy InputPolicy { get; }
        public string RouteClass { get; }
        public string ContextSignature => Identity.CycleSignature;
        public string Source => Identity.Source;
        public string Reason => Identity.Reason;

        public bool IsValid =>
            Identity.IsValid &&
            InitialInputMode != SessionOperationalInputModeKind.Unknown &&
            InputPolicy != SessionOperationalInputPolicy.Unknown &&
            !string.IsNullOrWhiteSpace(RouteClass) &&
            !string.IsNullOrWhiteSpace(ContextSignature);
}

    public readonly struct OperationalInputModeRequestResult
    {
        public OperationalInputModeRequestResult(
            OperationalInputModeRequestResultKind kind,
            string reason,
            string detail)
        {
            Kind = kind;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalInputModeRequestResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsSubmitted => Kind == OperationalInputModeRequestResultKind.Submitted;

        public static OperationalInputModeRequestResult Submitted(string reason) =>
            new(OperationalInputModeRequestResultKind.Submitted, reason, string.Empty);

        public static OperationalInputModeRequestResult Failed(string reason, string detail) =>
            new(OperationalInputModeRequestResultKind.Failed, reason, detail);
}

    public interface IOperationalInputModeRequestPort
    {
        OperationalInputModeRequestResult SubmitInitialInputMode(OperationalInputModeRequest request);
    }
}
