// Assets/_ImmersiveGames/NewScripts/Infrastructure/Gameplay/GameCommands.cs

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Commands
{
    /*
     * Auditoria (GameCommands)
     * - GamePauseCommandEvent(bool isPaused) -> EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true/false)).
     * - GameResumeRequestedEvent() -> EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent()).
     * - GameRunEndRequestedEvent(GameRunOutcome outcome, string reason) -> IGameRunEndRequestService.RequestEnd(...)
     *   (publica EventBus<GameRunEndRequestedEvent> dentro do serviço).
     * - GameRunEndedEvent(GameRunOutcome outcome, string reason) -> publicado pelo GameRunOutcomeService via EventBus<GameRunEndedEvent>.
     * - GameResetRequestedEvent(reason) -> EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent(reason)).
     * - GameExitToMenuRequestedEvent(reason) -> EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(reason)).
     */
    public sealed class GameCommands : IGameCommands
    {
        private readonly IGameRunEndRequestService _runEndRequestService;
        private const string DefaultExitToMenuReason = "GameCommands/ExitToMenu";
        private const string DefaultRestartReason = "GameCommands/Restart";

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
            string normalizedReason = NormalizeRequiredReason(reason);

            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestVictory reason='{normalizedReason}'");

            RequestRunEnd(GameRunOutcome.Victory, normalizedReason);
        }

        public void RequestDefeat(string reason)
        {
            string normalizedReason = NormalizeRequiredReason(reason);

            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestDefeat reason='{normalizedReason}'");

            RequestRunEnd(GameRunOutcome.Defeat, normalizedReason);
        }

        public void RequestRestart(string reason)
        {
            string normalizedReason = NormalizeOptionalReason(reason, DefaultRestartReason);

            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestRestart reason='{normalizedReason}'");

            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent(normalizedReason));
        }

        public void RequestExitToMenu(string reason)
        {
            string normalizedReason = NormalizeOptionalReason(reason, DefaultExitToMenuReason);

            DebugUtility.Log(typeof(GameCommands),
                $"[GameCommands] RequestExitToMenu reason='{normalizedReason}'");

            EventBus<GameExitToMenuRequestedEvent>.Raise(new GameExitToMenuRequestedEvent(normalizedReason));
        }

        private void RequestRunEnd(GameRunOutcome outcome, string reason)
        {
            if (_runEndRequestService != null)
            {
                _runEndRequestService.RequestEnd(outcome, reason);
                return;
            }

            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService) &&
                runEndRequestService != null)
            {
                runEndRequestService.RequestEnd(outcome, reason);
                return;
            }

            DebugUtility.LogWarning(typeof(GameCommands),
                "[GameCommands] IGameRunEndRequestService indisponível. Ignorando RequestRunEnd.");
        }

        private static string FormatReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();
        }

        private static string NormalizeRequiredReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "Unspecified" : reason.Trim();
        }

        private static string NormalizeOptionalReason(string reason, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            return string.IsNullOrWhiteSpace(fallback) ? "Unspecified" : fallback.Trim();
        }
    }
}

