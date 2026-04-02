#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    // Presenter visual minimo e scene-local do hook de pos-run em nivel.
    public sealed class LevelPostRunHookPresenter : MonoBehaviour, ILevelPostRunHookPresenter
    {
        private const float PanelWidth = 640f;
        private const float PanelHeight = 320f;

        [SerializeField] private Vector2 panelOffset = new Vector2(24f, 180f);

        private LevelPostRunHookContext _context;
        private bool _isBound;
        private bool _isCompleted;
        private bool _isVisible;
        private TaskCompletionSource<bool>? _completionSource;

        public string PresenterSignature { get; private set; } = string.Empty;

        public bool IsReady => _isBound && !string.IsNullOrWhiteSpace(PresenterSignature);

        private void OnEnable()
        {
            DebugUtility.LogVerbose<LevelPostRunHookPresenter>(
                $"[OBS][LevelFlow] LevelPostRunHookPresenterRegistered presenter='{name}' scene='{gameObject.scene.name}'.",
                DebugUtility.Colors.Info);
        }

        private void OnDisable()
        {
            if (_isBound && !_isCompleted)
            {
                CompleteAndDismiss("disable");
                return;
            }

            ClearSessionState();
        }

        public void BindToSession(LevelPostRunHookContext context)
        {
            if (context.LevelRef == null)
            {
                HardFailFastH1.Trigger(typeof(LevelPostRunHookPresenter),
                    "[FATAL][H1][LevelFlow] LevelPostRunHookContext sem levelRef ao bindar presenter visual de nivel.");
                return;
            }

            _context = context;
            PresenterSignature = Normalize(context.LevelSignature);
            _isBound = true;
            _isCompleted = false;
            _isVisible = true;
            _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            DebugUtility.Log<LevelPostRunHookPresenter>(
                $"[OBS][LevelFlow] LevelPostRunHookPresenterBound presenter='{name}' levelRef='{context.LevelRef.name}' signature='{PresenterSignature}' result='{context.Result}' reason='{Normalize(context.Reason)}' scene='{Normalize(context.SceneName)}' frame={context.Frame}.",
                DebugUtility.Colors.Info);
        }

        public Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
        {
            if (!_isBound || _completionSource == null)
            {
                throw new InvalidOperationException("[FATAL][Config][LevelFlow] LevelPostRunHookPresenter aguardado antes do bind.");
            }

            if (_isCompleted)
            {
                return Task.CompletedTask;
            }

            if (cancellationToken.CanBeCanceled)
            {
                return AwaitWithCancellationAsync(_completionSource.Task, cancellationToken);
            }

            return _completionSource.Task;
        }

        private void OnGUI()
        {
            if (!_isBound || !_isVisible || string.IsNullOrWhiteSpace(PresenterSignature))
            {
                return;
            }

            Rect panelRect = new Rect(panelOffset.x, panelOffset.y, PanelWidth, PanelHeight);
            GUILayout.BeginArea(panelRect, GUI.skin.box);
            GUILayout.Label("Level Post-Run");
            GUILayout.Space(4f);
            GUILayout.Label($"Level: {Normalize(_context.LevelRef != null ? _context.LevelRef.name : string.Empty)}");
            GUILayout.Label($"Signature: {PresenterSignature}");
            GUILayout.Space(6f);
            GUILayout.Label($"Result: {_context.Result}");
            GUILayout.Label($"Reason: {Normalize(_context.Reason)}");
            GUILayout.Space(12f);
            GUILayout.Label(_isCompleted ? "State: Completed" : "State: Waiting for Continue");
            GUILayout.Space(12f);

            GUI.enabled = !_isCompleted;
            if (GUILayout.Button("Continue", GUILayout.Height(42f)))
            {
                CompleteAndDismiss("button");
            }
            GUI.enabled = true;

            GUILayout.EndArea();
        }

        private void CompleteAndDismiss(string reason)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            _completionSource?.TrySetResult(true);

            DebugUtility.Log<LevelPostRunHookPresenter>(
                $"[OBS][LevelFlow] LevelPostRunHookPresenterCompleted presenter='{name}' signature='{PresenterSignature}' reason='{reason}' levelRef='{Normalize(_context.LevelRef != null ? _context.LevelRef.name : string.Empty)}' result='{_context.Result}'.",
                DebugUtility.Colors.Info);

            _isVisible = false;
            ClearSessionState(clearCompletionSource: false);

            DebugUtility.Log<LevelPostRunHookPresenter>(
                $"[OBS][LevelFlow] LevelPostRunHookPresenterDismissed presenter='{name}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private void ClearSessionState(bool clearCompletionSource = true)
        {
            _context = default;
            _isBound = false;
            PresenterSignature = string.Empty;

            if (clearCompletionSource)
            {
                _completionSource = null;
            }
        }

        private static async Task AwaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            Task cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            Task completedTask = await Task.WhenAny(task, cancellationTask).ConfigureAwait(true);

            if (completedTask == cancellationTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            await task.ConfigureAwait(true);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
