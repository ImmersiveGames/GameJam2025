using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Flow;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Tests
{
    /// <summary>
    /// Tester de fluxo real usando GameConfig.
    ///
    /// Controles:
    /// - M: Ir para Menu (MenuScene)
    /// - G: Ir para Gameplay (GameplayScene + UIScene)
    /// - R: Reset do jogo (por enquanto, GoToGameplay novamente)
    /// </summary>
    public class GameSceneFlowTester : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private GameConfig gameConfig;

        private IGameSceneFlowService _flowService;

        private void Awake()
        {
            if (gameConfig == null)
            {
                Debug.LogError("[GameSceneFlowTester] GameConfig não atribuído no Inspector.");
                return;
            }

            // Infra de baixo nível
            var sceneLoader = new SceneLoaderCore();
            var planner = new SimpleSceneTransitionPlanner();

            // FadeAwaiter opcional (usando DependencyManager)
            IFadeAwaiter fadeAwaiter = null;

            if (DependencyManager.Provider.TryGetGlobal(out IFadeService fadeService) &&
                DependencyManager.Provider.TryGetGlobal(out ICoroutineRunner runner))
            {
                fadeAwaiter = new FadeAwaiter(fadeService, runner);
                Debug.Log("[GameSceneFlowTester] FadeAwaiter configurado com sucesso.");
            }
            else
            {
                Debug.LogWarning(
                    "[GameSceneFlowTester] Não foi possível resolver IFadeService/ICoroutineRunner. " +
                    "Transições serão executadas sem fade."
                );
            }

            var transitionService = new SceneTransitionService(sceneLoader, fadeAwaiter);

            _flowService = new GameSceneFlowService(gameConfig, planner, transitionService);
        }

        private async void Update()
        {
            if (_flowService == null)
                return;

            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[GameSceneFlowTester] M pressionado - GoToMenuAsync.");
                await SafeRunAsync(_flowService.GoToMenuAsync);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("[GameSceneFlowTester] G pressionado - GoToGameplayAsync.");
                await SafeRunAsync(_flowService.GoToGameplayAsync);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("[GameSceneFlowTester] R pressionado - ResetGameAsync.");
                await SafeRunAsync(_flowService.ResetGameAsync);
            }
        }

        private async Task SafeRunAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameSceneFlowTester] Erro ao executar ação assíncrona: " + e);
            }
        }
    }
}
