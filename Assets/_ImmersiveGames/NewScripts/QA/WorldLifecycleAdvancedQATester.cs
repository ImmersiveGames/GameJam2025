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
    /// <summary>
    /// QA avançado do WorldLifecycle:
    /// - Ordem determinística de spawn services (A->B->C como subsequência)
    /// - Falha em B durante SpawnAsync => C não executa spawn
    /// - Gate sempre é liberado (recovery)
    /// - Reset subsequente após remover B recupera o mundo (ActorRegistry volta ao esperado)
    ///
    /// IMPORTANTE:
    /// - Não injeta no Awake, pois os serviços de cena podem não ter sido registrados ainda.
    /// - Aguarda scene-scope ficar pronto (com timeout) antes de executar.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleAdvancedQATester : MonoBehaviour
    {
        [Header("Execution")]
        [SerializeField] private bool autoRunOnStart = true;

        [Tooltip("Se verdadeiro, lança exceção ao detectar falha (útil para CI/QA).")]
        [SerializeField] private bool failFast = true;

        [Tooltip("Timeout (ms) para aguardar o scene-scope registrar serviços necessários.")]
        [SerializeField] private int dependencyWaitTimeoutMs = 3000;

        [Header("Logging")]
        [SerializeField] private string label = "WorldLifecycleAdvancedQATester";

        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;
        [Inject] private WorldLifecycleHookRegistry _hookRegistry;

        private bool _dependenciesInjected;
        private string _sceneName;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            // IMPORTANT: Não injetar aqui. Scene services podem não estar registrados ainda.
        }

        private async void Start()
        {
            if (!autoRunOnStart)
            {
                return;
            }

            await RunAllAsync();
        }

        [ContextMenu("QA/Run All (Advanced WorldLifecycle)")]
        public async void RunAll()
        {
            await RunAllAsync();
        }

        private async Task RunAllAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                $"[QA] {label}: start (scene='{_sceneName}')");

            await EnsureSceneDependenciesReadyAsync();

            if (!HasDependencies())
            {
                Fail("[QA] Missing dependencies; aborting QA.");
                return;
            }

            try
            {
                await Test_Order_ShortCircuit_And_GateRecovery_WhenSpawnServiceThrowsAsync();
                await Test_RecoveryReset_AfterRemovingFaultyServiceAsync();

                DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                    $"[QA] {label}: ✅ All advanced tests passed");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleAdvancedQATester),
                    $"[QA] {label}: ❌ FAILED: {ex}", this);

                if (failFast)
                {
                    throw;
                }
            }
        }

        private async Task EnsureSceneDependenciesReadyAsync()
        {
            // 1) Aguarda pelo menos 1 frame para permitir que bootstrappers rodem.
            await Task.Yield();

            // 2) Tenta injetar com retry até o scene-scope estar pronto ou estourar timeout.
            var start = Time.realtimeSinceStartup;

            while (true)
            {
                TryInject();

                // Gate é global e costuma estar disponível cedo; registry/actor/hook são de cena.
                var hasSceneDeps = _spawnRegistry != null && _actorRegistry != null;
                if (hasSceneDeps)
                {
                    // _hookRegistry pode ser opcional, mas se existir, melhor.
                    if (_hookRegistry == null)
                    {
                        DebugUtility.LogWarning(typeof(WorldLifecycleAdvancedQATester),
                            $"[QA] {label}: WorldLifecycleHookRegistry ainda não encontrado (scene='{_sceneName}'). Hooks via registry ficarão desativados.");
                    }

                    return;
                }

                var elapsedMs = (int)((Time.realtimeSinceStartup - start) * 1000f);
                if (elapsedMs > Mathf.Max(250, dependencyWaitTimeoutMs))
                {
                    DebugUtility.LogError(typeof(WorldLifecycleAdvancedQATester),
                        $"[QA] {label}: timeout aguardando serviços de cena. " +
                        $"spawnRegistry={(_spawnRegistry != null)}, actorRegistry={(_actorRegistry != null)}, hookRegistry={(_hookRegistry != null)}",
                        this);
                    return;
                }

                // Evita loop apertado no mesmo frame.
                await Task.Yield();
            }
        }

        private void TryInject()
        {
            if (_dependenciesInjected)
            {
                // Mesmo se já “injetou”, vale tentar novamente caso o injector suporte preencher campos nulos.
                DependencyManager.Provider.InjectDependencies(this);
                return;
            }

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;
        }

        private bool HasDependencies()
        {
            var ok = true;

            if (_gateService == null)
            {
                ok = false;
                DebugUtility.LogError(typeof(WorldLifecycleAdvancedQATester),
                    $"[QA] {label}: ISimulationGateService não injetado.", this);
            }

            if (_spawnRegistry == null)
            {
                ok = false;
                DebugUtility.LogError(typeof(WorldLifecycleAdvancedQATester),
                    $"[QA] {label}: IWorldSpawnServiceRegistry não injetado.", this);
            }

            if (_actorRegistry == null)
            {
                ok = false;
                DebugUtility.LogError(typeof(WorldLifecycleAdvancedQATester),
                    $"[QA] {label}: IActorRegistry não injetado.", this);
            }

            if (_hookRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleAdvancedQATester),
                    $"[QA] {label}: WorldLifecycleHookRegistry não injetado. Hooks via registry ficarão desativados.");
            }

            return ok;
        }

        private async Task Test_Order_ShortCircuit_And_GateRecovery_WhenSpawnServiceThrowsAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                $"[QA] {label}: Test 1 - Order + Short-circuit + Gate recovery (SpawnService throws)");

            var baseline = SnapshotBaselineServices();
            AssertTrue(baseline.Count > 0,
                "Expected at least one baseline spawn service (e.g., DummyActorSpawnService) in registry.");

            var trace = new TraceBuffer();

            // A, B (throws on Spawn), C
            var svcA = new QAOrderedSpawnService("QA_A", trace, throwOnSpawn: false);
            var svcB = new QAOrderedSpawnService("QA_B", trace, throwOnSpawn: true);
            var svcC = new QAOrderedSpawnService("QA_C", trace, throwOnSpawn: false);

            var services = new List<IWorldSpawnService>(baseline.Count + 3);
            services.AddRange(baseline);
            services.Add(svcA);
            services.Add(svcB);
            services.Add(svcC);

            var orchestrator = CreateOrchestrator(services);

            var failedAsExpected = false;
            try
            {
                await orchestrator.ResetWorldAsync();
                failedAsExpected = false;
            }
            catch
            {
                failedAsExpected = true;
            }

            AssertTrue(failedAsExpected,
                "Expected orchestrator reset to throw due to QA_B failing during SpawnAsync.");

            // Gate token must be released even on exception.
            AssertGateReleased();

            // Validate order (relative) and short-circuit:
            AssertTraceContains(trace, "QA_A.Spawn.Start");
            AssertTraceContains(trace, "QA_B.Spawn.Start");
            AssertTraceNotContains(trace, "QA_C.Spawn.Start");

            AssertTraceOrder(trace, "QA_A.Spawn.Start", "QA_B.Spawn.Start",
                "Expected QA_A spawn to start before QA_B spawn.");

            // Despawn relative ordering among QA services (they should run in list order).
            AssertTraceContains(trace, "QA_A.Despawn.Start");
            AssertTraceContains(trace, "QA_B.Despawn.Start");
            AssertTraceContains(trace, "QA_C.Despawn.Start");

            AssertTraceOrder(trace, "QA_A.Despawn.Start", "QA_B.Despawn.Start",
                "Expected QA_A despawn to start before QA_B despawn.");
            AssertTraceOrder(trace, "QA_B.Despawn.Start", "QA_C.Despawn.Start",
                "Expected QA_B despawn to start before QA_C despawn.");

            DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                $"[QA] {label}: Test 1 passed");
        }

        private async Task Test_RecoveryReset_AfterRemovingFaultyServiceAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                $"[QA] {label}: Test 2 - Recovery reset after removing faulty SpawnService");

            var baseline = SnapshotBaselineServices();
            AssertTrue(baseline.Count > 0,
                "Expected at least one baseline spawn service (e.g., DummyActorSpawnService) in registry.");

            var trace = new TraceBuffer();

            // Recovery list: baseline + A + C (no B)
            var svcA = new QAOrderedSpawnService("QA_A_RECOVERY", trace, throwOnSpawn: false);
            var svcC = new QAOrderedSpawnService("QA_C_RECOVERY", trace, throwOnSpawn: false);

            var services = new List<IWorldSpawnService>(baseline.Count + 2);
            services.AddRange(baseline);
            services.Add(svcA);
            services.Add(svcC);

            var orchestrator = CreateOrchestrator(services);

            Exception failure = null;
            try
            {
                await orchestrator.ResetWorldAsync();
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            AssertTrue(failure == null,
                $"Expected recovery reset to succeed, but it threw: {failure}");

            AssertGateReleased();

            // Com seu baseline atual (DummyActor), após reset bem-sucedido Count deve ser 1.
            AssertTrue(_actorRegistry != null, "ActorRegistry is null during recovery assertion.");

            AssertTrue(_actorRegistry.Count == 1,
                $"Expected ActorRegistry.Count == 1 after recovery reset, got {_actorRegistry.Count}.");

            DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                $"[QA] {label}: Test 2 passed");
        }

        private List<IWorldSpawnService> SnapshotBaselineServices()
        {
            var list = new List<IWorldSpawnService>();

            if (_spawnRegistry?.Services == null || _spawnRegistry.Services.Count == 0)
            {
                return list;
            }

            foreach (var svc in _spawnRegistry.Services)
            {
                if (svc != null)
                {
                    list.Add(svc);
                }
            }

            return list;
        }

        private WorldLifecycleOrchestrator CreateOrchestrator(IReadOnlyList<IWorldSpawnService> services)
        {
            return new WorldLifecycleOrchestrator(
                _gateService,
                services,
                _actorRegistry,
                provider: DependencyManager.Provider,
                sceneName: _sceneName,
                hookRegistry: _hookRegistry);
        }

        private void AssertGateReleased()
        {
            if (_gateService == null)
            {
                return;
            }

            var active = _gateService.IsTokenActive(WorldLifecycleTokens.WorldResetToken);
            AssertTrue(!active,
                $"Expected gate token '{WorldLifecycleTokens.WorldResetToken}' to be released, but it is still active.");

            DebugUtility.Log(typeof(WorldLifecycleAdvancedQATester),
                $"[QA] {label}: gate token released; IsOpen={_gateService.IsOpen}");
        }

        private static void AssertTraceContains(TraceBuffer trace, string entry)
        {
            if (!trace.Contains(entry))
            {
                throw new InvalidOperationException($"[QA] Trace missing expected entry: {entry}\nTrace:\n{trace}");
            }
        }

        private static void AssertTraceNotContains(TraceBuffer trace, string entry)
        {
            if (trace.Contains(entry))
            {
                throw new InvalidOperationException($"[QA] Trace contains unexpected entry: {entry}\nTrace:\n{trace}");
            }
        }

        private static void AssertTraceOrder(TraceBuffer trace, string first, string second, string message)
        {
            var i1 = trace.IndexOf(first);
            var i2 = trace.IndexOf(second);

            if (i1 < 0 || i2 < 0)
            {
                throw new InvalidOperationException(
                    $"[QA] Trace order check missing entries '{first}' or '{second}'.\nTrace:\n{trace}");
            }

            if (i1 >= i2)
            {
                throw new InvalidOperationException(
                    $"[QA] {message}\n(firstIndex={i1}, secondIndex={i2})\nTrace:\n{trace}");
            }
        }

        private void AssertTrue(bool condition, string messageIfFalse)
        {
            if (condition)
            {
                return;
            }

            Fail("[QA] " + messageIfFalse);
        }

        private void Fail(string message)
        {
            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            DebugUtility.LogError(typeof(WorldLifecycleAdvancedQATester), message, this);
        }

        /// <summary>
        /// SpawnService QA que registra uma trilha (trace) determinística e pode falhar no Spawn.
        /// </summary>
        private sealed class QAOrderedSpawnService : IWorldSpawnService
        {
            private readonly TraceBuffer _trace;
            private readonly bool _throwOnSpawn;

            public string Name { get; }

            public QAOrderedSpawnService(string name, TraceBuffer trace, bool throwOnSpawn)
            {
                Name = string.IsNullOrWhiteSpace(name) ? nameof(QAOrderedSpawnService) : name;
                _trace = trace ?? throw new ArgumentNullException(nameof(trace));
                _throwOnSpawn = throwOnSpawn;
            }

            public Task DespawnAsync()
            {
                _trace.Add($"{Name}.Despawn.Start");
                _trace.Add($"{Name}.Despawn.End");
                return Task.CompletedTask;
            }

            public Task SpawnAsync()
            {
                _trace.Add($"{Name}.Spawn.Start");

                if (_throwOnSpawn)
                {
                    throw new InvalidOperationException($"[QA] {Name} forced failure at SpawnAsync");
                }

                _trace.Add($"{Name}.Spawn.End");
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Buffer simples de trace, preservando ordem de inserção.
        /// </summary>
        private sealed class TraceBuffer
        {
            private readonly List<string> _entries = new(64);

            public void Add(string entry)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    return;
                }

                _entries.Add(entry);
                DebugUtility.LogVerbose(typeof(TraceBuffer), $"[QA][Trace] {entry}");
            }

            public bool Contains(string entry) => IndexOf(entry) >= 0;

            public int IndexOf(string entry)
            {
                if (_entries.Count == 0)
                {
                    return -1;
                }

                for (int i = 0; i < _entries.Count; i++)
                {
                    if (string.Equals(_entries[i], entry, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }

                return -1;
            }

            public override string ToString()
            {
                return _entries.Count == 0 ? "<empty>" : string.Join("\n", _entries);
            }
        }
    }
}
