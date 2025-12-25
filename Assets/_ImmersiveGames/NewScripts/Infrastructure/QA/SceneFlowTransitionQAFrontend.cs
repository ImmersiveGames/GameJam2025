#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// QA-only helper para disparar transições do SceneFlow via ContextMenu.
    /// Objetivo: reproduzir transições e validar logs/gates/readiness sem depender de UI pronta.
    ///
    /// Notas importantes:
    /// - "FrontendScene" por padrão aponta para o asset "MenuScene". O nome do asset pode permanecer "MenuScene"
    ///   sem significar "Menu" como estado macro; o conceito aqui é Frontend/Ready/NonGameplay.
    /// - Não acoplar SceneFlow no WorldLifecycleController.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowTransitionQaFrontend : MonoBehaviour
    {
        [Header("Default Scene Names (ajuste se necessário)")]
        [Tooltip("Cena de frontend (por padrão, o asset chama 'MenuScene').")]
        [SerializeField] private string frontendScene = "MenuScene";
        [SerializeField] private string gameplayScene = "GameplayScene";
        [SerializeField] private string uiGlobalScene = "UIGlobalScene";
        [SerializeField] private bool useFade = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Inject] private ISceneTransitionService _sceneFlow;

        private bool _dependenciesInjected;
        private int _transitionInFlight; // 0/1

        private void Start()
        {
            EnsureDependenciesInjected();

            if (_sceneFlow == null)
            {
                DebugUtility.LogWarning(typeof(SceneFlowTransitionQaFrontend),
                    "[SceneFlow QA] ISceneTransitionService não disponível no DI global. ContextMenus não funcionarão.");
            }
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
                return;

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;
        }

        [ContextMenu("QA/SceneFlow/To Gameplay (profile=gameplay)")]
        public void ContextToGameplay()
        {
            var request = BuildToGameplayRequest();
            _ = RunTransitionAsync(request, "ContextMenu/SceneFlowToGameplay");
        }

        [ContextMenu("QA/SceneFlow/To Frontend (profile=startup)")]
        public void ContextToFrontend()
        {
            var request = BuildToFrontendRequest();
            _ = RunTransitionAsync(request, "ContextMenu/SceneFlowToFrontend");
        }

        private SceneTransitionRequest BuildToGameplayRequest()
        {
            var scenesToLoad = new List<string> { gameplayScene, uiGlobalScene };

            // Mantém "NewBootstrap" como safe-unload: se não estiver carregada, o adapter pula.
            var scenesToUnload = new List<string> { frontendScene, "NewBootstrap" };

            return new SceneTransitionRequest(
                scenesToLoad: scenesToLoad,
                scenesToUnload: scenesToUnload,
                targetActiveScene: gameplayScene,
                useFade: useFade,
                transitionProfileName: "gameplay");
        }

        private SceneTransitionRequest BuildToFrontendRequest()
        {
            var scenesToLoad = new List<string> { frontendScene, uiGlobalScene };
            var scenesToUnload = new List<string> { gameplayScene, "NewBootstrap" };

            return new SceneTransitionRequest(
                scenesToLoad: scenesToLoad,
                scenesToUnload: scenesToUnload,
                targetActiveScene: frontendScene,
                useFade: useFade,
                transitionProfileName: "startup");
        }

        private async Task RunTransitionAsync(SceneTransitionRequest request, string reason)
        {
            EnsureDependenciesInjected();

            if (_sceneFlow == null)
            {
                DebugUtility.LogError(typeof(SceneFlowTransitionQaFrontend),
                    "[SceneFlow QA] ISceneTransitionService indisponível. Não foi possível disparar TransitionAsync().",
                    this);
                return;
            }

            if (request == null)
            {
                DebugUtility.LogError(typeof(SceneFlowTransitionQaFrontend),
                    "[SceneFlow QA] Request nulo. Abortando.");
                return;
            }

            if (Interlocked.CompareExchange(ref _transitionInFlight, 1, 0) == 1)
            {
                DebugUtility.LogWarning(typeof(SceneFlowTransitionQaFrontend),
                    "[SceneFlow QA] Transição já está em andamento. Ignorando novo comando.");
                return;
            }

            try
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(SceneFlowTransitionQaFrontend),
                        $"[SceneFlow QA] Disparando TransitionAsync via ContextMenu. reason='{reason}', " +
                        $"targetActive='{request.TargetActiveScene}', profile='{request.TransitionProfileName}'.",
                        DebugUtility.Colors.Info);
                }

                await _sceneFlow.TransitionAsync(request);

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(SceneFlowTransitionQaFrontend),
                        $"[SceneFlow QA] TransitionAsync concluído. reason='{reason}', targetActive='{request.TargetActiveScene}'.",
                        DebugUtility.Colors.Success);
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneFlowTransitionQaFrontend),
                    $"[SceneFlow QA] Falha em TransitionAsync. reason='{reason}', ex={ex}",
                    this);
            }
            finally
            {
                Interlocked.Exchange(ref _transitionInFlight, 0);
            }
        }
    }
}
#endif
