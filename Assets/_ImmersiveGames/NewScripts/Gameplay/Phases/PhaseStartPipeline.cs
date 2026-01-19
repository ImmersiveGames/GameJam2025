#nullable enable
using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.Phases
{
    [DebugLevel(DebugLevel.Verbose)]
    public static class PhaseStartPipeline
    {
        private const int ReadyWaitTimeoutMs = 2000;
        private const int ReadyWaitStepMs = 50;
        private const int ReadyWaitGraceMs = 250;

        public static async Task RunAsync(PhaseStartRequest request)
        {
            if (!request.IsValid)
            {
                DebugUtility.LogWarning(typeof(PhaseStartPipeline),
                    "[PhaseStart] Request inválido. Pipeline ignorado.");
                return;
            }

            var gameLoop = ResolveGameLoopService();
            if (gameLoop == null)
            {
                DebugUtility.LogWarning(typeof(PhaseStartPipeline),
                    "[PhaseStart] IGameLoopService indisponível; pipeline ignorado.");
                return;
            }

            await EnsureLoopReadyAsync(gameLoop, request);

            // Baseline 2.2: para PhaseChange In-Place (sem transição de cenas) o objetivo é trocar o conteúdo
            // sem reentrar na IntroStage (que bloquearia gameplay e exigiria confirmação/UI).
            // A IntroStage permanece válida para entradas via SceneFlow/Completed (entrada do gameplay) e
            // para outros fluxos que explicitamente desejem conteúdo antes de começar.
            if (ShouldSuppressIntroStageForRequest(request))
            {
                DebugUtility.LogVerbose(typeof(PhaseStartPipeline),
                    $"[PhaseStart] IntroStage suprimida. phaseId='{request.PhaseId}' reason='{request.Reason}'.");
                return;
            }

            var coordinator = ResolveIntroStageCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning(typeof(PhaseStartPipeline),
                    "[PhaseStart] IIntroStageCoordinator indisponível; IntroStage não será executada.");
                return;
            }

            var targetScene = string.IsNullOrWhiteSpace(request.TargetScene)
                ? SceneManager.GetActiveScene().name
                : request.TargetScene.Trim();

            DebugUtility.Log(typeof(PhaseStartPipeline),
                $"[OBS][Phase] PhaseStartPipeline -> IntroStage. phaseId='{request.PhaseId}' signature='{request.ContextSignature}' " +
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
                DebugUtility.LogWarning(typeof(PhaseStartPipeline),
                    $"[PhaseStart] Falha ao executar IntroStage. phaseId='{request.PhaseId}', ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static bool ShouldSuppressIntroStageForRequest(PhaseStartRequest request)
        {
            // Heurística mínima e determinística: apenas suprime para QA explícito de In-Place.
            // Isso evita alterar comportamento de produção e mantém a evidência rastreável via reason.
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return false;
            }

            // O reason do pipeline é formado como: "PhaseStart/Committed|phaseId=...|reason=<commitReason>".
            // Para o TC-G01-INPLACE, o commitReason começa com "QA/Phases/InPlace/".
            return request.Reason.Contains("|reason=QA/Phases/InPlace/", StringComparison.Ordinal)
                || request.Reason.StartsWith("QA/Phases/InPlace/", StringComparison.Ordinal);
        }

        private static async Task EnsureLoopReadyAsync(IGameLoopService gameLoop, PhaseStartRequest request)
        {
            var stateBefore = gameLoop.CurrentStateIdName ?? string.Empty;
            if (ShouldRequestReady(stateBefore))
            {
                DebugUtility.LogVerbose(typeof(PhaseStartPipeline),
                    $"[PhaseStart] RequestReady antes da IntroStage. state='{stateBefore}' phaseId='{request.PhaseId}'.");
                gameLoop.RequestReady();
            }

            if (IsReadyState(gameLoop.CurrentStateIdName))
            {
                return;
            }

            // Observação: para troca de fase, o GameLoop pode permanecer em 'Playing' sem transitar para 'Ready'.
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
                DebugUtility.LogVerbose(typeof(PhaseStartPipeline),
                    $"[PhaseStart] Prosseguindo sem aguardar Ready (estado='{stateAfterGrace}'). phaseId='{request.PhaseId}'.");
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

            DebugUtility.LogWarning(typeof(PhaseStartPipeline),
                $"[PhaseStart] Timeout aguardando GameLoop Ready. state='{gameLoop.CurrentStateIdName}' phaseId='{request.PhaseId}'.");
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

    public readonly struct PhaseStartRequest
    {
        public string ContextSignature { get; }
        public string PhaseId { get; }
        public string TargetScene { get; }
        public string Reason { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(PhaseId);

        public PhaseStartRequest(string contextSignature, string phaseId, string targetScene, string reason)
        {
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? "<none>" : contextSignature.Trim();
            PhaseId = string.IsNullOrWhiteSpace(phaseId) ? string.Empty : phaseId.Trim();
            TargetScene = targetScene ?? string.Empty;
            Reason = string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();
        }
    }
}
