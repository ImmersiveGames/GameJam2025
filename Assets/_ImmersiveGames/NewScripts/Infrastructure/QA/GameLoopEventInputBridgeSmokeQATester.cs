using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke test mínimo para validar o fluxo de eventos → GameLoop via GameLoopEventInputBridge.
    /// Importante: cria uma instância NOVA do bridge após injetar o stub no DI, para evitar cache do serviço antigo.
    /// </summary>
    public sealed class GameLoopEventInputBridgeSmokeQATester : MonoBehaviour
    {
        [ContextMenu("QA/GameLoop/Input Bridge/Run")]
        public void Run()
        {
            int passes = 0;
            int fails = 0;

            var provider = DependencyManager.Provider;

            // Salva serviço anterior (se houver)
            provider.TryGetGlobal(out IGameLoopService previousService);

            // Substitui por stub ANTES de criar o bridge (evita cache do service real)
            var stubService = new StubGameLoopService();
            provider.RegisterGlobal<IGameLoopService>(stubService, allowOverride: true);

            // Sempre cria um bridge novo para capturar o stub corretamente
            GameLoopEventInputBridge bridge = null;

            try
            {
                bridge = new GameLoopEventInputBridge();

                PublishSignals();

                Evaluate(stubService.StartRequested, "Start event routed to RequestStart()", ref passes, ref fails);
                Evaluate(stubService.PauseRequested, "Pause event routed to RequestPause()", ref passes, ref fails);

                // Alguns bridges tratam GamePauseEvent(false) como resume; outros só tratam GameResumeRequestedEvent.
                // Este smoke exige pelo menos 1 resume.
                Evaluate(stubService.ResumeRequestedCount >= 1,
                    "Resume routed (expected at least one: pause=false and/or resume request)", ref passes, ref fails);

                Evaluate(stubService.ResetRequested, "Reset event routed to RequestReset()", ref passes, ref fails);

                if (stubService.ResumeRequestedCount == 1)
                {
                    DebugUtility.Log(typeof(GameLoopEventInputBridgeSmokeQATester),
                        "[QA][GameLoopBridge] INFO - Resume ocorreu apenas 1x (provável: apenas GameResumeRequestedEvent foi mapeado).",
                        DebugUtility.Colors.Info);
                }
            }
            catch (Exception ex)
            {
                fails++;
                DebugUtility.LogError(typeof(GameLoopEventInputBridgeSmokeQATester),
                    $"[QA][GameLoopBridge] FAIL - Exceção inesperada: {ex}");
            }
            finally
            {
                try
                {
                    bridge?.Dispose();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning(typeof(GameLoopEventInputBridgeSmokeQATester),
                        $"[QA][GameLoopBridge] WARN - Dispose do bridge lançou exceção: {ex.GetType().Name}: {ex.Message}");
                }

                RestoreService(provider, previousService);
            }

            DebugUtility.Log(typeof(GameLoopEventInputBridgeSmokeQATester),
                $"[QA][GameLoopBridge] QA complete. Passes={passes} Fails={fails}",
                fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (fails > 0)
            {
                throw new InvalidOperationException($"GameLoopEventInputBridgeSmokeQATester detected {fails} failures.");
            }
        }

        private static void PublishSignals()
        {
            EventBus<GameStartEvent>.Raise(new GameStartEvent());
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(true));
            EventBus<GamePauseEvent>.Raise(new GamePauseEvent(false));
            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
        }

        private static void Evaluate(bool condition, string message, ref int passes, ref int fails)
        {
            if (condition)
            {
                DebugUtility.Log(typeof(GameLoopEventInputBridgeSmokeQATester),
                    $"[QA][GameLoopBridge] PASS - {message}", DebugUtility.Colors.Success);
                passes++;
            }
            else
            {
                DebugUtility.LogError(typeof(GameLoopEventInputBridgeSmokeQATester),
                    $"[QA][GameLoopBridge] FAIL - {message}");
                fails++;
            }
        }

        private static void RestoreService(IDependencyProvider provider, IGameLoopService previousService)
        {
            if (previousService != null)
            {
                provider.RegisterGlobal(previousService, allowOverride: true);
                return;
            }

            // fallback padrão
            var fallback = new GameLoopService();
            fallback.Initialize();
            provider.RegisterGlobal<IGameLoopService>(fallback, allowOverride: true);
        }

        private sealed class StubGameLoopService : IGameLoopService
        {
            public bool StartRequested { get; private set; }
            public bool PauseRequested { get; private set; }
            public bool ResetRequested { get; private set; }
            public int ResumeRequestedCount { get; private set; }

            public string CurrentStateName => string.Empty;

            public void Initialize() { }
            public void Tick(float dt) { }

            public void RequestStart() => StartRequested = true;
            public void RequestPause() => PauseRequested = true;
            public void RequestResume() => ResumeRequestedCount++;
            public void RequestReset() => ResetRequested = true;

            public void Dispose() { }
        }
    }
}
