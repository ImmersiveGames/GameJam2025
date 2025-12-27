using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;

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
        private const int DebounceFrameWindow = 0;

        private readonly ISceneTransitionService _sceneTransitionService;
        private int _transitionInFlight;
        private int _lastRequestFrame = -1;
        private long _lastRequestTimestampMs = -1;

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

            var nowMs = Environment.TickCount64;
            var lastFrame = Volatile.Read(ref _lastRequestFrame);
            var elapsedMs = lastFrame < 0 ? -1 : nowMs - Interlocked.Read(ref _lastRequestTimestampMs);
            if (lastFrame >= 0 && Time.frameCount - lastFrame <= DebounceFrameWindow)
            {
                DebugUtility.LogWarning<GameNavigationService>(
                    $"[Navigation] Pedido duplicado (debounced) elapsedMs={elapsedMs}.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogWarning<GameNavigationService>(
                    $"[Navigation] Debounce stack trace:\n{BuildShortStackTrace()}");
#endif
                return;
            }

            Interlocked.Exchange(ref _lastRequestFrame, Time.frameCount);
            Interlocked.Exchange(ref _lastRequestTimestampMs, nowMs);

            if (Interlocked.CompareExchange(ref _transitionInFlight, 1, 0) == 1)
            {
                DebugUtility.LogWarning<GameNavigationService>(
                    "[Navigation] Transição já em andamento. Ignorando novo pedido.");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogWarning<GameNavigationService>(
                    $"[Navigation] TransitionInFlight stack trace:\n{BuildShortStackTrace()}");
#endif
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

                // Produção: ao entrar em Gameplay, avançar GameLoop para Playing.
                if (string.Equals(request.TargetActiveScene, SceneGameplay, StringComparison.Ordinal))
                {
                    TryRequestStartAfterGameplayTransition(reason);
                }
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static string BuildShortStackTrace()
        {
            var stack = Environment.StackTrace;
            if (string.IsNullOrWhiteSpace(stack))
            {
                return "<stack trace indisponível>";
            }

            var lines = stack.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            const int maxLines = 10;
            if (lines.Length <= maxLines)
            {
                return stack;
            }

            return string.Join("\n", lines.Take(maxLines));
        }
#endif

        private static void TryRequestStartAfterGameplayTransition(string reason)
        {
            if (!DependencyManager.HasInstance)
            {
                DebugUtility.LogWarning<GameNavigationService>(
                    $"[Navigation] DependencyManager indisponível; não foi possível RequestStart. reason='{reason}'.");
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop) || gameLoop == null)
            {
                DebugUtility.LogWarning<GameNavigationService>(
                    $"[Navigation] IGameLoopService não encontrado no DI global; não foi possível RequestStart. reason='{reason}'.");
                return;
            }

            DebugUtility.Log<GameNavigationService>(
                $"[Navigation] Entrando em Gameplay → chamando GameLoop.RequestStart(). reason='{reason}'.",
                DebugUtility.Colors.Info);

            gameLoop.RequestStart();
        }
    }
}
