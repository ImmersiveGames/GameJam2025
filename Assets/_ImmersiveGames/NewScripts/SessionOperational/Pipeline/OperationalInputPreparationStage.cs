using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalInputPreparationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalInputPreparationResult
    {
        public OperationalInputPreparationResult(
            OperationalInputPreparationResultKind kind,
            SessionOperationalInputModeKind initialInputMode)
        {
            Kind = kind;
            InitialInputMode = initialInputMode;
        }

        public OperationalInputPreparationResultKind Kind { get; }
        public SessionOperationalInputModeKind InitialInputMode { get; }
        public bool IsCompleted => Kind == OperationalInputPreparationResultKind.Completed;
    }

    public readonly struct OperationalInputPreparationCommand
    {
        public OperationalInputPreparationCommand(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRoutePlan routePlan,
            SessionOperationalInputPolicy inputPolicy,
            string routeClass,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            Func<SessionOperationalInputModeKind, bool> tryObserveInputCapabilityPrepared,
            Func<SessionOperationalInputModeKind, bool> tryObserveInitialInputModePrepared,
            Action<SessionOperationalInputModeKind> dispatchInitialInputModeCommandOrFail)
        {
            RuntimeModeConfig = runtimeModeConfig;
            RoutePlan = routePlan;
            InputPolicy = inputPolicy;
            RouteClass = Normalize(routeClass);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
            TryObserveInputCapabilityPrepared = tryObserveInputCapabilityPrepared;
            TryObserveInitialInputModePrepared = tryObserveInitialInputModePrepared;
            DispatchInitialInputModeCommandOrFail = dispatchInitialInputModeCommandOrFail;
        }

        public RuntimeModeConfig RuntimeModeConfig { get; }
        public SessionOperationalRoutePlan RoutePlan { get; }
        public SessionOperationalInputPolicy InputPolicy { get; }
        public string RouteClass { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }
        public Func<SessionOperationalInputModeKind, bool> TryObserveInputCapabilityPrepared { get; }
        public Func<SessionOperationalInputModeKind, bool> TryObserveInitialInputModePrepared { get; }
        public Action<SessionOperationalInputModeKind> DispatchInitialInputModeCommandOrFail { get; }

        public bool IsValid =>
            RuntimeModeConfig != null &&
            RoutePlan.IsValid &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason) &&
            TryObserveInputCapabilityPrepared != null &&
            TryObserveInitialInputModePrepared != null &&
            DispatchInitialInputModeCommandOrFail != null;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalInputPreparationStage
    {
        public OperationalInputPreparationResult Execute(OperationalInputPreparationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalInputPreparationCommand is invalid.");
            }

            SessionOperationalInputModeKind initialInputMode = ResolveInitialInputModeFromPolicyOrFail(command);

            UnityOperationalInputRuntimeAdapter.PrepareOrFail(
                command.RuntimeModeConfig,
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.Source,
                command.Reason);

            LogInputCapabilityPrepared(command, initialInputMode);

            if (!command.TryObserveInputCapabilityPrepared(initialInputMode))
            {
                throw new InvalidOperationException(BuildMissingInputCapabilityPreparedMessage(command, initialInputMode));
            }

            if (!command.TryObserveInitialInputModePrepared(initialInputMode))
            {
                throw new InvalidOperationException(BuildMissingInitialInputModePreparedMessage(command, initialInputMode));
            }

            command.DispatchInitialInputModeCommandOrFail(initialInputMode);

            return new OperationalInputPreparationResult(
                OperationalInputPreparationResultKind.Completed,
                initialInputMode);
        }

        private static SessionOperationalInputModeKind ResolveInitialInputModeFromPolicyOrFail(OperationalInputPreparationCommand command)
        {
            return command.InputPolicy switch
            {
                SessionOperationalInputPolicy.MenuNavigation => SessionOperationalInputModeKind.FrontendMenu,
                SessionOperationalInputPolicy.ActivityGameplay => SessionOperationalInputModeKind.ActivityDefault,
                SessionOperationalInputPolicy.OverlayNavigation => SessionOperationalInputModeKind.PauseOverlay,
                SessionOperationalInputPolicy.InputLocked => SessionOperationalInputModeKind.InputLocked,
                _ => throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalInputCapability] inputPolicy invalida routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' inputPolicy='{command.InputPolicy}' source='{command.Source}' reason='{command.Reason}'."),
            };
        }

        private static void LogInputCapabilityPrepared(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            DebugUtility.Log(typeof(OperationalInputPreparationStage),
                $"[OBS][SessionOperationalPipeline][InputCapability] InputCapabilityPrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{command.RoutePlan.OperationalSurfaceKind}' inputPolicy='{command.InputPolicy}' inputMode='{initialInputMode}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string BuildMissingInputCapabilityPreparedMessage(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            return $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InputCapabilityPrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{command.RoutePlan.OperationalSurfaceKind}' inputPolicy='{command.InputPolicy}' resolvedInputMode='{initialInputMode}' source='{command.Source}' reason='{command.Reason}'.";
        }

        private static string BuildMissingInitialInputModePreparedMessage(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            return $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InitialInputModePrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{command.RoutePlan.OperationalSurfaceKind}' inputPolicy='{command.InputPolicy}' resolvedInputMode='{initialInputMode}' source='{command.Source}' reason='{command.Reason}'.";
        }
    }
}
