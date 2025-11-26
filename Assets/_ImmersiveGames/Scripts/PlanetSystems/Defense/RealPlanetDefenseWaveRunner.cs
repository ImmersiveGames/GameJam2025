using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que gerencia corrotinas de ondas e spawn real via PoolSystem.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RealPlanetDefenseWaveRunner : MonoBehaviour, IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private readonly Dictionary<PlanetsMaster, Coroutine> _running = new();
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();

        [Inject] private PlanetDefenseSpawnConfig config = new();
        [Inject] private IPlanetDefensePoolRunner poolRunner;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(RealPlanetDefenseWaveRunner);

        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            config ??= new PlanetDefenseSpawnConfig();
        }

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType)
        {
            StartWaves(planet, detectionType, null);
        }

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.ContainsKey(planet))
            {
                return;
            }

            strategy ??= ResolveStrategy(planet);

            if (!poolRunner.TryGetConfiguration(planet, out var context))
            {
                context = new PlanetDefenseSetupContext(planet, detectionType, strategy: strategy);
                poolRunner.ConfigureForPlanet(context);
            }

            var poolName = DefensePoolNaming.GetPoolName(planet, context);
            var pool = PoolManager.Instance?.GetPool(poolName);
            if (pool == null)
            {
                poolRunner.WarmUp(context);
                pool = PoolManager.Instance?.GetPool(poolName);
            }

            if (pool == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>($"Pool '{poolName}' indisponível para {planet.ActorName}.");
                return;
            }

            var routine = StartCoroutine(RunWaves(planet, detectionType, pool, strategy));
            _running[planet] = routine;
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.TryGetValue(planet, out var routine))
            {
                StopCoroutine(routine);
                _running.Remove(planet);
            }
        }

        public bool IsRunning(PlanetsMaster planet)
        {
            return planet != null && _running.ContainsKey(planet);
        }

        public void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy)
        {
            if (planet == null || strategy == null)
            {
                return;
            }

            _strategies[planet] = strategy;
        }

        public bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy)
        {
            return _strategies.TryGetValue(planet, out strategy);
        }

        private System.Collections.IEnumerator RunWaves(PlanetsMaster planet, DetectionType detectionType, ObjectPool pool, IDefenseStrategy strategy)
        {
            var waitBetweenWaves = new WaitForSeconds(config.DebugWaveDurationSeconds);
            while (true)
            {
                SpawnWave(planet, detectionType, pool, strategy);
                yield return waitBetweenWaves;
            }
        }

        private void SpawnWave(PlanetsMaster planet, DetectionType detectionType, ObjectPool pool, IDefenseStrategy strategy)
        {
            var spawnPosition = planet.transform.position;
            for (int i = 0; i < config.DebugWaveSpawnCount; i++)
            {
                var poolable = pool.GetObject(spawnPosition, planet);
                if (poolable == null)
                {
                    DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>($"Falha ao obter minion da pool para {planet.ActorName}.");
                    continue;
                }

                EventBus<PlanetDefenseMinionSpawnedEvent>.Raise(new PlanetDefenseMinionSpawnedEvent(planet, detectionType, poolable));
            }

            strategy?.OnEngaged(planet, detectionType);
        }

        private IDefenseStrategy ResolveStrategy(PlanetsMaster planet)
        {
            return _strategies.TryGetValue(planet, out var strategy) ? strategy : null;
        }
    }
}
