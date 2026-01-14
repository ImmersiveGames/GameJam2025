#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.InputSystems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Passo mínimo de pregame com confirmação via input.
    /// O timeout é opcional e só deve ser habilitado para QA/dev.
    /// </summary>
    public sealed class ConfirmToStartPregameStep : IPregameStep
    {
        private const float DefaultTimeoutSeconds = 10f;
        private const string UiMapName = "UI";
        private const string SubmitActionName = "Submit";
        private const string CancelActionName = "Cancel";

        private readonly float _timeoutSeconds;
        private readonly bool _timeoutEnabled;

        public ConfirmToStartPregameStep(bool enableTimeout = false, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            _timeoutEnabled = enableTimeout;
            _timeoutSeconds = Mathf.Max(0.1f, timeoutSeconds);
        }

        public bool HasContent => true;

        public async Task RunAsync(PregameContext context, CancellationToken cancellationToken)
        {
            var activeScene = NormalizeValue(SceneManager.GetActiveScene().name);
            var profile = context.ProfileId.Value;

            var signature = NormalizeSignature(context.ContextSignature);
            ApplyUiInputMode(signature, activeScene, profile);

            var controlService = ResolvePregameControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<ConfirmToStartPregameStep>(
                    "[Pregame] IPregameControlService indisponível. ConfirmToStart não poderá concluir o Pregame.");
                return;
            }

            var actions = new List<InputAction>();

            void CompleteFromInput(InputAction.CallbackContext _)
                => controlService.CompletePregame("confirm");

            TryBindUiActions(actions, CompleteFromInput);

            try
            {
                if (_timeoutEnabled)
                {
                    _ = TriggerTimeoutAsync(controlService, cancellationToken);
                }

                await controlService.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    actions[i].performed -= CompleteFromInput;
                }
            }
        }

        private static void ApplyUiInputMode(string signature, string sceneName, string profile)
        {
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                DebugUtility.LogWarning<ConfirmToStartPregameStep>(
                    "[Pregame] IInputModeService indisponível. InputMode não será alternado.");
                return;
            }

            DebugUtility.Log<ConfirmToStartPregameStep>(
                $"[OBS][InputMode] Apply mode='FrontendMenu' map='UI' phase='Pregame' reason='Pregame/ConfirmToStart' signature='{signature}' scene='{sceneName}' profile='{profile}'.",
                DebugUtility.Colors.Info);

            inputMode.SetFrontendMenu("Pregame/ConfirmToStart");
        }

        private async Task TriggerTimeoutAsync(IPregameControlService controlService, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), cancellationToken).ConfigureAwait(false);
                controlService.CompletePregame("timeout");
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private static void TryBindUiActions(List<InputAction> actions, Action<InputAction.CallbackContext> handler)
        {
            var inputs = UnityEngine.Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (inputs == null || inputs.Length == 0)
            {
                return;
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                var pi = inputs[i];
                if (pi == null || !pi.enabled || !pi.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var asset = pi.actions;
                if (asset == null)
                {
                    continue;
                }

                var map = asset.FindActionMap(UiMapName, false);
                if (map == null)
                {
                    continue;
                }

                BindActionIfPresent(map, SubmitActionName, actions, handler);
                BindActionIfPresent(map, CancelActionName, actions, handler);
            }
        }

        private static void BindActionIfPresent(
            InputActionMap map,
            string actionName,
            List<InputAction> actions,
            Action<InputAction.CallbackContext> handler)
        {
            var action = map.FindAction(actionName, false);
            if (action == null)
            {
                return;
            }

            action.performed += handler;
            actions.Add(action);
        }

        private static IInputModeService? ResolveInputModeService()
        {
            return DependencyManager.Provider.TryGetGlobal<IInputModeService>(out var service)
                ? service
                : null;
        }

        private static IPregameControlService? ResolvePregameControlService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var service)
                ? service
                : null;
        }

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
