using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Flow
{
    /// <summary>
    /// Implementação padrão do fluxo de cenas do jogo.
    /// 
    /// Nesta versão, o serviço é orientado a dados via SceneFlowMap/SceneGroupProfile:
    /// - Se um SceneFlowMap for fornecido, ele será sempre priorizado.
    /// - Caso contrário, há um fallback opcional baseado em GameConfig legado.
    /// 
    /// O trabalho pesado de planejamento é delegado para ISceneTransitionPlanner
    /// e a execução para ISceneTransitionService.
    /// </summary>
    public class GameSceneFlowService : IGameSceneFlowService
    {
        // Novo caminho preferencial (orientado a SceneGroupProfile/SceneFlowMap)
        private readonly SceneFlowMap _flowMap;

        // Caminho legado (mantido apenas como fallback)
        private readonly GameManagerSystems.GameConfig _config;

        private readonly ISceneTransitionPlanner _planner;
        private readonly ISceneTransitionService _transitionService;

        #region Construtores

        /// <summary>
        /// Construtor moderno, orientado a SceneFlowMap.
        /// </summary>
        public GameSceneFlowService(
            SceneFlowMap flowMap,
            ISceneTransitionPlanner planner,
            ISceneTransitionService transitionService)
        {
            _flowMap = flowMap;
            _planner = planner;
            _transitionService = transitionService;
        }

        /// <summary>
        /// Construtor legado, baseado em GameConfig.
        /// Mantido para compatibilidade com código existente que ainda não migrou para SceneFlowMap.
        /// </summary>
        public GameSceneFlowService(
            GameManagerSystems.GameConfig config,
            ISceneTransitionPlanner planner,
            ISceneTransitionService transitionService)
        {
            _config = config;
            _planner = planner;
            _transitionService = transitionService;
        }

        #endregion

        public async Task GoToMenuAsync()
        {
            // Caminho moderno: SceneFlowMap + SceneGroupProfile
            if (_flowMap != null && _flowMap.MenuGroup != null)
            {
                var targetGroup = _flowMap.MenuGroup;

                var state = SceneState.Capture();
                var context = _planner.BuildContext(state, targetGroup);

                Debug.Log("[GameSceneFlowService] GoToMenuAsync (FlowMap) - contexto: " + context);
                await _transitionService.RunTransitionAsync(context);
                return;
            }

            // Fallback legado baseado em GameConfig
            if (_config == null)
            {
                Debug.LogError("[GameSceneFlowService] GoToMenuAsync: nem SceneFlowMap.MenuGroup nem GameConfig disponíveis.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_config.MenuScene))
            {
                Debug.LogError("[GameSceneFlowService] GoToMenuAsync: GameConfig.MenuScene não definido.");
                return;
            }

            var targetScenes = new List<string> { _config.MenuScene };
            var legacyState = SceneState.Capture();

            var legacyContext = _planner.BuildContext(
                legacyState,
                targetScenes,
                _config.MenuScene,
                useFade: true);

            Debug.Log("[GameSceneFlowService] GoToMenuAsync (Legacy) - contexto: " + legacyContext);
            await _transitionService.RunTransitionAsync(legacyContext);
        }

        public async Task GoToGameplayAsync()
        {
            // Caminho moderno: SceneFlowMap + SceneGroupProfile
            if (_flowMap != null && _flowMap.GameplayGroup != null)
            {
                var targetGroup = _flowMap.GameplayGroup;

                var state = SceneState.Capture();
                var context = _planner.BuildContext(state, targetGroup);

                Debug.Log("[GameSceneFlowService] GoToGameplayAsync (FlowMap) - contexto: " + context);
                await _transitionService.RunTransitionAsync(context);
                return;
            }

            // Fallback legado baseado em GameConfig
            if (_config == null)
            {
                Debug.LogError("[GameSceneFlowService] GoToGameplayAsync: nem SceneFlowMap.GameplayGroup nem GameConfig disponíveis.");
                return;
            }

            var targetScenes = new List<string>();

            if (!string.IsNullOrWhiteSpace(_config.GameplayScene))
                targetScenes.Add(_config.GameplayScene);
            if (!string.IsNullOrWhiteSpace(_config.UIScene))
                targetScenes.Add(_config.UIScene);

            if (targetScenes.Count == 0)
            {
                Debug.LogError("[GameSceneFlowService] GoToGameplayAsync: GameConfig sem cenas definidas para Gameplay/UI.");
                return;
            }

            var stateLegacy = SceneState.Capture();

            var contextLegacy = _planner.BuildContext(
                stateLegacy,
                targetScenes,
                _config.GameplayScene,
                useFade: true);

            Debug.Log("[GameSceneFlowService] GoToGameplayAsync (Legacy) - contexto: " + contextLegacy);
            await _transitionService.RunTransitionAsync(contextLegacy);
        }

        public async Task ResetGameAsync()
        {
            Debug.Log("[GameSceneFlowService] ResetGameAsync chamado - delegando para GoToGameplayAsync.");
            await GoToGameplayAsync();
        }
    }
}
