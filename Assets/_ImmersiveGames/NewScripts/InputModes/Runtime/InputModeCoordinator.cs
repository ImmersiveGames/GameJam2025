using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.InputModes.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Coordinator canonico do trilho de requests de InputMode.
    /// Ele e o unico writer do IInputModeService no runtime canonico e opera apenas requests ja canonizados.
    /// </summary>
    public sealed class InputModeCoordinator : IDisposable
    {
        private readonly EventBinding<InputModeRequestEvent> _requestBinding;
        private int _lastRequestFrame = -1;
        private string _lastRequestKey = string.Empty;
        private bool _missingInputModeServiceWarned;

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

            DebugUtility.Log(typeof(InputModeCoordinator),
                $"[OBS][InputModes] InputModeRequested kind='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}'",
                DebugUtility.Colors.Info);

            if (Time.frameCount == _lastRequestFrame && string.Equals(_lastRequestKey, requestKey, StringComparison.Ordinal))
            {
                DebugUtility.Log(typeof(InputModeCoordinator),
                    $"[OBS][InputModes] InputModeRequestDeduped reason='same_frame' key='{requestKey}' contextSignature='{contextSignature}'",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null ||
                !DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service) || service == null)
            {
                if (!_missingInputModeServiceWarned)
                {
                    _missingInputModeServiceWarned = true;
                    DebugUtility.LogWarning(typeof(InputModeCoordinator),
                        $"[WARN][InputModes] Request ignored; IInputModeService missing key='{requestKey}' contextSignature='{contextSignature}'.");
                }

                return;
            }

            _missingInputModeServiceWarned = false;
            ApplyRequest(service, evt, requestKey, contextSignature);
            _lastRequestFrame = Time.frameCount;
            _lastRequestKey = requestKey;
        }

        private static void ApplyRequest(IInputModeService service, InputModeRequestEvent evt, string requestKey, string contextSignature)
        {
            switch (evt.Kind)
            {
                case InputModeRequestKind.FrontendMenu:
                    service.SetFrontendMenu(evt.Reason);
                    break;
                case InputModeRequestKind.Gameplay:
                    service.SetGameplay(evt.Reason);
                    break;
                case InputModeRequestKind.PauseOverlay:
                    service.SetPauseOverlay(evt.Reason);
                    break;
                case InputModeRequestKind.Unspecified:
                default:
                    HardFailFastH1.Trigger(typeof(InputModeCoordinator),
                        $"[FATAL][H1][InputModes] Unsupported InputModeRequestKind '{evt.Kind}' key='{requestKey}'.");
                    return;
            }

            DebugUtility.Log(typeof(InputModeCoordinator),
                $"[OBS][InputModes] InputModeApplied kind='{evt.Kind}' source='{evt.Source}' reason='{evt.Reason}' contextSignature='{contextSignature}'",
                DebugUtility.Colors.Info);
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

