using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleQATester : MonoBehaviour
    {
        [Header("Execution")]
        [SerializeField] private bool autoRunOnStart = false;

        [Tooltip("Se > 0, adiciona um hook de cena que atrasa artificialmente para validar warnings de hook lento.")]
        [SerializeField] private int artificialSceneHookDelayMs = 0;

        [Tooltip("Se verdadeiro, falha o QA ao detectar qualquer condição inesperada.")]
        [SerializeField] private bool failFast = true;

        [Tooltip("Timeout (ms) para aguardar reset concluir (ActorRegistry estabilizar).")]
        [SerializeField] private int resetAwaitTimeoutMs = 1500;

        [Tooltip("Frames mínimos a aguardar após disparar Reset (evita falso-timeout quando tudo ocorre no mesmo frame).")]
        [SerializeField] private int minFramesAfterReset = 2;

        private string _sceneName = string.Empty;
        private bool _dependenciesInjected;

        private WorldLifecycleController _lifecycleController;

        [Inject] private IActorRegistry _actorRegistry;
        [Inject] private WorldLifecycleHookRegistry _hookRegistry;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
        }

        private async void Start()
        {
            if (!autoRunOnStart)
            {
                return;
            }

            await RunAllAsync();
        }

        [ContextMenu("QA/Run All (WorldLifecycle)")]
        public async void RunAll()
        {
            await RunAllAsync();
        }

        private async Task RunAllAsync()
        {
            EnsureInjected();
            EnsureControllerLocated();

            DebugUtility.Log(typeof(WorldLifecycleQATester), $"[QA] Start (scene='{_sceneName}')");

            if (!HasCriticalDeps())
            {
                return;
            }

            try
            {
                EnsureQaSceneHooks();

                await Test_ResetInitial_SpawnsOneActorAsync();
                await Test_ResetConsecutive_ReplacesActorAsync();
                await Test_ResetConsecutive_StableAsync(rounds: 3);

                DebugUtility.Log(typeof(WorldLifecycleQATester), "[QA] ✅ All tests passed");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester), $"[QA] ❌ FAILED: {ex}", this);
                if (failFast)
                {
                    throw;
                }
            }
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

        private void EnsureControllerLocated()
        {
            if (_lifecycleController != null)
            {
                return;
            }

            _lifecycleController = FindFirstObjectByType<WorldLifecycleController>();
            if (_lifecycleController == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQATester),
                    "[QA] WorldLifecycleController não encontrado via FindFirstObjectByType. " +
                    "Certifique-se de que existe um WorldLifecycleController ativo na cena.");
            }
        }

        private bool HasCriticalDeps()
        {
            var ok = true;

            if (_lifecycleController == null)
            {
                ok = false;
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    "[QA] WorldLifecycleController não encontrado. Adicione um na cena para rodar os testes.", this);
            }

            if (_actorRegistry == null)
            {
                ok = false;
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    "[QA] IActorRegistry não injetado/encontrado. O NewSceneBootstrapper deve registrar o ActorRegistry.", this);
            }

            if (_hookRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQATester),
                    "[QA] WorldLifecycleHookRegistry não injetado. Sentinelas de hook de cena serão ignoradas.");
            }

            return ok;
        }

        private void EnsureQaSceneHooks()
        {
            if (_hookRegistry == null)
            {
                return;
            }

            if (ContainsHookOfType<QASceneLifecycleHookSentinel>(_hookRegistry.Hooks))
            {
                return;
            }

            var sentinel = new QASceneLifecycleHookSentinel(
                artificialDelayMs: Mathf.Max(0, artificialSceneHookDelayMs));

            _hookRegistry.Register(sentinel);

            DebugUtility.LogVerbose(typeof(WorldLifecycleQATester),
                $"[QA] Scene hook sentinel registered (delayMs={artificialSceneHookDelayMs}).");
        }

        private static bool ContainsHookOfType<T>(IReadOnlyList<IWorldLifecycleHook> hooks)
        {
            if (hooks == null || hooks.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < hooks.Count; i++)
            {
                if (hooks[i] is T)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task Test_ResetInitial_SpawnsOneActorAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleQATester), "[QA] Test 1: Initial reset spawns exactly 1 actor");

            var before = _actorRegistry.Count;
            DebugUtility.Log(typeof(WorldLifecycleQATester), $"[QA] ActorRegistry before reset: {before}");

            await InvokeControllerResetAsync();

            var after = _actorRegistry.Count;
            DebugUtility.Log(typeof(WorldLifecycleQATester), $"[QA] ActorRegistry after reset: {after}");

            AssertTrue(after == 1,
                $"Expected ActorRegistry.Count == 1 after initial reset, got {after}.");
        }

        private async Task Test_ResetConsecutive_ReplacesActorAsync()
        {
            DebugUtility.Log(typeof(WorldLifecycleQATester), "[QA] Test 2: Consecutive reset replaces actor (ID changes)");

            var firstId = SnapshotSingleActorId();
            AssertTrue(!string.IsNullOrWhiteSpace(firstId),
                "Expected a valid actor id after initial reset.");

            await InvokeControllerResetAsync();

            var secondId = SnapshotSingleActorId();
            AssertTrue(!string.IsNullOrWhiteSpace(secondId),
                "Expected a valid actor id after consecutive reset.");

            AssertTrue(firstId != secondId,
                $"Expected actor id to change after consecutive reset. First='{firstId}' Second='{secondId}'.");

            AssertTrue(_actorRegistry.Count == 1,
                $"Expected ActorRegistry.Count == 1 after consecutive reset, got {_actorRegistry.Count}.");
        }

        private async Task Test_ResetConsecutive_StableAsync(int rounds)
        {
            rounds = Mathf.Max(1, rounds);
            DebugUtility.Log(typeof(WorldLifecycleQATester), $"[QA] Test 3: Stability across {rounds} consecutive resets");

            string lastId = SnapshotSingleActorId();

            for (int i = 0; i < rounds; i++)
            {
                await InvokeControllerResetAsync();

                var id = SnapshotSingleActorId();
                AssertTrue(!string.IsNullOrWhiteSpace(id), $"Round {i}: Expected actor id.");

                AssertTrue(id != lastId, $"Round {i}: Expected actor id to change. Prev='{lastId}' New='{id}'.");

                AssertTrue(_actorRegistry.Count == 1,
                    $"Round {i}: Expected ActorRegistry.Count == 1, got {_actorRegistry.Count}.");

                lastId = id;
            }
        }

        private string SnapshotSingleActorId()
        {
            var list = new List<IActor>(4);
            _actorRegistry.GetActors(list);

            if (list.Count == 0)
            {
                return string.Empty;
            }

            if (list.Count > 1)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQATester),
                    $"[QA] Expected single actor, but registry returned {list.Count}. Using first for ID checks.");
            }

            return list[0]?.ActorId ?? string.Empty;
        }

        private async Task InvokeControllerResetAsync()
        {
            if (_lifecycleController == null)
            {
                throw new InvalidOperationException("[QA] WorldLifecycleController is null.");
            }

            _lifecycleController.ResetWorldNow();

            await WaitForRegistryStabilizeAsync(
                minFrames: Mathf.Max(1, minFramesAfterReset),
                stableFramesRequired: 2,
                timeoutMs: Mathf.Max(250, resetAwaitTimeoutMs));
        }

        private async Task WaitForRegistryStabilizeAsync(int minFrames, int stableFramesRequired, int timeoutMs)
        {
            var start = Time.realtimeSinceStartup;

            // 1) Aguarda alguns frames mínimos para evitar o caso onde tudo ocorre no mesmo frame do Reset.
            for (int i = 0; i < minFrames; i++)
            {
                await Task.Yield();

                var elapsedMs0 = (int)((Time.realtimeSinceStartup - start) * 1000f);
                if (elapsedMs0 > timeoutMs)
                {
                    throw new TimeoutException($"[QA] Timeout waiting initial frames after reset. timeoutMs={timeoutMs}");
                }
            }

            // 2) Aguarda estabilização do Count por N frames consecutivos.
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
                    if (stableFrames >= Mathf.Max(1, stableFramesRequired))
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

        private void AssertTrue(bool condition, string messageIfFalse)
        {
            if (condition)
            {
                return;
            }

            if (failFast)
            {
                throw new InvalidOperationException("[QA] " + messageIfFalse);
            }

            DebugUtility.LogError(typeof(WorldLifecycleQATester), "[QA] " + messageIfFalse, this);
        }

        private sealed class QASceneLifecycleHookSentinel : IWorldLifecycleHook
        {
            private readonly int _artificialDelayMs;

            public QASceneLifecycleHookSentinel(int artificialDelayMs)
            {
                _artificialDelayMs = artificialDelayMs;
            }

            public async Task OnBeforeDespawnAsync()
            {
                DebugUtility.LogVerbose(typeof(QASceneLifecycleHookSentinel), "[QA] SceneHook -> OnBeforeDespawn");
                await DelayIfNeeded();
            }

            public async Task OnAfterDespawnAsync()
            {
                DebugUtility.LogVerbose(typeof(QASceneLifecycleHookSentinel), "[QA] SceneHook -> OnAfterDespawn");
                await DelayIfNeeded();
            }

            public async Task OnBeforeSpawnAsync()
            {
                DebugUtility.LogVerbose(typeof(QASceneLifecycleHookSentinel), "[QA] SceneHook -> OnBeforeSpawn");
                await DelayIfNeeded();
            }

            public async Task OnAfterSpawnAsync()
            {
                DebugUtility.LogVerbose(typeof(QASceneLifecycleHookSentinel), "[QA] SceneHook -> OnAfterSpawn");
                await DelayIfNeeded();
            }

            private async Task DelayIfNeeded()
            {
                if (_artificialDelayMs <= 0)
                {
                    return;
                }

                await Task.Delay(_artificialDelayMs);
            }
        }
    }
}
