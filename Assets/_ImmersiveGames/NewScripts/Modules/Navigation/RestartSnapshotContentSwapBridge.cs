using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Bridge global: ao commitar ContentSwap, sincroniza contentId no snapshot de restart.
    /// Não altera routeId/levelId; apenas reforça observabilidade para RestartAsync.
    /// </summary>
    public sealed class RestartSnapshotContentSwapBridge : IDisposable
    {
        private readonly EventBinding<ContentSwapCommittedEvent> _committedBinding;
        private bool _disposed;

        public RestartSnapshotContentSwapBridge()
        {
            _committedBinding = new EventBinding<ContentSwapCommittedEvent>(OnContentSwapCommitted);
            EventBus<ContentSwapCommittedEvent>.Register(_committedBinding);

            DebugUtility.LogVerbose<RestartSnapshotContentSwapBridge>(
                "[Navigation] RestartSnapshotContentSwapBridge registrado (ContentSwapCommittedEvent -> RestartContext.contentId).",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<ContentSwapCommittedEvent>.Unregister(_committedBinding);
        }

        private static void OnContentSwapCommitted(ContentSwapCommittedEvent evt)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var restartContext) || restartContext == null)
            {
                return;
            }

            if (!restartContext.TryGetCurrent(out var currentSnapshot) || !currentSnapshot.IsValid)
            {
                return;
            }

            string nextContentId = string.IsNullOrWhiteSpace(evt.current.contentId)
                ? string.Empty
                : evt.current.contentId.Trim();

            string reasonSuffix = string.IsNullOrWhiteSpace(evt.reason) ? "Committed" : evt.reason.Trim();
            string reason = $"ContentSwap/{reasonSuffix}";

            if (!restartContext.TryUpdateCurrentContentId(nextContentId, reason))
            {
                return;
            }

            if (!restartContext.TryGetCurrent(out var updatedSnapshot) || !updatedSnapshot.IsValid)
            {
                return;
            }

            DebugUtility.Log(typeof(RestartSnapshotContentSwapBridge),
                $"[OBS][Navigation] RestartSnapshotContentUpdated routeId='{updatedSnapshot.RouteId}' levelId='{(updatedSnapshot.HasLevelId ? updatedSnapshot.LevelId.ToString() : "<none>")}' contentId='{(updatedSnapshot.HasContentId ? updatedSnapshot.ContentId : "<none>")}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }
    }
}
