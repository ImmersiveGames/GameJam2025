using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    public sealed class InputModeAdapter : ISessionActivityInputModeAdapter
    {
        public SessionActivityInputModeObservation Apply(SessionActivityInputModeCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("[FATAL][PauseSurface] InputModeAdapter received an invalid SessionActivityInputModeCommand.");
            }

            var context = command.RoutePauseSurfaceContext;
            EnsureCanonicalInputModeTrailOrFail(command, context);

            var requestKind = MapInputModeKindOrFail(command.Kind);
            string contextSignature = BuildContextSignature(command, context);
            string reason = command.Reason.TrimToOrDefault(command.Kind.ToString());

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(
                    requestKind,
                    reason,
                    "SessionActivityPipeline",
                    contextSignature,
                    context.RouteIdentity,
                    context.RouteOperationId,
                    context.TransitionId,
                    context.RouteSequence,
                    command.Kind.ToString()));

            SessionActivityInputModeObservation observation = new(
                command,
                "InputModeApplied",
                "input_mode_applied",
                "delegated");

            if (!observation.IsValid)
            {
                throw new InvalidOperationException("InputModeAdapter produced an invalid observation.");
            }

            DebugUtility.Log(typeof(InputModeAdapter),
                $"event='SessionActivityInputModeRequestSubmitted' command='ApplyActivityInputMode' mode='{command.Kind}' inputMode='{requestKind}' routeIdentity='{context.RouteIdentity}' routeOperationId='{context.RouteOperationId}' transitionId='{context.TransitionId}' routeSequence='{context.RouteSequence}' source='SessionActivityPipeline' requestSource='{command.Source}' reason='{reason}' contextSignature='{contextSignature}' outcomeKind='delegated'.",
                DebugUtility.Colors.Info);

            DebugUtility.LogVerbose(typeof(InputModeAdapter),
                $"fact='{observation.Fact}' snapshot='{observation.Snapshot}' command='ApplyActivityInputMode' mode='{command.Kind}' inputMode='{requestKind}' reason='{reason}' outcomeKind='{observation.Outcome}' identity='{command.Identity}' source='{command.Source}' contextSignature='{contextSignature}'.",
                DebugUtility.Colors.Info);

            return observation;
        }

        private static void EnsureCanonicalInputModeTrailOrFail(
            SessionActivityInputModeCommand command,
            SessionActivityRoutePauseSurfaceContext context)
        {
            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][InputModes] DependencyManager indisponivel para aplicar SessionActivity input mode mode='{command.Kind}' surfaceId='{context.SurfaceId}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service) || service == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][InputModes] IInputModeService obrigatorio ausente para aplicar SessionActivity input mode mode='{command.Kind}' surfaceId='{context.SurfaceId}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<InputModeCoordinator>(out var coordinator) || coordinator == null)
            {
                throw new InvalidOperationException(
                    $"[FATAL][InputModes] InputModeCoordinator obrigatorio ausente para aplicar SessionActivity input mode mode='{command.Kind}' surfaceId='{context.SurfaceId}'.");
            }
        }

        private static InputModeRequestKind MapInputModeKindOrFail(SessionActivityInputModeKind kind)
        {
            return kind switch
            {
                SessionActivityInputModeKind.ActivityGameplay => InputModeRequestKind.Gameplay,
                SessionActivityInputModeKind.PauseOverlay => InputModeRequestKind.PauseOverlay,
                SessionActivityInputModeKind.Disabled => InputModeRequestKind.InputLocked,
                _ => throw new InvalidOperationException($"[FATAL][InputModes] Unsupported SessionActivityInputModeKind '{kind}'.")
            };
        }

        private static string BuildContextSignature(
            SessionActivityInputModeCommand command,
            SessionActivityRoutePauseSurfaceContext context)
        {
            return $"SessionActivityPipeline.v0|{context.RouteIdentity}|{context.RouteOperationId}|{context.TransitionId}|{context.RouteSequence}|{command.Identity.SessionId}|{command.Identity.ActivityId}|{command.Identity.EntrySequence}|{command.Kind}";
        }
    }
}
