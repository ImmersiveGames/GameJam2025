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
    /// </summary>
    public sealed class GameLoopEventInputBridgeSmokeQATester : MonoBehaviour
    {
        [ContextMenu("QA/GameLoop/Input Bridge/Run")]
        public void Run()
        {
            int passes = 0;
            int fails = 0;

            var provider = DependencyManager.Provider;
            provider.TryGetGlobal(out IGameLoopService previousService);

            var stubService = new StubGameLoopService();
            provider.RegisterGlobal<IGameLoopService>(stubService, allowOverride: true);

            GameLoopEventInputBridge bridge = ResolveBridge(out bool ownsBridge);

            try
            {
                PublishSignals();

                Evaluate(stubService.StartRequested, "Start event routed to RequestStart()", ref passes, ref fails);
                Evaluate(stubService.PauseRequested, "Pause event routed to RequestPause()", ref passes, ref fails);
                Evaluate(stubService.ResumeRequestedCount >= 2, "Resume routed by pause=false and resume request", ref passes, ref fails);
                Evaluate(stubService.ResetRequested, "Reset event routed to RequestReset()", ref passes, ref fails);
            }
            catch (Exception ex)
            {
                fails++;
                DebugUtility.LogError(typeof(GameLoopEventInputBridgeSmokeQATester),
                    $"[QA][GameLoopBridge] FAIL - Exceção inesperada: {ex}");
            }
            finally
            {
                if (ownsBridge)
                {
                    bridge?.Dispose();
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

        private static GameLoopEventInputBridge ResolveBridge(out bool ownsBridge)
        {
            ownsBridge = false;

            if (DependencyManager.Provider.TryGetGlobal<GameLoopEventInputBridge>(out var existing) && existing != null)
            {
                return existing;
            }

            ownsBridge = true;
            return new GameLoopEventInputBridge();
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
