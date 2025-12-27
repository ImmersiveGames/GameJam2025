using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;

namespace _ImmersiveGames.NewScripts.Infrastructure.Navigation
{
    /// <summary>
    /// Serviço global para navegação de produção usando SceneFlow.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameNavigationService : IGameNavigationService
    {
        private const string SceneMenu = "MenuScene";
        private const string SceneGameplay = "GameplayScene";
        private const string SceneUIGlobal = "UIGlobalScene";

        private readonly ISceneTransitionService _sceneTransitionService;
        private int _transitionInFlight;

        public GameNavigationService(ISceneTransitionService sceneTransitionService)
        {
            _sceneTransitionService = sceneTransitionService;
        }

        public Task RequestToGameplay(string reason = null)
        {
            var request = BuildToGameplayRequest();
            return RunTransitionAsync(request, reason ?? "GameNavigation/ToGameplay");
        }

        public Task RequestToMenu(string reason = null)
        {
            var request = BuildToMenuRequest();
            return RunTransitionAsync(request, reason ?? "GameNavigation/ToMenu");
        }

        private static SceneTransitionRequest BuildToGameplayRequest()
        {
            var scenesToLoad = new List<string> { SceneGameplay, SceneUIGlobal };
            var scenesToUnload = new List<string> { SceneMenu };

            return new SceneTransitionRequest(
                scenesToLoad: scenesToLoad,
                scenesToUnload: scenesToUnload,
                targetActiveScene: SceneGameplay,
                useFade: true,
                transitionProfileName: SceneFlowProfileNames.Gameplay);
        }

        private static SceneTransitionRequest BuildToMenuRequest()
        {
            var scenesToLoad = new List<string> { SceneMenu, SceneUIGlobal };
            var scenesToUnload = new List<string> { SceneGameplay };

            return new SceneTransitionRequest(
                scenesToLoad: scenesToLoad,
                scenesToUnload: scenesToUnload,
                targetActiveScene: SceneMenu,
                useFade: true,
                transitionProfileName: SceneFlowProfileNames.Startup);
        }

        private async Task RunTransitionAsync(SceneTransitionRequest request, string reason)
        {
            if (_sceneTransitionService == null)
            {
                DebugUtility.LogError<GameNavigationService>(
                    "[Navigation] ISceneTransitionService indisponível. Transição não será executada.");
                return;
            }

            if (request == null)
            {
                DebugUtility.LogError<GameNavigationService>(
                    "[Navigation] Request nulo. Abortando transição.");
                return;
            }

            if (Interlocked.CompareExchange(ref _transitionInFlight, 1, 0) == 1)
            {
                DebugUtility.LogWarning<GameNavigationService>(
                    "[Navigation] Transição já em andamento. Ignorando novo pedido.");
                return;
            }

            try
            {
                DebugUtility.Log<GameNavigationService>(
                    $"[Navigation] Disparando TransitionAsync. reason='{reason}', targetActive='{request.TargetActiveScene}', profile='{request.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                await _sceneTransitionService.TransitionAsync(request);

                DebugUtility.Log<GameNavigationService>(
                    $"[Navigation] TransitionAsync concluído. reason='{reason}', targetActive='{request.TargetActiveScene}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameNavigationService>(
                    $"[Navigation] Falha em TransitionAsync. reason='{reason}', ex={ex}");
            }
            finally
            {
                Interlocked.Exchange(ref _transitionInFlight, 0);
            }
        }
    }
}
