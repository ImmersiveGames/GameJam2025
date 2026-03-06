using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    public sealed class RestartSnapshotContentSwapBridge : IDisposable
    {
        private readonly EventBinding<ContentSwapCommittedEvent> _committedBinding;
        private bool _disposed;

        public RestartSnapshotContentSwapBridge()
        {
            _committedBinding = new EventBinding<ContentSwapCommittedEvent>(OnContentSwapCommitted);
            EventBus<ContentSwapCommittedEvent>.Register(_committedBinding);

            DebugUtility.LogVerbose<RestartSnapshotContentSwapBridge>(
                "[WARN][LEGACY_API_USED][Navigation] RestartSnapshotContentSwapBridge is no-op in canonical LevelFlow.",
                DebugUtility.Colors.Warning);
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

        private static void OnContentSwapCommitted(ContentSwapCommittedEvent _)
        {
            // Comentario: trilho canonico nao sincroniza snapshot por contentId.
        }
    }
}
