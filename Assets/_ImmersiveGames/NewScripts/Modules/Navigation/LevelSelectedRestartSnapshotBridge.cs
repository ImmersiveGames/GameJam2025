using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Bridge global: sincroniza o snapshot de restart a partir do evento canônico de seleção de level.
    /// </summary>
    public sealed class LevelSelectedRestartSnapshotBridge : IDisposable
    {
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private bool _disposed;

        public LevelSelectedRestartSnapshotBridge()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<LevelSelectedRestartSnapshotBridge>(
                "[Navigation] LevelSelectedRestartSnapshotBridge registrado (LevelSelectedEvent -> GameplayStartSnapshot).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<LevelSelectedEvent>.Unregister(_levelSelectedBinding);
        }

        private static void OnLevelSelected(LevelSelectedEvent evt)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext) || restartContext == null)
            {
                return;
            }

            var snapshot = new GameplayStartSnapshot(
                evt.LevelId,
                evt.RouteId,
                TransitionStyleId.None,
                evt.ContentId,
                evt.Reason,
                evt.SelectionVersion,
                evt.ContextSignature);

            restartContext.UpdateGameplayStartSnapshot(snapshot);
        }
    }
}
