using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Runtime
{
    public enum ActorPresentationPlanResolutionResultKind
    {
        Unknown = 0,
        Success = 1,
        SkippedOptional = 2,
        Failed = 3
    }

    public readonly struct ActorPresentationPlanResolutionResult
    {
        private ActorPresentationPlanResolutionResult(
            ActorPresentationPlanResolutionResultKind kind,
            ActorPresentationResolvedPlan resolvedPlan,
            string reasonCode,
            string message)
        {
            Kind = kind;
            ResolvedPlan = resolvedPlan;
            ReasonCode = reasonCode.TrimToEmpty();
            Message = message.TrimToEmpty();
        }

        public ActorPresentationPlanResolutionResultKind Kind { get; }
        public ActorPresentationResolvedPlan ResolvedPlan { get; }
        public string ReasonCode { get; }
        public string Message { get; }

        public bool IsSuccess => Kind == ActorPresentationPlanResolutionResultKind.Success;
        public bool IsSkippedOptional => Kind == ActorPresentationPlanResolutionResultKind.SkippedOptional;
        public bool IsFailed => Kind == ActorPresentationPlanResolutionResultKind.Failed;

        public bool IsValid =>
            Kind != ActorPresentationPlanResolutionResultKind.Unknown &&
            (Kind == ActorPresentationPlanResolutionResultKind.Success ? ResolvedPlan.IsValid : true) &&
            (Kind != ActorPresentationPlanResolutionResultKind.Success ? !string.IsNullOrWhiteSpace(ReasonCode) : true);

        public static ActorPresentationPlanResolutionResult Success(
            ActorPresentationResolvedPlan resolvedPlan,
            string message)
        {
            return new ActorPresentationPlanResolutionResult(
                ActorPresentationPlanResolutionResultKind.Success,
                resolvedPlan,
                reasonCode: string.Empty,
                message: message);
        }

        public static ActorPresentationPlanResolutionResult SkippedOptional(
            string reasonCode,
            string message)
        {
            return new ActorPresentationPlanResolutionResult(
                ActorPresentationPlanResolutionResultKind.SkippedOptional,
                default,
                reasonCode,
                message);
        }

        public static ActorPresentationPlanResolutionResult Failed(
            string reasonCode,
            string message)
        {
            return new ActorPresentationPlanResolutionResult(
                ActorPresentationPlanResolutionResultKind.Failed,
                default,
                reasonCode,
                message);
        }
}
}
