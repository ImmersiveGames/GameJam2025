using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Run;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Input
{
    public interface IPauseCommands
    {
        void RequestPause(string reason = null);
        void RequestResume(string reason = null);
    }

    /*
     * Auditoria (GameLoopCommands)
     * - GamePauseCommandEvent(bool isPaused, string reason) -> EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true/false, reason)).
     * - GameResumeRequestedEvent(string reason) -> EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent(reason)).
     * - GameRunEndRequestedEvent(GameRunOutcome outcome, string reason) -> IGameRunEndRequestService.RequestRunEnd(...)
     *   (publica EventBus<GameRunEndRequestedEvent> dentro do serviço).
     * - GameRunEndedEvent(GameRunOutcome outcome, string reason) -> publicado pelo GameRunOutcomeService via EventBus<GameRunEndedEvent>.
     * - Restart/ExitToMenu => dispatch direto ao owner canonico downstream (LevelFlow/Navigation).
     */
    public sealed class GameLoopCommands : IGameLoopCommands
    {
        private readonly IGameRunEndRequestService _runEndRequestService;
        private const string DefaultExitToMenuReason = "GameLoopCommands/ExitToMenu";
        private const string DefaultRestartReason = "GameLoopCommands/Restart";

        public GameLoopCommands(IGameRunEndRequestService runEndRequestService)
        {
            _runEndRequestService = runEndRequestService;
        }

        public void RequestPause(string reason = null)
        {
            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestPause reason='{GameLoopReasonFormatter.Format(reason)}'");

            EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true, reason));
        }

        public void RequestResume(string reason = null)
        {
            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestResume reason='{GameLoopReasonFormatter.Format(reason)}'");

            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent(reason));
        }

        public void RequestVictory(string reason)
        {
            string normalizedReason = GameLoopReasonFormatter.NormalizeRequired(reason);

            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestVictory reason='{normalizedReason}'");

            RequestRunEnd(GameRunOutcome.Victory, normalizedReason);
        }

        public void RequestDefeat(string reason)
        {
            string normalizedReason = GameLoopReasonFormatter.NormalizeRequired(reason);

            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestDefeat reason='{normalizedReason}'");

            RequestRunEnd(GameRunOutcome.Defeat, normalizedReason);
        }

        public void RequestRestart(string reason)
        {
            string normalizedReason = GameLoopReasonFormatter.NormalizeOptional(reason, DefaultRestartReason);

            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestRestart reason='{normalizedReason}'");

            IPostLevelActionsService postLevelActions = ResolveRequiredPostLevelActionsOrFail(normalizedReason);
            _ = ObserveAsync(postLevelActions.RestartLevelAsync(normalizedReason, System.Threading.CancellationToken.None), normalizedReason, "Restart");
        }

        public void RequestExitToMenu(string reason)
        {
            string normalizedReason = GameLoopReasonFormatter.NormalizeOptional(reason, DefaultExitToMenuReason);

            DebugUtility.Log(typeof(GameLoopCommands),
                $"[GameLoopCommands] RequestExitToMenu reason='{normalizedReason}'");

            IGameNavigationService navigationService = ResolveRequiredNavigationServiceOrFail(normalizedReason);
            _ = ObserveAsync(navigationService.GoToMenuAsync(normalizedReason), normalizedReason, "ExitToMenu");
        }

        private static async System.Threading.Tasks.Task ObserveAsync(System.Threading.Tasks.Task task, string reason, string actionName)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(GameLoopCommands),
                    $"[GameLoopCommands] {actionName} failed reason='{reason}'. ex={ex}");
            }
        }

        private static IPostLevelActionsService ResolveRequiredPostLevelActionsOrFail(string reason)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameLoopCommands),
                    $"[FATAL][H1][LevelFlow] Restart requested without DependencyManager.Provider. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPostLevelActionsService>(out var postLevelActions) || postLevelActions == null)
            {
                HardFailFastH1.Trigger(typeof(GameLoopCommands),
                    $"[FATAL][H1][LevelFlow] Restart requested without IPostLevelActionsService. reason='{reason}'.");
            }

            return postLevelActions;
        }

        private static IGameNavigationService ResolveRequiredNavigationServiceOrFail(string reason)
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(GameLoopCommands),
                    $"[FATAL][H1][Navigation] ExitToMenu requested without DependencyManager.Provider. reason='{reason}'.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var navigationService) || navigationService == null)
            {
                HardFailFastH1.Trigger(typeof(GameLoopCommands),
                    $"[FATAL][H1][Navigation] ExitToMenu requested without IGameNavigationService. reason='{reason}'.");
            }

            return navigationService;
        }

        private void RequestRunEnd(GameRunOutcome outcome, string reason)
        {
            if (_runEndRequestService != null)
            {
                _runEndRequestService.RequestRunEnd(outcome, reason);
                return;
            }

            if (DependencyManager.HasInstance &&
                DependencyManager.Provider.TryGetGlobal<IGameRunEndRequestService>(out var runEndRequestService) &&
                runEndRequestService != null)
            {
                runEndRequestService.RequestRunEnd(outcome, reason);
                return;
            }

            DebugUtility.LogWarning(typeof(GameLoopCommands),
                "[GameLoopCommands] IGameRunEndRequestService indisponível. Ignorando RequestRunEnd.");
        }
    }
}
