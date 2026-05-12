using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Adapter tecnico do SessionOperationalPipeline.
    /// Converte o command semantico do SessionOperationalPipeline em request canonico de InputMode.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionOperationalInputModeAdapter : IDisposable
    {
        private readonly EventBinding<SessionOperationalInputModeCommand> _binding;
        private bool _disposed;

        public SessionOperationalInputModeAdapter()
        {
            _binding = new EventBinding<SessionOperationalInputModeCommand>(OnCommandReceived);
            EventBus<SessionOperationalInputModeCommand>.Register(_binding);

            DebugUtility.LogVerbose(typeof(SessionOperationalInputModeAdapter),
                "[OBS][InputModes][Adapter] registered source='SessionOperationalPipeline' target='InputModeRequestEvent'.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SessionOperationalInputModeCommand>.Unregister(_binding);
        }

        private void OnCommandReceived(SessionOperationalInputModeCommand command)
        {
            if (_disposed)
            {
                return;
            }

            if (!command.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalInputModeAdapter),
                    "[FATAL][H1][InputModes] Invalid SessionOperationalInputModeCommand received by SessionOperational adapter.");
                return;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null ||
                !DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var pipeline) || pipeline == null)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalInputModeAdapter),
                    $"[FATAL][H1][InputModes] SessionOperationalPipeline missing for input mode adapter contextSignature='{command.ContextSignature}'.");
                return;
            }

            if (!IsCurrentOperation(command, pipeline))
            {
                DebugUtility.LogVerbose(typeof(SessionOperationalInputModeAdapter),
                    $"[OBS][InputModes][Adapter] rejected reason='stale_or_foreign_event' contextSignature='{command.ContextSignature}' pipelineId='{command.Identity.SessionOperationalPipelineId}' routeOperationId='{command.Identity.RouteOperationId}' transitionId='{command.Identity.TransitionId}' transitionSequence='{command.Identity.TransitionSequence}' routeId='{command.Identity.RouteId}' routeProfileId='{command.Identity.RouteProfileId}' initialInputMode='{command.InitialInputMode}'.",
                    DebugUtility.Colors.Info);
                return;
            }
            PublishInputModeRequest(command);
        }

        private static bool IsCurrentOperation(SessionOperationalInputModeCommand command, SessionOperationalPipeline pipeline)
        {
            SessionOperationalRuntimeState state = pipeline.State;

            return state.HasStarted &&
                   !state.HasCompleted &&
                   string.Equals(state.SessionOperationalPipelineId, command.Identity.SessionOperationalPipelineId, StringComparison.Ordinal) &&
                   string.Equals(state.RouteOperationId, command.Identity.RouteOperationId, StringComparison.Ordinal) &&
                   string.Equals(state.TransitionId, command.Identity.TransitionId, StringComparison.Ordinal) &&
                   state.TransitionSequence == command.Identity.TransitionSequence &&
                   string.Equals(state.RouteId, command.Identity.RouteId, StringComparison.Ordinal) &&
                   string.Equals(state.RouteProfileId, command.Identity.RouteProfileId, StringComparison.Ordinal) &&
                   string.Equals(state.CurrentIdentity.CycleSignature, command.ContextSignature, StringComparison.Ordinal);
        }

        private static void PublishInputModeRequest(SessionOperationalInputModeCommand command)
        {
            InputModeRequestKind kind = MapInputModeKindOrFail(command);

            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(
                    kind,
                    command.Reason,
                    "SessionOperationalPipeline",
                    command.ContextSignature));

            DebugUtility.Log(typeof(SessionOperationalInputModeAdapter),
                $"[OBS][InputModes][Adapter] requested mode='{kind}' source='SessionOperationalPipeline' contextSignature='{command.ContextSignature}' routeClass='{command.RouteClass}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static InputModeRequestKind MapInputModeKindOrFail(SessionOperationalInputModeCommand command)
        {
            return command.InitialInputMode switch
            {
                SessionOperationalInputModeKind.FrontendMenu => InputModeRequestKind.FrontendMenu,
                SessionOperationalInputModeKind.ActivityDefault => InputModeRequestKind.Gameplay,
                _ => throw new InvalidOperationException(
                    $"[FATAL][H1][InputModes] Unsupported SessionOperationalInputModeKind '{command.InitialInputMode}' contextSignature='{command.ContextSignature}'."),
            };
        }
    }
}

