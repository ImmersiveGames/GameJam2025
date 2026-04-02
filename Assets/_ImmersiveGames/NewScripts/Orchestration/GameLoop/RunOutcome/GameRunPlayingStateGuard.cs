using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunOutcome
{
    /// <summary>
    /// Centraliza a validação de gameplay ativo para serviços de run.
    ///
    /// Responsabilidade:
    /// - Considerar gameplay ativo apenas quando o GameLoop está em Playing.
    /// - Expor o nome do estado atual para logs/diagnóstico.
    ///
    /// Observação:
    /// - Falha de forma segura quando IGameLoopService está indisponível.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunPlayingStateGuard : IGameRunPlayingStateGuard
    {
        private readonly IGameLoopService _gameLoopService;

        public GameRunPlayingStateGuard(IGameLoopService gameLoopService)
        {
            _gameLoopService = gameLoopService;
        }

        public bool IsInActiveGameplay(out string stateName)
        {
            if (_gameLoopService == null)
            {
                stateName = "<null>";

                DebugUtility.LogWarning<GameRunPlayingStateGuard>(
                    "[GameLoop] IGameLoopService indisponível; não é possível validar gameplay ativo (Playing).");
                return false;
            }

            stateName = _gameLoopService.CurrentStateIdName ?? string.Empty;
            return string.Equals(stateName, nameof(GameLoopStateId.Playing), System.StringComparison.Ordinal);
        }
    }
}
