using System;
using System.Threading;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Bridge global: ao receber GameResetRequestedEvent, aciona o IGameNavigationService.
    /// Mantém o reset oficial via SceneFlow + WorldLifecycle (profile gameplay).
    /// </summary>
    public sealed class RestartNavigationBridge : IDisposable
    {
        private const string RestartReason = "PostGame/Restart";

        private readonly EventBinding<GameResetRequestedEvent> _resetBinding;
        private bool _disposed;

        public RestartNavigationBridge()
        {
            _resetBinding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
            EventBus<GameResetRequestedEvent>.Register(_resetBinding);

            DebugUtility.LogVerbose<RestartNavigationBridge>(
                "[Navigation] RestartNavigationBridge registrado (GameResetRequestedEvent -> RestartAsync).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GameResetRequestedEvent>.Unregister(_resetBinding);
        }

        private void OnResetRequested(GameResetRequestedEvent evt)
        {
            string reason = evt?.Reason ?? RestartReason;

            if (DependencyManager.Provider.TryGetGlobal<ILevelFlowRuntimeService>(out var levelFlowRuntime) && levelFlowRuntime != null)
            {
                DebugUtility.Log<RestartNavigationBridge>(
                    $"[Navigation] GameResetRequestedEvent recebido -> RestartLastGameplayAsync. reason='{reason}'.",
                    DebugUtility.Colors.Info);

                NavigationTaskRunner.FireAndForget(
                    levelFlowRuntime.RestartLastGameplayAsync(reason, CancellationToken.None),
                    typeof(RestartNavigationBridge),
                    "Restart -> LevelFlow/RestartLastGameplay");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigation) || navigation == null)
            {
                DebugUtility.LogWarning<RestartNavigationBridge>(
                    "[Navigation] IGameNavigationService indisponível; Restart ignorado.");
                return;
            }

            DebugUtility.LogWarning<RestartNavigationBridge>(
                "[WARN][OBS][Navigation] Restart fallback -> IGameNavigationService.RestartAsync (LevelFlowRuntimeService missing).");

            DebugUtility.Log<RestartNavigationBridge>(
                $"[Navigation] GameResetRequestedEvent recebido -> RestartAsync. reason='{reason}'.",
                DebugUtility.Colors.Info);

            NavigationTaskRunner.FireAndForget(
                navigation.RestartAsync(reason),
                typeof(RestartNavigationBridge),
                $"Restart -> coreIntent=Gameplay");
        }
    }
}
