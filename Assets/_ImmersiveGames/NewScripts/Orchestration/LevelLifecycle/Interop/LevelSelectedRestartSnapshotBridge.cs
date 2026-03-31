using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Interop
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
                "[LevelFlow] LevelSelectedRestartSnapshotBridge registered (LevelSelectedEvent -> GameplayStartSnapshot).",
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
                $"[OBS][LevelFlow] LevelSelectedEventConsumed levelRef='{(evt.LevelRef != null ? evt.LevelRef.name : "<none>")}' routeId='{evt.MacroRouteId}' routeRef='{(evt.MacroRouteRef != null ? evt.MacroRouteRef.name : "<none>")}' contentId='{evt.LocalContentId}' v='{evt.SelectionVersion}' levelSignature='{evt.LevelSignature}' restartContextResolved='{serviceResolved}'.",
                DebugUtility.Colors.Info);

            if (!serviceResolved)
            {
                return;
            }

            restartContext.RegisterGameplayStart(evt);
        }
    }
}
