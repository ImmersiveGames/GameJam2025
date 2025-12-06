using System.Collections.Generic;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SceneManagement.Tests
{
    /// <summary>
    /// Tester que EXECUTA a transição:
    /// - Usa SceneState + SimpleSceneTransitionPlanner para montar o contexto
    /// - Usa SceneTransitionService para aplicar o plano (load/unload/active)
    /// - Opcionalmente usa FadeAwaiter para FadeIn/FadeOut
    ///
    /// Controles:
    /// - Tecla T: executa transição para o alvo configurado.
    /// </summary>
    public class SceneTransitionTester : MonoBehaviour
    {
        [Header("Cenas alvo após a transição")]
        [SerializeField] private string[] targetScenes;
        [SerializeField] private string explicitTargetActiveScene = "";
        [SerializeField] private bool useFade = true;

        private ISceneLoader _sceneLoader;
        private ISceneTransitionPlanner _planner;
        private ISceneTransitionService _transitionService;

        private void Awake()
        {
            _sceneLoader = new SceneLoaderCore();
            _planner = new SimpleSceneTransitionPlanner();

            IFadeAwaiter fadeAwaiter = null;

            if (useFade)
            {
                if (DependencyManager.Provider.TryGetGlobal(out IFadeService fadeService) &&
                    DependencyManager.Provider.TryGetGlobal(out ICoroutineRunner runner))
                {
                    fadeAwaiter = new FadeAwaiter(fadeService, runner);
                    Debug.Log("[SceneTransitionTester] FadeAwaiter configurado com sucesso.");
                }
                else
                {
                    Debug.LogWarning(
                        "[SceneTransitionTester] Não foi possível obter IFadeService e/ou ICoroutineRunner " +
                        "do DependencyManager. Transição será executada sem fade."
                    );
                    useFade = false;
                }
            }

            _transitionService = new SceneTransitionService(_sceneLoader, fadeAwaiter);
        }

        private async void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("[SceneTransitionTester] Tecla T pressionada - executando transição.");

                // 1) Snapshot do estado atual
                var state = SceneState.FromSceneManager();

                // 2) Monta contexto via planner
                var context = _planner.BuildContext(
                    state,
                    new List<string>(targetScenes),
                    explicitTargetActiveScene,
                    useFade);

                Debug.Log("[SceneTransitionTester] Contexto montado: " + context);

                // 3) Executa a transição
                await _transitionService.RunTransitionAsync(context);
            }
        }
    }
}
