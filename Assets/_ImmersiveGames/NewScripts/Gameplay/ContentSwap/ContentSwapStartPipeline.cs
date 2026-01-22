#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
{
    [DebugLevel(DebugLevel.Verbose)]
    public static class ContentSwapStartPipeline
    {
        private const int ReadyWaitTimeoutMs = 2000;
        private const int ReadyWaitStepMs = 50;
        private const int ReadyWaitGraceMs = 250;

        public static async Task RunAsync(ContentSwapStartRequest request)
        {
            if (!request.IsValid)
            {
                DebugUtility.LogWarning(typeof(ContentSwapStartPipeline),
                    "[ContentSwapStart] Request inválido. Pipeline ignorado.");
                return;
            }

            var gameLoop = ResolveGameLoopService();
            if (gameLoop == null)
            {
                DebugUtility.LogWarning(typeof(ContentSwapStartPipeline),
                    "[ContentSwapStart] IGameLoopService indisponível; pipeline ignorado.");
                return;
            }

            await EnsureLoopReadyAsync(gameLoop, request);

            var coordinator = ResolveIntroStageCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning(typeof(ContentSwapStartPipeline),
                    "[ContentSwapStart] IIntroStageCoordinator indisponível; IntroStage não será executada.");
                return;
            }

            var targetScene = string.IsNullOrWhiteSpace(request.TargetScene)
                ? SceneManager.GetActiveScene().name
                : request.TargetScene.Trim();

            DebugUtility.Log(typeof(ContentSwapStartPipeline),
                $"[OBS][ContentSwap] ContentSwapStartPipeline -> IntroStage. contentId='{request.ContentId}' signature='{request.ContextSignature}' " +
                $"scene='{targetScene}' reason='{request.Reason}'.",
                DebugUtility.Colors.Info);

            var introStageContext = new IntroStageContext(
                request.ContextSignature,
                SceneFlowProfileId.Gameplay,
                targetScene,
                request.Reason);

            try
            {
                await coordinator.RunIntroStageAsync(introStageContext);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(ContentSwapStartPipeline),
                    $"[ContentSwapStart] Falha ao executar IntroStage. contentId='{request.ContentId}', ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static async Task EnsureLoopReadyAsync(IGameLoopService gameLoop, ContentSwapStartRequest request)
        {
            var stateBefore = gameLoop.CurrentStateIdName ?? string.Empty;
            if (ShouldRequestReady(stateBefore))
            {
                DebugUtility.LogVerbose(typeof(ContentSwapStartPipeline),
                    $"[ContentSwapStart] RequestReady antes da IntroStage. state='{stateBefore}' contentId='{request.ContentId}'.");
                gameLoop.RequestReady();
            }

            if (IsReadyState(gameLoop.CurrentStateIdName))
            {
                return;
            }

            // Observação: para troca de conteúdo, o GameLoop pode permanecer em 'Playing' sem transitar para 'Ready'.
            // A IntroStage já bloqueia a simulação via gate próprio, então não faz sentido aguardar longamente.
            // Aguardamos apenas uma janela curta para permitir transições assíncronas, e então prosseguimos.
            var graceStart = Environment.TickCount;
            while (Environment.TickCount - graceStart < ReadyWaitGraceMs)
            {
                await Task.Delay(ReadyWaitStepMs);
                if (IsReadyState(gameLoop.CurrentStateIdName))
                {
                    return;
                }
            }

            var stateAfterGrace = gameLoop.CurrentStateIdName ?? string.Empty;
            if (ShouldRequestReady(stateBefore) && ShouldRequestReady(stateAfterGrace))
            {
                DebugUtility.LogVerbose(typeof(ContentSwapStartPipeline),
                    $"[ContentSwapStart] Prosseguindo sem aguardar Ready (estado='{stateAfterGrace}'). contentId='{request.ContentId}'.");
                return;
            }

            // Fallback: em casos inesperados (ex.: estados intermediários), mantém o comportamento de espera total.
            var start = Environment.TickCount;
            while (Environment.TickCount - start < ReadyWaitTimeoutMs)
            {
                await Task.Delay(ReadyWaitStepMs);
                if (IsReadyState(gameLoop.CurrentStateIdName))
                {
                    return;
                }
            }

            DebugUtility.LogWarning(typeof(ContentSwapStartPipeline),
                $"[ContentSwapStart] Timeout aguardando GameLoop Ready. state='{gameLoop.CurrentStateIdName}' contentId='{request.ContentId}'.");
        }

        private static bool ShouldRequestReady(string state)
        {
            return string.Equals(state, nameof(GameLoopStateId.Playing), StringComparison.Ordinal)
                || string.Equals(state, nameof(GameLoopStateId.Paused), StringComparison.Ordinal)
                || string.Equals(state, nameof(GameLoopStateId.PostPlay), StringComparison.Ordinal);
        }

        private static bool IsReadyState(string state)
        {
            return string.Equals(state, nameof(GameLoopStateId.Ready), StringComparison.Ordinal)
                || string.Equals(state, nameof(GameLoopStateId.Boot), StringComparison.Ordinal)
                || string.Equals(state, nameof(GameLoopStateId.IntroStage), StringComparison.Ordinal);
        }

        private static IGameLoopService? ResolveGameLoopService()
        {
            return DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service)
                ? service
                : null;
        }

        private static IIntroStageCoordinator? ResolveIntroStageCoordinator()
        {
            return DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var service)
                ? service
                : null;
        }
    }

    public readonly struct ContentSwapStartRequest
    {
        public string ContextSignature { get; }
        public string ContentId { get; }
        public string TargetScene { get; }
        public string Reason { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ContentId);

        public ContentSwapStartRequest(string contextSignature, string contentId, string targetScene, string reason)
        {
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? "<none>" : contextSignature.Trim();
            ContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
            TargetScene = targetScene ?? string.Empty;
            Reason = string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();
        }
    }
}
