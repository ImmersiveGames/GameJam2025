using System;
using _ImmersiveGames.NewScripts.ActorSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;

namespace _ImmersiveGames.NewScripts.ActorSystem.Integration.SessionFlow
{
    /// <summary>
    /// Thin semantic bridge that refreshes ActorSystem read model on participation updates.
    /// </summary>
    public sealed class ActorSystemParticipationRefreshBridge : IDisposable
    {
        private readonly IActorSystemReadModelService _readModelService;
        private readonly EventBinding<ParticipationSnapshotChangedEvent> _participationBinding;
        private bool _disposed;

        public ActorSystemParticipationRefreshBridge(IActorSystemReadModelService readModelService)
        {
            _readModelService = readModelService ?? throw new ArgumentNullException(nameof(readModelService));
            _participationBinding = new EventBinding<ParticipationSnapshotChangedEvent>(OnParticipationChanged);
            EventBus<ParticipationSnapshotChangedEvent>.Register(_participationBinding);

            DebugUtility.LogVerbose(typeof(ActorSystemParticipationRefreshBridge),
                "[OBS][ActorSystem][SessionFlow] Participation refresh bridge registrado.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<ParticipationSnapshotChangedEvent>.Unregister(_participationBinding);
        }

        private void OnParticipationChanged(ParticipationSnapshotChangedEvent evt)
        {
            if (_disposed || !evt.IsValid)
            {
                return;
            }

            if (evt.IsCleared)
            {
                _readModelService.Clear(evt.Reason);
                DebugUtility.LogVerbose(typeof(ActorSystemParticipationRefreshBridge),
                    $"[OBS][ActorSystem][SessionFlow] Snapshot limpo via participation clear reason='{evt.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _readModelService.Refresh();
        }
    }
}
