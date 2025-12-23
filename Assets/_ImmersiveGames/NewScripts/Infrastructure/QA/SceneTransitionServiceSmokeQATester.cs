using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke QA para o pipeline nativo de Scene Flow (NewScripts).
    /// Valida ordem dos eventos e integração com GameReadinessService/SimulationGate.
    /// </summary>
    public sealed class SceneTransitionServiceSmokeQATester : MonoBehaviour
    {
        [ContextMenu("QA/SceneFlow/SceneTransitionService/Run")]
        public void Run()
        {
            int passes = 0;
            int fails = 0;

            var provider = DependencyManager.Provider;
            var readiness = ResolveReadiness(provider);
            var gateService = ResolveGate(provider);
            var service = ResolveSceneTransitionService(provider);

            var eventOrder = new List<string>();
            bool gameplayReadyAfterStart = true;
            bool gameplayReadyAfterCompleted = false;
            bool gateOpenAfterStart = true;
            bool gateOpenAfterCompleted = false;

            var startedBinding = new EventBinding<SceneTransitionStartedEvent>(_ =>
            {
                eventOrder.Add("Started");
                gameplayReadyAfterStart = readiness?.IsGameplayReady ?? true;
                gateOpenAfterStart = gateService?.IsOpen ?? true;
            });
            var scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(_ =>
            {
                eventOrder.Add("ScenesReady");
            });
            var completedBinding = new EventBinding<SceneTransitionCompletedEvent>(_ =>
            {
                eventOrder.Add("Completed");
                gameplayReadyAfterCompleted = readiness?.IsGameplayReady ?? false;
                gateOpenAfterCompleted = gateService?.IsOpen ?? false;
            });

            EventBus<SceneTransitionStartedEvent>.Register(startedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(scenesReadyBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(completedBinding);

            try
            {
                var activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                var request = new SceneTransitionRequest(
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    targetActiveScene: string.IsNullOrWhiteSpace(activeSceneName) ? null : activeSceneName,
                    useFade: false);

                service.TransitionAsync(request).GetAwaiter().GetResult();

                Evaluate(eventOrder.Count == 3, "Eventos foram emitidos (3 marcos).", ref passes, ref fails);
                Evaluate(eventOrder.Count >= 1 && eventOrder[0] == "Started", "Primeiro evento é Started.", ref passes, ref fails);
                Evaluate(eventOrder.Count >= 2 && eventOrder[1] == "ScenesReady", "Segundo evento é ScenesReady.", ref passes, ref fails);
                Evaluate(eventOrder.Count >= 3 && eventOrder[2] == "Completed", "Terceiro evento é Completed.", ref passes, ref fails);

                Evaluate(!gameplayReadyAfterStart, "GameReadinessService marca gameplay como NOT READY após Started.", ref passes, ref fails);
                Evaluate(gateOpenAfterStart == false, "SimulationGate fecha durante transição (após Started).", ref passes, ref fails);
                Evaluate(gameplayReadyAfterCompleted, "GameReadinessService marca gameplay como READY após Completed.", ref passes, ref fails);
                Evaluate(gateOpenAfterCompleted, "SimulationGate reabre após Completed.", ref passes, ref fails);
            }
            catch (Exception ex)
            {
                fails++;
                DebugUtility.LogError(typeof(SceneTransitionServiceSmokeQATester),
                    $"[QA][SceneFlow] FAIL - Exceção inesperada: {ex}");
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(startedBinding);
                EventBus<SceneTransitionScenesReadyEvent>.Unregister(scenesReadyBinding);
                EventBus<SceneTransitionCompletedEvent>.Unregister(completedBinding);
            }

            DebugUtility.Log(typeof(SceneTransitionServiceSmokeQATester),
                $"[QA][SceneFlow] QA complete. Passes={passes} Fails={fails}",
                fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (fails > 0)
            {
                throw new InvalidOperationException($"SceneTransitionServiceSmokeQATester detected {fails} failures.");
            }
        }

        private static GameReadinessService ResolveReadiness(IDependencyProvider provider)
        {
            if (provider.TryGetGlobal<GameReadinessService>(out var readiness) && readiness != null)
            {
                return readiness;
            }

            DebugUtility.LogWarning(typeof(SceneTransitionServiceSmokeQATester),
                "[QA][SceneFlow] GameReadinessService indisponível. Asserções de readiness serão ignoradas.");
            return null;
        }

        private static ISimulationGateService ResolveGate(IDependencyProvider provider)
        {
            if (provider.TryGetGlobal<ISimulationGateService>(out var gate) && gate != null)
            {
                return gate;
            }

            DebugUtility.LogWarning(typeof(SceneTransitionServiceSmokeQATester),
                "[QA][SceneFlow] ISimulationGateService indisponível. Asserções de gate serão ignoradas.");
            return null;
        }

        private static ISceneTransitionService ResolveSceneTransitionService(IDependencyProvider provider)
        {
            if (provider.TryGetGlobal<ISceneTransitionService>(out var registered) && registered != null)
            {
                return registered;
            }

            DebugUtility.LogWarning(typeof(SceneTransitionServiceSmokeQATester),
                "[QA][SceneFlow] ISceneTransitionService não encontrado no DI global. Criando instância fallback.");

            var loader = new SceneManagerLoaderAdapter();
            var fade = new NullFadeAdapter();
            var service = new SceneTransitionService(loader, fade);
            provider.RegisterGlobal<ISceneTransitionService>(service, allowOverride: true);
            return service;
        }

        private static void Evaluate(bool condition, string message, ref int passes, ref int fails)
        {
            if (condition)
            {
                DebugUtility.Log(typeof(SceneTransitionServiceSmokeQATester),
                    $"[QA][SceneFlow] PASS - {message}", DebugUtility.Colors.Success);
                passes++;
            }
            else
            {
                DebugUtility.LogError(typeof(SceneTransitionServiceSmokeQATester),
                    $"[QA][SceneFlow] FAIL - {message}");
                fails++;
            }
        }
    }
}
