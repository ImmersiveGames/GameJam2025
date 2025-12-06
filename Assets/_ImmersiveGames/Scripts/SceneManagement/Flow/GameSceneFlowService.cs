using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Flow
{
    /// <summary>
    /// Implementação padrão do fluxo de cenas do jogo.
    /// Usa GameConfig para saber quais cenas formam Menu e Gameplay
    /// e delega o trabalho pesado para o planner + SceneTransitionService.
    /// </summary>
    public class GameSceneFlowService : IGameSceneFlowService
    {
        private readonly GameConfig _config;
        private readonly ISceneTransitionPlanner _planner;
        private readonly ISceneTransitionService _transitionService;

        public GameSceneFlowService(
            GameConfig config,
            ISceneTransitionPlanner planner,
            ISceneTransitionService transitionService)
        {
            _config = config;
            _planner = planner;
            _transitionService = transitionService;
        }

        public async Task GoToMenuAsync()
        {
            if (_config == null)
            {
                Debug.LogError("[GameSceneFlowService] GameConfig é null. Não é possível ir para o Menu.");
                return;
            }

            var targetScenes = new List<string> { _config.MenuScene };
            var state = SceneState.FromSceneManager();

            var context = _planner.BuildContext(
                state,
                targetScenes,
                _config.MenuScene, // cena ativa alvo
                useFade: true
            );

            Debug.Log("[GameSceneFlowService] GoToMenuAsync - contexto: " + context);
            await _transitionService.RunTransitionAsync(context);
        }

        public async Task GoToGameplayAsync()
        {
            if (_config == null)
            {
                Debug.LogError("[GameSceneFlowService] GameConfig é null. Não é possível ir para o Gameplay.");
                return;
            }

            var targetScenes = new List<string>
            {
                _config.GameplayScene,
                _config.UIScene
            };

            var state = SceneState.FromSceneManager();

            var context = _planner.BuildContext(
                state,
                targetScenes,
                _config.GameplayScene, // cena ativa alvo
                useFade: true
            );

            Debug.Log("[GameSceneFlowService] GoToGameplayAsync - contexto: " + context);
            await _transitionService.RunTransitionAsync(context);
        }

        public async Task ResetGameAsync()
        {
            Debug.Log("[GameSceneFlowService] ResetGameAsync chamado - delegando para GoToGameplayAsync.");
            await GoToGameplayAsync();
        }
    }
}
