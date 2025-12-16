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
        [SerializeField] private string label = "WorldLifecycleQATester";

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
            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;
        }

        [ContextMenu("QA/Run Gate Recovery Test")]
        public async void RunGateRecoveryTest()
        {
            DebugUtility.Log(typeof(WorldLifecycleQATester),
                $"[QA] {label}: starting gate recovery scenario in scene '{_sceneName}'.");

            if (!HasDependencies())
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: missing dependencies; aborting.");
                return;
            }

            var faultyHook = new QAFaultySceneLifecycleHook("Faulty", FaultyLifecyclePhase.BeforeSpawn);
            _hookRegistry.Register(faultyHook);

            var orchestrator = CreateOrchestrator();

            var failedAsExpected = await RunFaultyResetAsync(orchestrator);
            ValidateGateReleased();

            _hookRegistry.Unregister(faultyHook);

            await RunRecoveryResetAsync(orchestrator, failedAsExpected);
        }

        public async Task InvokeControllerResetAsync()
        {
            if (!HasDependencies())
            {
                throw new InvalidOperationException("[QA] Missing dependencies for reset invocation.");
            }

            var orchestrator = CreateOrchestrator();
            await orchestrator.ResetWorldAsync();
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

        private async Task RunRecoveryResetAsync(WorldLifecycleOrchestrator orchestrator, bool failedAsExpected)
        {
            if (!failedAsExpected)
            {
                DebugUtility.Log(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: skipping recovery reset because faulty reset did not throw.");
                return;
            }

            try
            {
                await orchestrator.ResetWorldAsync();
                DebugUtility.Log(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: recovery reset completed after removing faulty hook.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleQATester),
                    $"[QA] {label}: recovery reset still failing: {ex}");
            }
        }

        private void ValidateGateReleased()
        {
            if (_gateService == null)
            {
                return;
            }

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

        private bool HasDependencies()
        {
            if (_dependenciesInjected)
            {
                return _spawnRegistry != null && _actorRegistry != null && _hookRegistry != null && _gateService != null;
            }

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;

            return _spawnRegistry != null && _actorRegistry != null && _hookRegistry != null && _gateService != null;
        }
    }
}
