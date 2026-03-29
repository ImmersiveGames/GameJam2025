using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.PostGame;

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
    ///
    /// Observação de slice:
    /// - este serviço fica como bridge temporária de resultado em GameLoop até o backbone
    ///   de PostGame assumir integralmente o consumo visual downstream.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunResultSnapshotService : IGameRunResultSnapshotService, IDisposable
    {
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<PostGameResultUpdatedEvent> _binding;
        private readonly EventBinding<GameRunStartedEvent> _startBinding;
        private bool _disposed;

        public bool HasResult { get; private set; }
        public GameRunOutcome Outcome { get; private set; } = GameRunOutcome.Unknown;
        public string Reason { get; private set; }

        public GameRunResultSnapshotService()
        {
            _binding = new EventBinding<PostGameResultUpdatedEvent>(OnPostGameResultUpdated);
            _startBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            _subscriptions.Register(_binding);
            _subscriptions.Register(_startBinding);

            DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                "[OBS][ExitStage] GameRunResultSnapshotService bridge registrado no EventBus<PostGameResultUpdatedEvent> e EventBus<GameRunStartedEvent>.");
        }

        public void ClearSnapshot()
        {
            HasResult = false;
            Outcome = GameRunOutcome.Unknown;
            Reason = null;

            DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                "[OBS][ExitStage] GameRunResultBridgeSnapshot limpo. Resultado resetado para Unknown.");
        }

        private void OnPostGameResultUpdated(PostGameResultUpdatedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            if (HasResult)
            {
                DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                    $"[OBS][ExitStage] RunResultBridgeDuplicateIgnored currentOutcome={Outcome} incomingOutcome={evt.Result} incomingReason='{GameLoopReasonFormatter.Format(evt.Reason)}'.");
                return;
            }

            HasResult = true;
            Outcome = evt.Result switch
            {
                PostGameResult.Victory => GameRunOutcome.Victory,
                PostGameResult.Defeat => GameRunOutcome.Defeat,
                _ => GameRunOutcome.Unknown,
            };
            Reason = evt.Reason;

            DebugUtility.Log<GameRunResultSnapshotService>(
                $"[OBS][ExitStage] RunResultBridgeSnapshotUpdated outcome={Outcome} reason='{GameLoopReasonFormatter.Format(Reason)}' source='PostGameResultUpdatedEvent'.");
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
                    $"[OBS][ExitStage] GameRunResultBridgeSnapshot resetado em nova run (state={evt?.StateId}).");
                return;
            }

            DebugUtility.LogVerbose<GameRunResultSnapshotService>(
                $"[OBS][ExitStage] GameRunStartedEvent observado (state={evt?.StateId}). Bridge snapshot já estava limpo.");
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subscriptions.Dispose();
        }
    }
}
