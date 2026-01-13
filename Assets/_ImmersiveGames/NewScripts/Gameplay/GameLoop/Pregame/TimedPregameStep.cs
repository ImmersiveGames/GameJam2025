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
    /// Passo mínimo de pregame com timeout curto para evidência de Teste 3.
    /// </summary>
    public sealed class TimedPregameStep : IPregameStep
    {
        private const float DefaultTimeoutSeconds = 0.5f;
        private const string UiMapName = "UI";
        private const string SubmitActionName = "Submit";
        private const string CancelActionName = "Cancel";

        private readonly float _timeoutSeconds;

        public TimedPregameStep(float timeoutSeconds = DefaultTimeoutSeconds)
        {
            _timeoutSeconds = Mathf.Max(0.1f, timeoutSeconds);
        }

        public bool HasContent => true;

        public async Task RunAsync(PregameContext context, CancellationToken cancellationToken)
        {
            var signature = NormalizeSignature(context.ContextSignature);
            var reason = NormalizeValue(context.Reason);
            var targetScene = NormalizeValue(context.TargetScene);
            var activeScene = NormalizeValue(SceneManager.GetActiveScene().name);
            var profile = context.ProfileId.Value;

            DebugUtility.Log<TimedPregameStep>(
                $"[OBS][Pregame] PregameStarted signature='{signature}' reason='{reason}' target='{targetScene}' scene='{activeScene}' profile='{profile}'.",
                DebugUtility.Colors.Info);

            ApplyUiInputMode(signature, activeScene, profile);

            var completedReason = await AwaitCompletionAsync(cancellationToken).ConfigureAwait(false);

            DebugUtility.Log<TimedPregameStep>(
                $"[OBS][Pregame] PregameCompleted signature='{signature}' result='{completedReason}' target='{targetScene}' scene='{activeScene}' profile='{profile}'.",
                DebugUtility.Colors.Info);
        }

        private static void ApplyUiInputMode(string signature, string sceneName, string profile)
        {
            var inputMode = ResolveInputModeService();
            if (inputMode == null)
            {
                DebugUtility.LogWarning<TimedPregameStep>(
                    "[Pregame] IInputModeService indisponível. InputMode não será alternado.");
                return;
            }

            DebugUtility.Log<TimedPregameStep>(
                $"[OBS][InputMode] Apply mode='FrontendMenu' map='UI' phase='Pregame' reason='Pregame/TimedStep' signature='{signature}' scene='{sceneName}' profile='{profile}'.",
                DebugUtility.Colors.Info);

            inputMode.SetFrontendMenu("Pregame/TimedStep");
        }

        private async Task<string> AwaitCompletionAsync(CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var actions = new List<InputAction>();

            void CompleteFromInput(InputAction.CallbackContext _) => completionSource.TrySetResult(true);

            TryBindUiActions(actions, CompleteFromInput);

            try
            {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(_timeoutSeconds), cancellationToken);
                var completed = await Task.WhenAny(completionSource.Task, delayTask).ConfigureAwait(false);

                if (completed == completionSource.Task)
                {
                    return "input";
                }

                if (delayTask.IsCanceled)
                {
                    return "cancelled";
                }

                return "timeout";
            }
            catch (OperationCanceledException)
            {
                return "cancelled";
            }
            finally
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    actions[i].performed -= CompleteFromInput;
                }
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

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
    }
}
