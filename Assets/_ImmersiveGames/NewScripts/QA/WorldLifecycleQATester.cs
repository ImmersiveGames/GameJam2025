using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleQATester : MonoBehaviour
    {
        [Header("QA")]
        [SerializeField] private string label = "WorldLifecycleQATester";
        [SerializeField] private bool autoRunOnStart = false;

        [Header("Timing")]
        [Tooltip("Frames mínimos a aguardar após disparar Reset (evita falso-timeout quando tudo ocorre no mesmo frame).")]
        [SerializeField] private int minFramesAfterReset = 2;

        [Tooltip("Timeout (ms) para aguardar reset concluir (ActorRegistry estabilizar).")]
        [SerializeField] private int resetAwaitTimeoutMs = 1500;

        [Tooltip("Frames consecutivos com Count estável necessários para considerar 'estável'.")]
        [SerializeField] private int stableFramesRequired = 2;

        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;
        [Inject] private WorldLifecycleHookRegistry _hookRegistry;

        private readonly List<IWorldSpawnService> _spawnServices = new();
        private bool _dependenciesInjected;
        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            // Não injetar aqui: serviços de cena podem ainda não ter sido registrados.
        }

        private async void Start()
        {
            if (!autoRunOnStart)
            {
                return;
            }

            await RunGateRecoveryScenarioAsync();
        }

        [ContextMenu("QA/Run Gate Recovery Test")]
        public async void RunGateRecoveryTest()
        {
            await RunGateRecoveryScenarioAsync();
        }

        public async Task RunAllAsync()
        {
            // No NewScripts, mantemos este QA focado no cenário de gate recovery.
            await RunGateRecoveryScenarioAsync();
        }

        private async Task RunGateRecoveryScenarioAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleQATester),
                $"[QA] {label}: starting gate recovery scenario in scene '{_sceneName}'.");

            EnsureInjected();

            if (!HasCriticalDependencies())
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: missing dependencies; aborting.");
                return;
            }

            await Test_GateRecovery_WhenSceneHookThrowsAsync();
        }

        private void EnsureInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;
        }

        private bool HasCriticalDependencies()
        {
            // Gate NÃO é crítico para existir (orchestrator suporta null),
            // mas para o teste de "gate recovery" ele será validado separadamente.
            return _spawnRegistry != null && _actorRegistry != null && _hookRegistry != null;
        }

        private WorldLifecycleOrchestrator CreateOrchestrator()
        {
            BuildSpawnServices();

            return new WorldLifecycleOrchestrator(
                _gateService,
                _spawnServices,
                _actorRegistry,
                provider: DependencyManager.Provider,
                sceneName: _sceneName,
                hookRegistry: _hookRegistry);
        }

        private void BuildSpawnServices()
        {
            _spawnServices.Clear();

            if (_spawnRegistry == null)
            {
                return;
            }

            foreach (var service in _spawnRegistry.Services)
            {
                if (service != null)
                {
                    _spawnServices.Add(service);
                }
            }
        }

        private async Task Test_GateRecovery_WhenSceneHookThrowsAsync()
        {
            if (_hookRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: hook registry missing; cannot run gate recovery test.");
                return;
            }

            if (_gateService == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: ISimulationGateService not injected. Test will still validate recovery reset, but cannot assert token release.");
            }

            var faultyHook = new QAFaultySceneLifecycleHook("FaultyHook", FaultyLifecyclePhase.BeforeSpawn);
            _hookRegistry.Register(faultyHook);

            var failedAsExpected = false;

            try
            {
                // 1) Rodar reset com hook defeituoso -> deve falhar
                var orchestrator = CreateOrchestrator();
                failedAsExpected = await RunFaultyResetAsync(orchestrator);

                // 2) Validar token liberado (se o gate existir)
                ValidateGateReleased();
            }
            finally
            {
                _hookRegistry.Unregister(faultyHook);
            }

            if (!failedAsExpected)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: faulty reset unexpectedly succeeded; skipping recovery assertion.");
                return;
            }

            // 3) Rodar reset normal (sem hook defeituoso) e validar que não travou e estabilizou
            await RunRecoveryResetAndAssertAsync();
        }

        private async Task<bool> RunFaultyResetAsync(WorldLifecycleOrchestrator orchestrator)
        {
            try
            {
                await orchestrator.ResetWorldAsync();

                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: faulty reset finished without throwing.");
                return false;
            }
            catch (Exception ex)
            {
                DebugUtility.Log(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: faulty reset failed as expected: {ex.Message}");
                return true;
            }
        }

        private void ValidateGateReleased()
        {
            if (_gateService == null)
            {
                return;
            }

            // Assumindo que existe esta API no seu gate service (você usou no QA).
            // Se essa API não existir, a correção é: expor uma propriedade/consulta equivalente no gate.
            var isTokenActive = _gateService.IsTokenActive(WorldLifecycleTokens.WorldResetToken);

            if (isTokenActive)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: gate token '{WorldLifecycleTokens.WorldResetToken}' is still active after faulty reset.");
            }
            else
            {
                DebugUtility.Log(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: gate token released after faulty reset; IsOpen={_gateService.IsOpen}.");
            }
        }

        private async Task RunRecoveryResetAndAssertAsync()
        {
            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: actor registry missing; cannot assert recovery count.");
                return;
            }

            var orchestrator = CreateOrchestrator();
            await orchestrator.ResetWorldAsync();

            await WaitForRegistryStabilizeAsync(
                minFrames: Mathf.Max(1, minFramesAfterReset),
                stableFramesRequired: Mathf.Max(1, stableFramesRequired),
                timeoutMs: Mathf.Max(250, resetAwaitTimeoutMs));

            if (_actorRegistry.Count != 1)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: expected one actor after recovery reset but found {_actorRegistry.Count}.");
            }
            else
            {
                DebugUtility.Log(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: recovery reset restored actor registry to expected state (count=1).");
            }
        }

        private async Task WaitForRegistryStabilizeAsync(int minFrames, int stableFramesRequired, int timeoutMs)
        {
            var start = Time.realtimeSinceStartup;

            // 1) Aguarda frames mínimos
            for (int i = 0; i < minFrames; i++)
            {
                await Task.Yield();

                var elapsedMs0 = (int)((Time.realtimeSinceStartup - start) * 1000f);
                if (elapsedMs0 > timeoutMs)
                {
                    throw new TimeoutException($"[QA] Timeout waiting initial frames after reset. timeoutMs={timeoutMs}");
                }
            }

            // 2) Aguarda estabilização do Count por N frames consecutivos
            var stableFrames = 0;
            var lastCount = _actorRegistry != null ? _actorRegistry.Count : -1;

            while (true)
            {
                await Task.Yield();

                var elapsedMs = (int)((Time.realtimeSinceStartup - start) * 1000f);
                if (elapsedMs > timeoutMs)
                {
                    throw new TimeoutException($"[QA] Timeout waiting for registry stabilize. lastCount={lastCount}, timeoutMs={timeoutMs}");
                }

                var current = _actorRegistry != null ? _actorRegistry.Count : -1;

                if (current == lastCount)
                {
                    stableFrames++;
                    if (stableFrames >= stableFramesRequired)
                    {
                        return;
                    }
                }
                else
                {
                    stableFrames = 0;
                    lastCount = current;
                }
            }
        }
    }
}
