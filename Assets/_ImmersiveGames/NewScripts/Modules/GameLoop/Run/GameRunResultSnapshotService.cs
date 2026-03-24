using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Run
{
    /// <summary>
    /// Projeção simples do resultado da última run observada.
    ///
    /// Responsabilidades:
    /// - Materializar Outcome/Reason para UI e consultas.
    /// - Limpar o snapshot quando uma nova run começa.
    ///
    /// Não é owner do lifecycle terminal:
    /// - não valida Playing;
    /// - não decide PostGame;
    /// - não publica eventos.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunResultSnapshotService : IGameRunResultSnapshotService, IDisposable
    {
        private readonly EventBinding<GameRunEndedEvent> _binding;
        private readonly EventBinding<GameRunStartedEvent> _startBinding;
        private bool _disposed;

        public bool HasResult { get; private set; }
        public GameRunOutcome Outcome { get; private set; } = GameRunOutcome.Unknown;
        public string Reason { get; private set; }

        public GameRunResultSnapshotService()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _startBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            EventBus<GameRunEndedEvent>.Register(_binding);
            EventBus<GameRunStartedEvent>.Register(_startBinding);

            DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                "[GameLoop] GameRunResultSnapshotService registrado no EventBus<GameRunEndedEvent> e EventBus<GameRunStartedEvent>.");
        }

        public void ClearSnapshot()
        {
            HasResult = false;
            Outcome = GameRunOutcome.Unknown;
            Reason = null;

            DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                "[GameLoop] GameRunResultSnapshotService.ClearSnapshot() chamado. Resultado resetado para Unknown.");
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            if (_disposed || evt == null)
            {
                return;
            }

            if (HasResult)
            {
                DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                    $"[GameLoop] GameRunEndedEvent duplicado ignorado. CurrentOutcome={Outcome}, IncomingOutcome={evt.Outcome}, IncomingReason='{GameLoopReasonFormatter.Format(evt.Reason)}'.");
                return;
            }

            HasResult = true;
            Outcome = evt.Outcome;
            Reason = evt.Reason;

            DebugUtility.Log<GameRunResultSnapshotService>(
                $"[GameLoop] GameRunStatus atualizado. Outcome={Outcome}, Reason='{GameLoopReasonFormatter.Format(Reason)}'.");
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            bool wasDirty = HasResult || Outcome != GameRunOutcome.Unknown || !string.IsNullOrEmpty(Reason);
            if (wasDirty)
            {
                ClearSnapshot();
                DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                    $"[GameLoop] GameRunResultSnapshotService: nova run iniciada (state={evt?.StateId}). Resultado anterior resetado.");
                return;
            }

            DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                $"[GameLoop] GameRunStartedEvent observado (state={evt?.StateId}). Snapshot já estava limpo.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try { EventBus<GameRunEndedEvent>.Unregister(_binding); } catch { /* best-effort */ }
            try { EventBus<GameRunStartedEvent>.Unregister(_startBinding); } catch { /* best-effort */ }
        }
    }
}
