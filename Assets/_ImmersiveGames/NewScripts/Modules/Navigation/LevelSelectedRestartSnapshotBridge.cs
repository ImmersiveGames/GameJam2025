using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    public sealed class LevelSelectedRestartSnapshotBridge : IDisposable
    {
        private readonly EventBinding<LevelSelectedEvent> _levelSelectedBinding;
        private bool _disposed;

        public LevelSelectedRestartSnapshotBridge()
        {
            _levelSelectedBinding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
            EventBus<LevelSelectedEvent>.Register(_levelSelectedBinding);

            DebugUtility.LogVerbose<LevelSelectedRestartSnapshotBridge>(
                "[Navigation] LevelSelectedRestartSnapshotBridge registered (LevelSelectedEvent -> GameplayStartSnapshot).",
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
            bool serviceResolved = DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext) && restartContext != null;

            DebugUtility.Log(typeof(LevelSelectedRestartSnapshotBridge),
                $"[OBS][Navigation] LevelSelectedEventConsumed levelRef='{(evt.LevelRef != null ? evt.LevelRef.name : "<none>")}' routeId='{evt.MacroRouteId}' v='{evt.SelectionVersion}' levelSignature='{evt.LevelSignature}' restartContextResolved='{serviceResolved}'.",
                DebugUtility.Colors.Info);

            if (!serviceResolved)
            {
                return;
            }

            var snapshot = new GameplayStartSnapshot(
                evt.LevelRef,
                evt.MacroRouteId,
                evt.Reason,
                evt.SelectionVersion,
                evt.LevelSignature,
                TransitionStyleId.None);

            restartContext.UpdateGameplayStartSnapshot(snapshot);
        }
    }
}

