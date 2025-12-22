using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA
{
    /// <summary>
    /// QA focado em validar o reset do WorldLifecycle com tolerância a ordem de boot.
    /// Usa injeção lazy com retry/timeout para evitar falso negativo quando o bootstrapper ainda não rodou.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleQaTester : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private string label = "WorldLifecycleQATester";
        [SerializeField] private bool autoBindOnStart = true;
        [SerializeField] private float resolveTimeoutSeconds = 1.0f;
        [SerializeField] private float retryIntervalSeconds = 0.05f;
        [SerializeField] private bool verboseLogs = true;

        [Header("Stress")]
        [SerializeField] private int stressResetCount = 3;
        [SerializeField] private float stressResetDelaySeconds = 0.25f;

        [Inject] private WorldLifecycleController _controller;
        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;
        [Inject] private WorldLifecycleHookRegistry _hookRegistry;

        private readonly List<IWorldSpawnService> _spawnServices = new();
        private CancellationTokenSource _lifetimeCts;
        private bool _dependenciesResolved;
        private bool _isResetting;
        private string _sceneName;
        private bool _waitingForBootstrapLogEmitted;

        private static bool IsNewscriptsModeActive =>
#if NEWSCRIPTS_MODE
            true;
#else
            false;
#endif

        private void OnEnable()
        {
            _sceneName = gameObject.scene.name;
            _lifetimeCts = new CancellationTokenSource();
        }

        private void Start()
        {
            if (autoBindOnStart)
            {
                _ = EnsureDependenciesAsync(_lifetimeCts.Token, logSuccess: verboseLogs, logWaiting: verboseLogs);
            }
        }

        private void OnDisable()
        {
            CancelLifetime("OnDisable");
        }

        private void OnDestroy()
        {
            CancelLifetime("OnDestroy");
        }

        [ContextMenu("QA/Reset World Now")]
        public async void ResetWorldNow()
        {
            await RunSingleResetAsync(_lifetimeCts?.Token ?? CancellationToken.None);
        }

        [ContextMenu("QA/Stress Reset (N times)")]
        public async void StressReset()
        {
            await RunStressResetsAsync(_lifetimeCts?.Token ?? CancellationToken.None);
        }

        [ContextMenu("QA/Dump Diagnostics")]
        public void DumpDiagnostics()
        {
            int hookCount = _hookRegistry?.Hooks?.Count ?? -1;

            DebugUtility.Log(typeof(WorldLifecycleQaTester),
                $"[QA] {label}: scene='{_sceneName}', controller={(_controller != null)}, actorRegistry={(_actorRegistry != null)}, " +
                $"spawnRegistry={(_spawnRegistry != null)}, hookRegistry={(_hookRegistry != null ? hookCount.ToString() : "-")}");

            if (hookCount >= 0)
            {
                DebugUtility.Log(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: hook registry contains {hookCount} item(s).");
            }

            if (IsNewscriptsModeActive)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: NEWSCRIPTS_MODE ativo — inicializadores podem ser ignorados (comportamento esperado em modo de desenvolvimento).");
            }
        }

        private async Task RunSingleResetAsync(CancellationToken token)
        {
            if (_isResetting)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: reset ignorado porque outro reset está em andamento.");
                return;
            }

            bool injected = await EnsureDependenciesAsync(token, logSuccess: verboseLogs, logWaiting: verboseLogs);
            if (!injected)
            {
                return;
            }

            if (_actorRegistry == null || _spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: não encontrou registries críticos. Verifique NewSceneBootstrapper na cena.");
                return;
            }

            _isResetting = true;
            try
            {
                var orchestrator = CreateOrchestrator();
                DebugUtility.Log(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: disparando reset único (scene='{_sceneName}').");

                await orchestrator.ResetWorldAsync();

                DebugUtility.Log(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: reset concluído (scene='{_sceneName}').");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: erro durante reset: {ex.Message}");
            }
            finally
            {
                _isResetting = false;
            }
        }

        private async Task RunStressResetsAsync(CancellationToken token)
        {
            bool injected = await EnsureDependenciesAsync(token, logSuccess: verboseLogs, logWaiting: verboseLogs);
            if (!injected)
            {
                return;
            }

            if (stressResetCount <= 0)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: stressResetCount inválido ({stressResetCount}). Nada a fazer.");
                return;
            }

            for (int i = 0; i < stressResetCount; i++)
            {
                if (token.IsCancellationRequested)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleQaTester),
                        $"[QA] {label}: stress reset cancelado (iteration={i}).");
                    return;
                }

                await RunSingleResetAsync(token);

                if (i < stressResetCount - 1)
                {
                    try
                    {
                        int delayMs = Mathf.Max(1, (int)(stressResetDelaySeconds * 1000f));
                        await Task.Delay(delayMs, token);
                    }
                    catch (OperationCanceledException)
                    {
                        DebugUtility.LogWarning(typeof(WorldLifecycleQaTester),
                            $"[QA] {label}: stress reset cancelado durante o delay.");
                        return;
                    }
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleQaTester),
                $"[QA] {label}: stress reset finalizado ({stressResetCount}x).");
        }

        private async Task<bool> EnsureDependenciesAsync(CancellationToken token, bool logSuccess, bool logWaiting = false)
        {
            if (_dependenciesResolved && HasResolvedDependencies())
            {
                return true;
            }

            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.01f, resolveTimeoutSeconds);
            _waitingForBootstrapLogEmitted = false;

            while (Time.realtimeSinceStartup <= deadline && !token.IsCancellationRequested)
            {
                DependencyManager.Provider.InjectDependencies(this);

                if (HasResolvedDependencies())
                {
                    _dependenciesResolved = true;
                    _waitingForBootstrapLogEmitted = false;
                    if (logSuccess)
                    {
                        DebugUtility.Log(typeof(WorldLifecycleQaTester),
                            $"[QA] {label}: dependências resolvidas via lazy injection.");
                    }
                    return true;
                }

                if (logWaiting && !_waitingForBootstrapLogEmitted)
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleQaTester),
                        $"[QA] {label}: aguardando NewSceneBootstrapper registrar serviços de cena (scene='{_sceneName}').");
                    _waitingForBootstrapLogEmitted = true;
                }

                int retryMs = Mathf.Max(1, (int)(retryIntervalSeconds * 1000f));
                try
                {
                    await Task.Delay(retryMs, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            if (!HasResolvedDependencies() && !token.IsCancellationRequested)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQaTester),
                    $"[QA] {label}: falha ao resolver dependências em {resolveTimeoutSeconds:F2}s (scene='{_sceneName}'). " +
                    "Confirme que o NewSceneBootstrapper está ativo na cena e executa antes deste tester (ordem de execução). " +
                    $"controller={(_controller != null)}, gateService={(_gateService != null)}, actorRegistry={(_actorRegistry != null)}, " +
                    $"spawnRegistry={(_spawnRegistry != null)}, hookRegistry={(_hookRegistry != null)}.");
            }

            return false;
        }

        private bool HasResolvedDependencies()
        {
            return _gateService != null
                   && _actorRegistry != null
                   && _spawnRegistry != null
                   && _hookRegistry != null;
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

            if (_spawnRegistry == null || _spawnRegistry.Services == null)
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

        private void CancelLifetime(string reason)
        {
            if (_lifetimeCts == null)
            {
                return;
            }

            if (!_lifetimeCts.IsCancellationRequested)
            {
                _lifetimeCts.Cancel();
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleQaTester),
                        $"[QA] {label}: cancelado ({reason}).");
                }
            }

            _lifetimeCts.Dispose();
            _lifetimeCts = null;
        }
    }
}
