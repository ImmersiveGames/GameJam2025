using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Adapter tecnico do Base11Sandbox.
    /// Converte o command semantico do SessionOperationalPipeline em request canonico de InputMode.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class Base11SandboxSessionOperationalInputModeAdapter : IDisposable
    {
        private readonly EventBinding<SessionOperationalInputModeCommand> _binding;
        private bool _disposed;

        public Base11SandboxSessionOperationalInputModeAdapter()
        {
            _binding = new EventBinding<SessionOperationalInputModeCommand>(OnCommandReceived);
            EventBus<SessionOperationalInputModeCommand>.Register(_binding);

            DebugUtility.LogVerbose(typeof(Base11SandboxSessionOperationalInputModeAdapter),
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
                HardFailFastH1.Trigger(typeof(Base11SandboxSessionOperationalInputModeAdapter),
                    "[FATAL][H1][InputModes] Invalid SessionOperationalInputModeCommand received by Base11Sandbox adapter.");
                return;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null ||
                !DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var pipeline) || pipeline == null)
            {
                HardFailFastH1.Trigger(typeof(Base11SandboxSessionOperationalInputModeAdapter),
                    $"[FATAL][H1][InputModes] SessionOperationalPipeline missing for input mode adapter contextSignature='{command.ContextSignature}'.");
                return;
            }

            if (!IsCurrentOperation(command, pipeline))
            {
                DebugUtility.LogVerbose(typeof(Base11SandboxSessionOperationalInputModeAdapter),
                    $"[OBS][InputModes][Adapter] rejected reason='stale_or_foreign_event' contextSignature='{command.ContextSignature}' pipelineId='{command.Identity.SessionOperationalPipelineId}' routeOperationId='{command.Identity.RouteOperationId}' transitionId='{command.Identity.TransitionId}' transitionSequence='{command.Identity.TransitionSequence}' routeId='{command.Identity.RouteId}' routeProfileId='{command.Identity.RouteProfileId}' initialInputMode='{command.InitialInputMode}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (command.InitialInputMode != SessionOperationalInputModeKind.FrontendMenu)
            {
                DebugUtility.LogVerbose(typeof(Base11SandboxSessionOperationalInputModeAdapter),
                    $"[OBS][InputModes][Adapter] outcome='observed_noop' contextSignature='{command.ContextSignature}' initialInputMode='{command.InitialInputMode}' routeClass='{command.RouteClass}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose(typeof(Base11SandboxSessionOperationalInputModeAdapter),
                $"[OBS][InputModes][Adapter] outcome='deferred_no_runtime_target' contextSignature='{command.ContextSignature}' initialInputMode='{command.InitialInputMode}' routeClass='{command.RouteClass}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            PublishFrontendMenuRequest(command);
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

        private static void PublishFrontendMenuRequest(SessionOperationalInputModeCommand command)
        {
            EventBus<InputModeRequestEvent>.Raise(
                new InputModeRequestEvent(
                    InputModeRequestKind.FrontendMenu,
                    command.Reason,
                    "SessionOperationalPipeline",
                    command.ContextSignature));

            DebugUtility.Log(typeof(Base11SandboxSessionOperationalInputModeAdapter),
                $"[OBS][InputModes][Adapter] requested mode='FrontendMenu' source='SessionOperationalPipeline' contextSignature='{command.ContextSignature}' routeClass='{command.RouteClass}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
