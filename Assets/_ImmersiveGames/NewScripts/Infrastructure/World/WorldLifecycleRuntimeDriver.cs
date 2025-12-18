using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Driver de produção para acionar reset determinístico do mundo no momento correto do Scene Flow.
    /// Reage ao SceneTransitionScenesReadyEvent garantindo idempotência e bloqueio da simulação durante o reset.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class WorldLifecycleRuntimeDriver
    {
        private readonly IDependencyProvider _provider;
        private readonly ISimulationGateService _gateService;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly object _resetLock = new();

        private Task _ongoingResetTask;
        private string _lastResetContextSignature;

        public WorldLifecycleRuntimeDriver()
        {
            _provider = DependencyManager.Provider;

            if (_provider.TryGetGlobal<ISimulationGateService>(out var gateService))
            {
                _gateService = gateService;
                DebugUtility.LogVerbose<WorldLifecycleRuntimeDriver>("[WorldLifecycle] ISimulationGateService encontrado no escopo global.");
            }
            else
            {
                DebugUtility.LogWarning<WorldLifecycleRuntimeDriver>("[WorldLifecycle] ISimulationGateService ausente. Reset seguirá sem gate dedicado.");
            }

            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnSceneTransitionScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);

            DebugUtility.LogVerbose<WorldLifecycleRuntimeDriver>("[WorldLifecycle] Runtime driver registrado para SceneTransitionScenesReadyEvent.");
        }

        /// <summary>
        /// Exposto para permitir reset inicial invocado por orquestradores externos (ex.: GameManager).
        /// </summary>
        public void RequestInitialReset(string reason = null)
        {
            TriggerResetAsync(reason ?? "WorldLifecycleRuntimeDriver/InitialReset", null);
        }

        /// <summary>
        /// Exposto para hard restart explícito, reutilizando a mesma lógica do reset inicial.
        /// </summary>
        public void RequestHardRestart(string reason = null)
        {
            TriggerResetAsync(reason ?? "WorldLifecycleRuntimeDriver/HardRestart", null);
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var contextSignature = BuildContextSignature(evt.Context);

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] SceneTransitionScenesReady recebido. Context={evt.Context}");

            if (!TryMarkContext(contextSignature))
            {
                DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] Reset ignorado (já executado para este contexto). Context={evt.Context}");
                return;
            }

            DebugUtility.Log(typeof(WorldLifecycleRuntimeDriver),
                $"[WorldLifecycle] Disparando hard reset após ScenesReady. Context={evt.Context}");

            TriggerResetAsync($"ScenesReady/{evt.Context.targetActiveScene}", contextSignature);
        }

        private void TriggerResetAsync(string reason, string contextSignature)
        {
            lock (_resetLock)
            {
                if (_ongoingResetTask != null && !_ongoingResetTask.IsCompleted)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                        $"[WorldLifecycle] Pedido de reset ignorado (já em andamento). reason='{reason}'.");
                    return;
                }

                if (!string.IsNullOrEmpty(contextSignature))
                {
                    _lastResetContextSignature = contextSignature;
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
            // Preferir serviços registrados no escopo da cena alvo.
            var activeScene = SceneManager.GetActiveScene().name;
            if (_provider != null && _provider.TryGetForScene<WorldLifecycleController>(activeScene, out var injectedController) && injectedController != null)
            {
                return injectedController;
            }

            // Fallback seguro: procurar na cena ativa.
            var foundController = Object.FindFirstObjectByType<WorldLifecycleController>();
            if (foundController == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleRuntimeDriver),
                    $"[WorldLifecycle] WorldLifecycleController não encontrado via DI nem na cena ativa '{activeScene}'.");
            }

            return foundController;
        }

        private bool TryMarkContext(string contextSignature)
        {
            if (string.IsNullOrEmpty(contextSignature))
            {
                return true;
            }

            if (string.Equals(_lastResetContextSignature, contextSignature, StringComparison.Ordinal))
            {
                return false;
            }
            return true;
        }

        private static string BuildContextSignature(SceneTransitionContext context)
        {
            // Usa ToString() imutável do context para identificar transições de forma determinística.
            return context.ToString();
        }
    }
}
