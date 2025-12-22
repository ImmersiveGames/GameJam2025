using System;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Driver de produção para acionar reset determinístico do mundo no momento correto do Scene Flow.
    /// Reage ao SceneTransitionScenesReadyEvent garantindo idempotência e bloqueio da simulação durante o reset.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeDriver
    {
        private readonly ISimulationGateService _gateService;
        private readonly object _resetLock = new();

        private Task _ongoingResetTask;
        private string _lastResetContextSignature;

        public WorldLifecycleRuntimeDriver()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService))
            {
                _gateService = gateService;
                DebugUtility.LogVerbose<WorldLifecycleRuntimeDriver>("[WorldLifecycle] ISimulationGateService encontrado no escopo global.");
            }
            else
            {
                DebugUtility.LogWarning<WorldLifecycleRuntimeDriver>("[WorldLifecycle] ISimulationGateService ausente. Reset seguirá sem gate dedicado.");
            }

            var scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnSceneTransitionScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(scenesReadyBinding);

            DebugUtility.LogVerbose<WorldLifecycleRuntimeDriver>("[WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent.");
        }

        /// <summary>
        /// Exposto para permitir reset inicial invocado por orquestradores externos (ex.: GameManager).
        /// </summary>
        public void RequestInitialReset(string reason = null)
        {
            TriggerResetAsync(reason ?? "WorldLifecycleRuntimeDriver/InitialReset", null, "RequestInitialReset");
        }

        /// <summary>
        /// Exposto para hard restart explícito, reutilizando a mesma lógica do reset inicial.
        /// </summary>
        public void RequestHardRestart(string reason = null)
        {
            TriggerResetAsync(reason ?? "WorldLifecycleRuntimeDriver/HardRestart", null, "RequestHardRestart");
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var contextSignature = BuildContextSignature(evt.Context);

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] SceneTransitionScenesReady recebido. Context={evt.Context}");

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] Disparando hard reset após ScenesReady. Context={evt.Context}");

            TriggerResetAsync($"ScenesReady/{evt.Context.targetActiveScene}", contextSignature, evt.Context);
        }

        private void TriggerResetAsync(string reason, string contextSignature, object contextForLog)
        {
            lock (_resetLock)
            {
                if (_ongoingResetTask != null && !_ongoingResetTask.IsCompleted)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Pedido de reset ignorado (já em andamento). reason='{reason}'.");
                    return;
                }

                if (!TryMarkAndStoreContext(contextSignature))
                {
                    DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Reset ignorado (já executado para este contexto). Context={contextForLog}");
                    return;
                }

                _ongoingResetTask = RunResetAsync(reason ?? "WorldLifecycleRuntimeDriver/Reset");
            }
        }

        private async Task RunResetAsync(string reason)
        {
            IDisposable gateHandle = null;

            try
            {
                gateHandle = AcquireGateForReset();

                var controller = ResolveController();
                if (controller == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                        "[WorldLifecycle] WorldLifecycleController não encontrado. Reset abortado.");
                    return;
                }

                await controller.ResetWorldAsync(reason ?? "WorldLifecycleRuntimeDriver");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Erro durante reset: {ex}");
            }
            finally
            {
                try
                {
                    gateHandle?.Dispose();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Falha ao liberar gate: {ex}");
                }

                if (gateHandle != null)
                {
                    DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Gate liberado após reset. token='{SimulationGateTokens.Loading}'.");
                }

                lock (_resetLock)
                {
                    _ongoingResetTask = null;
                }
            }
        }

        private IDisposable AcquireGateForReset()
        {
            if (_gateService == null)
            {
                return null;
            }

            IDisposable gateHandle = null;
            try
            {
                gateHandle = _gateService.Acquire(SimulationGateTokens.Loading);
                DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Gate adquirido para reset. token='{SimulationGateTokens.Loading}'. Active={_gateService.ActiveTokenCount}. IsOpen={_gateService.IsOpen}");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Falha ao adquirir gate para reset: {ex}");
            }

            return gateHandle;
        }

        private WorldLifecycleController ResolveController()
        {
            // Fallback seguro: procurar na cena ativa.
            var activeScene = SceneManager.GetActiveScene().name;
            var foundController = Object.FindFirstObjectByType<WorldLifecycleController>();
            if (foundController == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] WorldLifecycleController não encontrado na cena ativa '{activeScene}'.");
            }

            return foundController;
        }

        private bool TryMarkAndStoreContext(string contextSignature)
        {
            if (string.IsNullOrEmpty(contextSignature))
            {
                return true;
            }

            if (string.Equals(_lastResetContextSignature, contextSignature, StringComparison.Ordinal))
            {
                return false;
            }

            _lastResetContextSignature = contextSignature;
            return true;
        }

        private static string BuildContextSignature(SceneTransitionContext context)
        {
            var targetScene = string.IsNullOrEmpty(context.targetActiveScene) ? "<null>" : context.targetActiveScene;
            var useFade = context.useFade ? "1" : "0";
            var loadPart = BuildListPart(context.scenesToLoad);
            var unloadPart = BuildListPart(context.scenesToUnload);
            var profileId = context.transitionProfile == null ? "<null>" : context.transitionProfile.name;

            return $"Target={targetScene};Fade={useFade};Load=[{loadPart}];Unload=[{unloadPart}];Profile={profileId}";
        }

        private static string BuildListPart(System.Collections.Generic.IReadOnlyList<string> list)
        {
            if (list == null)
            {
                return "<null>";
            }

            var filteredEntries = list.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            return filteredEntries.Length == 0 ? "<empty>" : string.Join("|", filteredEntries);
        }
    }
}
