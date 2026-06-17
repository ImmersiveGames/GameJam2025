using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Coordinator canonico do trilho de requests de InputMode.
    /// Ele e o unico writer do IInputModeService no runtime canonico e opera apenas requests ja canonizados.
    /// </summary>
    public sealed class InputModeCoordinator : IDisposable
    {
        private readonly EventBinding<InputModeRequestEvent> _requestBinding;

        public InputModeCoordinator()
        {
            _requestBinding = new EventBinding<InputModeRequestEvent>(OnInputModeRequested);
            EventBus<InputModeRequestEvent>.Register(_requestBinding);
        }

        public void Dispose()
        {
            EventBus<InputModeRequestEvent>.Unregister(_requestBinding);
        }

        private void OnInputModeRequested(InputModeRequestEvent evt)
        {
            string requestKey = BuildRequestKey(evt);
            string contextSignature = string.IsNullOrWhiteSpace(evt.ContextSignature) ? "<none>" : evt.ContextSignature;

            DebugUtility.LogVerbose(typeof(InputModeCoordinator),
                $"InputModeRequested routeIdentity='{evt.RouteIdentity}' routeOperationId='{evt.RouteOperationId}' transitionId='{evt.TransitionId}' routeSequence='{evt.RouteSequence}' initialInputMode='{evt.InitialInputMode}' inputMode='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}'",
                DebugUtility.Colors.Info);

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null ||
                !DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(InputModeCoordinator),
                    $"[FATAL][H1][InputModes] Canonical trail broken: IInputModeService missing key='{requestKey}' contextSignature='{contextSignature}'.");
                return;
            }

            ApplyRequest(service, evt, requestKey, contextSignature);
        }

        private static void ApplyRequest(IInputModeService service, InputModeRequestEvent evt, string requestKey, string contextSignature)
        {
            switch (evt.Kind)
            {
                case InputModeRequestKind.FrontendMenu:
                    service.SetFrontendMenu(evt.Reason);
                    DebugUtility.Log(typeof(InputModeCoordinator),
                        $"InputModeRequestDelegated routeIdentity='{evt.RouteIdentity}' routeOperationId='{evt.RouteOperationId}' transitionId='{evt.TransitionId}' routeSequence='{evt.RouteSequence}' initialInputMode='{evt.InitialInputMode}' inputMode='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}' outcomeKind='delegated'.",
                        DebugUtility.Colors.Info);
                    return;
                case InputModeRequestKind.Gameplay:
                    service.SetGameplay(evt.Reason);
                    DebugUtility.Log(typeof(InputModeCoordinator),
                        $"InputModeRequestDelegated routeIdentity='{evt.RouteIdentity}' routeOperationId='{evt.RouteOperationId}' transitionId='{evt.TransitionId}' routeSequence='{evt.RouteSequence}' initialInputMode='{evt.InitialInputMode}' inputMode='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}' outcomeKind='delegated'.",
                        DebugUtility.Colors.Info);
                    return;
                case InputModeRequestKind.PauseOverlay:
                    service.SetPauseOverlay(evt.Reason);
                    DebugUtility.Log(typeof(InputModeCoordinator),
                        $"InputModeRequestDelegated routeIdentity='{evt.RouteIdentity}' routeOperationId='{evt.RouteOperationId}' transitionId='{evt.TransitionId}' routeSequence='{evt.RouteSequence}' initialInputMode='{evt.InitialInputMode}' inputMode='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}' outcomeKind='delegated'.",
                        DebugUtility.Colors.Info);
                    return;
                case InputModeRequestKind.InputLocked:
                    service.SetInputLocked(evt.Reason);
                    DebugUtility.Log(typeof(InputModeCoordinator),
                        $"InputModeRequestDelegated routeIdentity='{evt.RouteIdentity}' routeOperationId='{evt.RouteOperationId}' transitionId='{evt.TransitionId}' routeSequence='{evt.RouteSequence}' initialInputMode='{evt.InitialInputMode}' inputMode='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}' outcomeKind='delegated'.",
                        DebugUtility.Colors.Info);
                    return;
                case InputModeRequestKind.Unspecified:
                default:
                    HardFailFastH1.Trigger(typeof(InputModeCoordinator),
                        $"[FATAL][H1][InputModes] Unsupported InputModeRequestKind '{evt.Kind}' key='{requestKey}'.");
                    return;
            }
        }

        private static string BuildRequestKey(InputModeRequestEvent evt)
        {
            string kind = evt.Kind.ToString();
            string source = string.IsNullOrWhiteSpace(evt.Source) ? "<none>" : evt.Source.Trim();
            string reason = string.IsNullOrWhiteSpace(evt.Reason) ? "<none>" : evt.Reason.Trim();
            string contextSignature = string.IsNullOrWhiteSpace(evt.ContextSignature) ? "<none>" : evt.ContextSignature.Trim();
            return $"{kind}|{source}|{reason}|{contextSignature}";
        }
    }
}

