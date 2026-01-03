// Assets/_ImmersiveGames/NewScripts/Infrastructure/Gameplay/GameCommands.cs

using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.Gameplay
{
    /*
     * Auditoria (GameCommands)
     * - GamePauseCommandEvent(bool isPaused) -> EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true/false)).
     * - GameResumeRequestedEvent() -> EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent()).
     * - GameRunEndRequestedEvent(GameRunOutcome outcome, string reason) -> IGameRunEndRequestService.RequestEnd(...)
     *   (publica EventBus<GameRunEndRequestedEvent> dentro do serviço).
     * - GameRunEndedEvent(GameRunOutcome outcome, string reason) -> publicado pelo GameRunOutcomeService via EventBus<GameRunEndedEvent>.
     * - GameResetRequestedEvent() -> EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent()).
     * - GameExitToMenuRequestedEvent() -> EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent()).
     */
    public sealed class GameCommands : IGameCommands
    {
        private readonly IGameRunEndRequestService _runEndRequestService;

        public GameCommands(IGameRunEndRequestService runEndRequestService)
        {
            _runEndRequestService = runEndRequestService;
        }

        public void RequestPause(string reason = null)
        {
            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestPause reason='{FormatReason(reason)}'");

            EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true));
        }

        public void RequestResume(string reason = null)
        {
            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestResume reason='{FormatReason(reason)}'");

            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
        }

        public void RequestVictory(string reason)
        {
            var normalizedReason = NormalizeRequiredReason(reason);

            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestVictory reason='{normalizedReason}'");

            RequestRunEnd(GameRunOutcome.Victory, normalizedReason);
        }

        public void RequestDefeat(string reason)
        {
            var normalizedReason = NormalizeRequiredReason(reason);

            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestDefeat reason='{normalizedReason}'");

            RequestRunEnd(GameRunOutcome.Defeat, normalizedReason);
        }

        public void RequestRestart(string reason)
        {
            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestRestart reason='{FormatReason(reason)}'");

            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
        }

        public void RequestExitToMenu(string reason)
        {
            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestExitToMenu reason='{FormatReason(reason)}'");

            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent());
        }

        private void RequestRunEnd(GameRunOutcome outcome, string reason)
        {
            if (_runEndRequestService != null)
            {
                _runEndRequestService.RequestEnd(outcome, reason);
                return;
            }

            DebugUtility.LogWarning(typeof(GameCommands),
                "[GameCommands] IGameRunEndRequestService indisponível. Publicando GameRunEndRequestedEvent diretamente.");

            EventBus<GameRunEndRequestedEvent>.Raise(new GameRunEndRequestedEvent(outcome, reason));
        }

        private static string FormatReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();
        }

        private static string NormalizeRequiredReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason.Trim();
        }
    }
}
