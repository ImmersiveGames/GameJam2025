using System;
using System.Collections.Generic;
using ImprovedTimers;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner concreto que gerencia ondas de spawn utilizando FrequencyTimer
    /// para reduzir overhead e facilitar pausa/retomada.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class RealPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private sealed class WaveLoop
        {
            public FrequencyTimer Timer;
            public Action Tick;
        }

        private readonly Dictionary<PlanetsMaster, WaveLoop> _running = new();
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();

        [Inject] private IPlanetDefensePoolRunner poolRunner;

        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => nameof(RealPlanetDefenseWaveRunner);

        public void OnDependenciesInjected() => InjectionState = DependencyInjectionState.Ready;

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

            var resolvedDetection = context.DetectionType ?? detectionType;

            var poolData = context.PoolData;
            if (poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>($"PoolData missing for planet {planet.ActorName}; unable to start waves.");
                return;
            }

            var poolName = poolData.ObjectName;
            var pool = PoolManager.Instance?.GetPool(poolName);
            if (pool == null)
            {
                poolRunner.WarmUp(context);
                pool = PoolManager.Instance?.GetPool(poolName);
            }

            if (pool == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>($"Pool '{poolName}' unavailable for {planet.ActorName}.");
                return;
            }

            SpawnWave(planet, resolvedDetection, pool, strategy, context);

            var intervalSeconds = ResolveIntervalSeconds(context);
            var loop = new WaveLoop();

            loop.Tick = () => TickWave(planet, resolvedDetection, pool, strategy, context);
            loop.Timer = new FrequencyTimer(intervalSeconds);
            loop.Timer.OnTick += loop.Tick;
            loop.Timer.Start();

            _running[planet] = loop;
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.TryGetValue(planet, out var loop))
            {
                if (loop.Timer != null && loop.Tick != null)
                {
                    loop.Timer.OnTick -= loop.Tick;
                }

                loop.Timer?.Stop();
                DisposeIfPossible(loop.Timer);
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

        private void SpawnWave(
            PlanetsMaster planet,
            DetectionType detectionType,
            ObjectPool pool,
            IDefenseStrategy strategy,
            PlanetDefenseSetupContext context)
        {
            if (planet == null || pool == null)
            {
                return;
            }

            var spawnPosition = planet.transform.position;
            int spawns = ResolveSpawnCount(context);
            float radius = Mathf.Max(0f, context?.WaveProfile?.spawnRadius ?? 0f);
            float heightOffset = context?.WaveProfile?.spawnHeightOffset ?? 0f;
            for (int i = 0; i < spawns; i++)
            {
                var offset = ResolveSpawnOffset(radius, heightOffset);
                var poolable = pool.GetObject(spawnPosition + offset, planet);
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

        private static int ResolveSpawnCount(PlanetDefenseSetupContext context)
        {
            return Mathf.Max(1, context?.WaveProfile?.minionsPerWave ?? 6);
        }

        private static int ResolveIntervalSeconds(PlanetDefenseSetupContext context)
        {
            var rawInterval = context?.WaveProfile?.waveIntervalSeconds ?? 5;
            return Mathf.Max(1, rawInterval);
        }

        private void TickWave(
            PlanetsMaster planet,
            DetectionType detectionType,
            ObjectPool pool,
            IDefenseStrategy strategy,
            PlanetDefenseSetupContext context)
        {
            if (planet == null || pool == null)
            {
                return;
            }

            SpawnWave(planet, detectionType, pool, strategy, context);
        }

        private static Vector3 ResolveSpawnOffset(float radius, float heightOffset)
        {
            if (radius <= 0f && Mathf.Approximately(heightOffset, 0f))
            {
                return Vector3.zero;
            }

            var planar = UnityEngine.Random.insideUnitCircle * radius;
            return new Vector3(planar.x, heightOffset, planar.y);
        }

        private static void DisposeIfPossible(FrequencyTimer timer)
        {
            if (timer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
