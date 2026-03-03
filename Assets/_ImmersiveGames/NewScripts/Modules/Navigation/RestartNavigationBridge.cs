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
    /// Bridge global: ao receber GameResetRequestedEvent, aciona o trilho canônico de LevelReset.
    /// Evita restart por navegação macro para manter o reset local no domínio de Level.
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
                string levelSignature = ResolveCurrentLevelSignature();

                DebugUtility.Log<RestartNavigationBridge>(
                    $"[OBS][LevelFlow] RestartUsesLevelReset reason='{reason}' levelSignature='{levelSignature}'.",
                    DebugUtility.Colors.Info);

                NavigationTaskRunner.FireAndForget(
                    levelFlowRuntime.ResetCurrentLevelAsync(reason, CancellationToken.None),
                    typeof(RestartNavigationBridge),
                    "Restart -> LevelFlow/ResetCurrentLevel");
                return;
            }

            DebugUtility.LogWarning<RestartNavigationBridge>(
                "[WARN][OBS][LevelFlow] RestartUsesLevelReset skipped reason='missing_level_flow_runtime_service'.");
        }

        private static string ResolveCurrentLevelSignature()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext) || restartContext == null)
            {
                return "<none>";
            }

            if (!restartContext.TryGetLastGameplayStartSnapshot(out GameplayStartSnapshot snapshot) || !snapshot.IsValid)
            {
                return "<none>";
            }

            if (!string.IsNullOrWhiteSpace(snapshot.LevelSignature))
            {
                return snapshot.LevelSignature;
            }

            if (!snapshot.HasLevelId || !snapshot.RouteId.IsValid)
            {
                return "<none>";
            }

            string contentId = snapshot.HasContentId ? snapshot.ContentId : string.Empty;
            return LevelContextSignature.Create(snapshot.LevelId, snapshot.RouteId, snapshot.Reason, contentId).Value;
        }
    }
}
