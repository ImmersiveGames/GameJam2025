using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow;
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
            provider.TryGetGlobal<ISimulationGateService>(out var previousGate);
            provider.TryGetGlobal<GameReadinessService>(out var previousReadiness);

            var gateService = ResolveOrInstallGate(provider);
            var readiness = ResolveOrInstallReadiness(provider, gateService);
            var previousService = ResolveExistingService(provider);

            var stubLoader = new StubSceneFlowLoaderAdapter();
            var stubFade = new StubSceneFlowFadeAdapter();
            var service = new SceneTransitionService(stubLoader, stubFade);
            provider.RegisterGlobal<ISceneTransitionService>(service, allowOverride: true);

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
                var request = new SceneTransitionRequest(
                    new[] { "QA_Load_A" },
                    new[] { "QA_Unload_B" },
                    targetActiveScene: "QA_TargetActive",
                    useFade: true,
                    transitionProfileName: "QA_Profile");

                service.TransitionAsync(request).GetAwaiter().GetResult();

                Evaluate(eventOrder.Count == 3, "Eventos foram emitidos (3 marcos).", ref passes, ref fails);
                Evaluate(eventOrder.Count >= 1 && eventOrder[0] == "Started", "Primeiro evento é Started.", ref passes, ref fails);
                Evaluate(eventOrder.Count >= 2 && eventOrder[1] == "ScenesReady", "Segundo evento é ScenesReady.", ref passes, ref fails);
                Evaluate(eventOrder.Count >= 3 && eventOrder[2] == "Completed", "Terceiro evento é Completed.", ref passes, ref fails);

                Evaluate(stubLoader.LoadCalls.Contains("QA_Load_A"), "Loader adapter recebeu LoadSceneAsync para QA_Load_A.", ref passes, ref fails);
                Evaluate(stubLoader.UnloadCalls.Contains("QA_Unload_B"), "Loader adapter recebeu UnloadSceneAsync para QA_Unload_B.", ref passes, ref fails);
                Evaluate(string.Equals(stubLoader.ActiveScene, "QA_TargetActive", StringComparison.Ordinal),
                    "Loader adapter recebeu TrySetActiveSceneAsync para QA_TargetActive.", ref passes, ref fails);
                Evaluate(string.Equals(stubFade.ConfiguredProfileName, "QA_Profile", StringComparison.Ordinal),
                    "Fade adapter configurado com TransitionProfileName.", ref passes, ref fails);
                Evaluate(stubFade.FadeInCount == 1 && stubFade.FadeOutCount == 1,
                    "Fade adapter executou FadeIn e FadeOut exatamente uma vez.", ref passes, ref fails);

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
                RestoreService(provider, previousService);
                RestoreReadinessAndGate(provider, previousGate, previousReadiness);
            }

            DebugUtility.Log(typeof(SceneTransitionServiceSmokeQATester),
                $"[QA][SceneFlow] QA complete. Passes={passes} Fails={fails}",
                fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (fails > 0)
            {
                throw new InvalidOperationException($"SceneTransitionServiceSmokeQATester detected {fails} failures.");
            }
        }

        private static ISimulationGateService ResolveOrInstallGate(IDependencyProvider provider)
        {
            if (provider.TryGetGlobal<ISimulationGateService>(out var gate) && gate != null)
            {
                return gate;
            }

            var newGate = new SimulationGateService();
            provider.RegisterGlobal<ISimulationGateService>(newGate, allowOverride: true);

            DebugUtility.LogVerbose(typeof(SceneTransitionServiceSmokeQATester),
                "[QA][SceneFlow] ISimulationGateService não encontrado. SimulationGateService de teste registrado.");
            return newGate;
        }

        private static GameReadinessService ResolveOrInstallReadiness(IDependencyProvider provider, ISimulationGateService gate)
        {
            if (provider.TryGetGlobal<GameReadinessService>(out var readiness) && readiness != null)
            {
                return readiness;
            }

            if (gate == null)
            {
                DebugUtility.LogWarning(typeof(SceneTransitionServiceSmokeQATester),
                    "[QA][SceneFlow] Gate indisponível; GameReadinessService não será inicializado para o teste.");
                return null;
            }

            var readinessService = new GameReadinessService(gate);
            provider.RegisterGlobal(readinessService, allowOverride: true);

            DebugUtility.LogVerbose(typeof(SceneTransitionServiceSmokeQATester),
                "[QA][SceneFlow] GameReadinessService não encontrado. Instância de teste registrada.");
            return readinessService;
        }

        private static ISceneTransitionService ResolveExistingService(IDependencyProvider provider)
        {
            if (provider.TryGetGlobal<ISceneTransitionService>(out var registered) && registered != null)
            {
                return registered;
            }
            return null;
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

        private static void RestoreService(IDependencyProvider provider, ISceneTransitionService previousService)
        {
            if (previousService != null)
            {
                provider.RegisterGlobal(previousService, allowOverride: true);
                return;
            }

            var loader = LegacySceneFlowAdapters.CreateLoaderAdapter(provider);
            var fade = LegacySceneFlowAdapters.CreateFadeAdapter(provider);
            var service = new SceneTransitionService(loader, fade);
            provider.RegisterGlobal<ISceneTransitionService>(service, allowOverride: true);
        }

        private static void RestoreReadinessAndGate(
            IDependencyProvider provider,
            ISimulationGateService previousGate,
            GameReadinessService previousReadiness)
        {
            if (previousGate != null)
            {
                provider.RegisterGlobal(previousGate, allowOverride: true);
            }

            if (previousReadiness != null)
            {
                provider.RegisterGlobal(previousReadiness, allowOverride: true);
            }
        }

        private sealed class StubSceneFlowLoaderAdapter : ISceneFlowLoaderAdapter
        {
            public List<string> LoadCalls { get; } = new();
            public List<string> UnloadCalls { get; } = new();
            public string ActiveScene { get; private set; }
            private readonly HashSet<string> _loadedScenes = new(StringComparer.Ordinal);

            public StubSceneFlowLoaderAdapter()
            {
                _loadedScenes.Add("QA_Unload_B"); // marca como carregada para exercitar Unload
            }

            public Task LoadSceneAsync(string sceneName)
            {
                LoadCalls.Add(sceneName);
                _loadedScenes.Add(sceneName);
                return Task.CompletedTask;
            }

            public Task UnloadSceneAsync(string sceneName)
            {
                UnloadCalls.Add(sceneName);
                _loadedScenes.Remove(sceneName);
                return Task.CompletedTask;
            }

            public bool IsSceneLoaded(string sceneName) => _loadedScenes.Contains(sceneName);

            public Task<bool> TrySetActiveSceneAsync(string sceneName)
            {
                ActiveScene = sceneName;
                return Task.FromResult(true);
            }

            public string GetActiveSceneName() => ActiveScene ?? string.Empty;
        }

        private sealed class StubSceneFlowFadeAdapter : ISceneFlowFadeAdapter
        {
            public int FadeInCount { get; private set; }
            public int FadeOutCount { get; private set; }
            public string ConfiguredProfileName { get; private set; }

            public bool IsAvailable => true;

            public void ConfigureFromProfile(string profileName)
            {
                ConfiguredProfileName = profileName;
            }

            public Task FadeInAsync()
            {
                FadeInCount++;
                return Task.CompletedTask;
            }

            public Task FadeOutAsync()
            {
                FadeOutCount++;
                return Task.CompletedTask;
            }
        }
    }
}
