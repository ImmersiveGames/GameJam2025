using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Pipeline;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.SessionActivity.Adapters
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/SessionActivity/Session Activity Pause Toggle Input Adapter")]
    public sealed class SessionActivityPauseToggleInputAdapter : MonoBehaviour
    {
        private const string PlayerActionMapName = "Player";
        private const string UiActionMapName = "UI";
        private const string PauseToggleActionName = "PauseToggle";

        private SessionActivityHost _host;
        private InputActionAsset _actionsAsset;
        private InputAction _playerPauseToggle;
        private InputAction _uiPauseToggle;
        private bool _initialized;
        private int _lastHandledFrame = -1;

        public void Initialize(SessionActivityHost host, InputActionAsset actionsAsset)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (actionsAsset == null)
            {
                throw new ArgumentNullException(nameof(actionsAsset));
            }

            Unsubscribe();

            _host = host;
            _actionsAsset = actionsAsset;
            _playerPauseToggle = ResolvePauseToggleActionOrFail(actionsAsset, PlayerActionMapName);
            _uiPauseToggle = ResolvePauseToggleActionOrFail(actionsAsset, UiActionMapName);

            Subscribe(_playerPauseToggle);
            Subscribe(_uiPauseToggle);

            _initialized = true;
            DebugUtility.Log(typeof(SessionActivityPauseToggleInputAdapter),
                $"event='SessionActivityPauseToggleInputAdapterReady' asset='{actionsAsset.name}' playerAction='{FormatAction(_playerPauseToggle)}' uiAction='{FormatAction(_uiPauseToggle)}' owner='SessionActivityPipeline' source='SessionActivityCompositionInstaller' reason='pause_toggle_input_adapter_ready'.",
                DebugUtility.Colors.Info);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe(InputAction action)
        {
            if (action == null)
            {
                return;
            }

            action.performed += OnPauseTogglePerformed;
            action.Enable();
        }

        private void Unsubscribe()
        {
            Unsubscribe(_playerPauseToggle);
            Unsubscribe(_uiPauseToggle);

            _playerPauseToggle = null;
            _uiPauseToggle = null;
            _actionsAsset = null;
            _initialized = false;
        }

        private void Unsubscribe(InputAction action)
        {
            if (action == null)
            {
                return;
            }

            action.performed -= OnPauseTogglePerformed;
        }

        private void OnPauseTogglePerformed(InputAction.CallbackContext context)
        {
            if (!_initialized || _host == null)
            {
                return;
            }

            if (!_host.HasPipeline || _host.State == null)
            {
                DebugUtility.LogVerbose(typeof(SessionActivityPauseToggleInputAdapter),
                    $"event='SessionActivityPauseToggleInputIgnored' reason='session_activity_pipeline_unavailable' action='{FormatAction(context.action)}' frame='{Time.frameCount}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_lastHandledFrame == Time.frameCount)
            {
                DebugUtility.LogVerbose(typeof(SessionActivityPauseToggleInputAdapter),
                    $"event='SessionActivityPauseToggleInputIgnored' reason='dedupe_same_frame' action='{FormatAction(context.action)}' frame='{Time.frameCount}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastHandledFrame = Time.frameCount;
            string actionPath = FormatAction(context.action);

            DebugUtility.LogVerbose(typeof(SessionActivityPauseToggleInputAdapter),
                $"event='SessionActivityPauseToggleInputPerformed' action='{actionPath}' source='SessionActivityPauseToggleInputAdapter' reason='input_action_pause_toggle'.",
                DebugUtility.Colors.Info);

            _host.RequestPauseToggle(
                "SessionActivityPauseToggleInputAdapter",
                $"input_action:{actionPath}");
        }

        private static InputAction ResolvePauseToggleActionOrFail(InputActionAsset actionsAsset, string actionMapName)
        {
            var actionMap = actionsAsset.FindActionMap(actionMapName, false);
            if (actionMap == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityPipeline] PauseToggle action map ausente. asset='{actionsAsset.name}' actionMap='{actionMapName}'.");
            }

            var action = actionMap.FindAction(PauseToggleActionName, false);
            if (action == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionActivityPipeline] PauseToggle action ausente. asset='{actionsAsset.name}' actionMap='{actionMapName}' action='{PauseToggleActionName}'.");
            }

            return action;
        }

        private static string FormatAction(InputAction action)
        {
            if (action == null)
            {
                return "<none>";
            }

            string mapName = action.actionMap != null ? action.actionMap.name.TrimToEmpty() : string.Empty;
            string actionName = action.name.TrimToEmpty();

            if (string.IsNullOrWhiteSpace(mapName))
            {
                return actionName;
            }

            return $"{mapName}/{actionName}";
        }
    }
}
