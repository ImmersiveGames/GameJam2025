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
    /// Runner concreto que gerencia waves de defesa com <see cref="IntervalTimer"/>,
    /// usando o ScriptableObject como fonte única para cadência (secondsBetweenWaves)
    /// e quantidade de inimigos por wave.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class RealPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private readonly struct WaveSettings
        {
            public WaveSettings(int secondsBetweenWaves, int enemiesPerWave, float spawnRadius, float spawnHeightOffset)
            {
                SecondsBetweenWaves = secondsBetweenWaves;
                EnemiesPerWave = enemiesPerWave;
                SpawnRadius = spawnRadius;
                SpawnHeightOffset = spawnHeightOffset;
            }

            public int SecondsBetweenWaves { get; }
            public int EnemiesPerWave { get; }
            public float SpawnRadius { get; }
            public float SpawnHeightOffset { get; }
        }

        private sealed class WaveLoop
        {
            public IntervalTimer Timer;
            public Action Tick;
            public int SecondsBetweenWaves;
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

            var waveSettings = ResolveWaveSettings(context);

            // Dispara a primeira wave imediatamente.
            SpawnWave(planet, resolvedDetection, pool, strategy, context, waveSettings);

            // Configura o timer de cadência com IntervalTimer (segundos entre waves).
            var loop = BuildWaveLoop(planet, resolvedDetection, pool, strategy, context, waveSettings);

            loop.Timer.OnInterval += loop.Tick;
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
                    loop.Timer.OnInterval -= loop.Tick;
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
            PlanetDefenseSetupContext context,
            WaveSettings waveSettings)
        {
            if (planet == null || pool == null)
            {
                return;
            }

            var spawnPosition = planet.transform.position;
            int spawns = waveSettings.EnemiesPerWave;
            float radius = Mathf.Max(0f, waveSettings.SpawnRadius);
            float heightOffset = waveSettings.SpawnHeightOffset;
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

        private void TickWave(
            PlanetsMaster planet,
            DetectionType detectionType,
            ObjectPool pool,
            IDefenseStrategy strategy,
            PlanetDefenseSetupContext context,
            WaveSettings waveSettings)
        {
            if (planet == null || pool == null)
            {
                return;
            }

            SpawnWave(planet, detectionType, pool, strategy, context, waveSettings);
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

        private WaveLoop BuildWaveLoop(
            PlanetsMaster planet,
            DetectionType detectionType,
            ObjectPool pool,
            IDefenseStrategy strategy,
            PlanetDefenseSetupContext context,
            WaveSettings waveSettings)
        {
            var loop = new WaveLoop
            {
                SecondsBetweenWaves = waveSettings.SecondsBetweenWaves
            };

            loop.Tick = () =>
            {
                TickWave(planet, detectionType, pool, strategy, context, waveSettings);
                loop.Timer.Reset(loop.SecondsBetweenWaves);
                loop.Timer.Start();
            };
            // IntervalTimer usa segundos inteiros entre ticks (cada tick = uma wave).
            loop.Timer = new IntervalTimer(loop.SecondsBetweenWaves, loop.SecondsBetweenWaves);
            return loop;
        }

        private static WaveSettings ResolveWaveSettings(PlanetDefenseSetupContext context)
        {
            var profile = context?.WaveProfile;
            if (profile == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                    "DefenseWaveProfileSO ausente no contexto; usando defaults seguros (5s entre waves, 6 inimigos).");
                return new WaveSettings(5, 6, 0f, 0f);
            }

            var secondsBetweenWaves = Mathf.Max(1, profile.secondsBetweenWaves);
            var enemiesPerWave = Mathf.Max(1, profile.enemiesPerWave);
            var spawnRadius = profile.spawnRadius;
            var spawnHeightOffset = profile.spawnHeightOffset;

            return new WaveSettings(secondsBetweenWaves, enemiesPerWave, spawnRadius, spawnHeightOffset);
        }

        private static void DisposeIfPossible(IntervalTimer timer)
        {
            if (timer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
