// RealPlanetDefenseWaveRunner.cs

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
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class RealPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private sealed class WaveLoop
        {
            public PlanetsMaster planet;
            public DetectionType detectionType;
            public IDefenseStrategy strategy;
            public DefenseWaveProfileSo waveProfile;
            public ObjectPool pool;
            public CountdownTimer timer;
            public Action timerHandler;
            public bool isActive;
        }

        private readonly Dictionary<PlanetsMaster, WaveLoop> _running = new();
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();

        [Inject] private IPlanetDefensePoolRunner _poolRunner;

        public DependencyInjectionState InjectionState { get; set; }
        public string GetObjectId() => nameof(RealPlanetDefenseWaveRunner);

        public void OnDependenciesInjected() => InjectionState = DependencyInjectionState.Ready;

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType)
            => StartWaves(planet, detectionType, null);

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, IDefenseStrategy strategy)
        {
            if (planet == null) return;

            // Limpa loop anterior, se existir
            if (_running.ContainsKey(planet)) StopWaves(planet);

            strategy ??= ResolveStrategy(planet);

            // Garante configuração do pool
            if (!_poolRunner.TryGetConfiguration(planet, out var context))
            {
                context = new PlanetDefenseSetupContext(planet, detectionType, strategy: strategy);
                _poolRunner.ConfigureForPlanet(context);
            }

            if (!EnsureWaveProfileAvailable(planet, context)) return;

            int interval = Mathf.Max(1, context.WaveProfile.secondsBetweenWaves);
            int spawnCount = Mathf.Max(1, context.WaveProfile.enemiesPerWave);

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] Iniciando defesa em {planet.ActorName} | Intervalo: {interval}s | Minions/Onda: {spawnCount}");

            var pool = GetOrCreatePool(context);
            if (pool == null) return;

            var loop = new WaveLoop
            {
                planet = planet,
                detectionType = context.DetectionType ?? detectionType,
                strategy = strategy,
                waveProfile = context.WaveProfile,
                pool = pool,
                timer = new CountdownTimer(interval),
                isActive = true
            };

            loop.timerHandler = () => {
                if (!loop.isActive) return;
                SpawnWave(loop);
                if (loop.isActive)
                {
                    loop.timer.Reset();
                    loop.timer.Start();
                }
            };

            loop.timer.OnTimerStop += loop.timerHandler;

            // Wave imediata
            SpawnWave(loop);
            loop.timer.Start();

            _running[planet] = loop;
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null || !_running.TryGetValue(planet, out var loop)) return;

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] Parando defesa em {planet.ActorName}");

            loop.isActive = false;
            if (loop.timer != null) loop.timer.OnTimerStop -= loop.timerHandler;
            loop.timer?.Stop();
            DisposeIfPossible(loop.timer);
            _running.Remove(planet);
        }

        public bool IsRunning(PlanetsMaster planet) => planet != null && _running.ContainsKey(planet);

        public void ConfigureStrategy(PlanetsMaster planet, IDefenseStrategy strategy)
            => _strategies[planet] = strategy ?? throw new ArgumentNullException(nameof(strategy));

        public bool TryGetStrategy(PlanetsMaster planet, out IDefenseStrategy strategy)
            => _strategies.TryGetValue(planet, out strategy);

        private ObjectPool GetOrCreatePool(PlanetDefenseSetupContext context)
        {
            var poolData = context.PoolData;
            if (poolData == null)
            {
                DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>("PoolData nulo");
                return null;
            }

            var pool = PoolManager.Instance?.GetPool(poolData.ObjectName);
            if (pool == null)
            {
                _poolRunner.WarmUp(context);
                pool = PoolManager.Instance?.GetPool(poolData.ObjectName);
            }
            return pool;
        }

        private void SpawnWave(WaveLoop loop)
        {
            var profile = loop.waveProfile;
            int count = Mathf.Max(1, profile.enemiesPerWave);
            float radius = profile.spawnRadius;
            float height = profile.spawnHeightOffset;

            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                var offset = ResolveSpawnOffset(radius, height);
                var obj = loop.pool.GetObject(loop.planet.transform.position + offset, loop.planet);
                if (obj != null)
                {
                    spawned++;
                    EventBus<PlanetDefenseMinionSpawnedEvent>.Raise(
                        new PlanetDefenseMinionSpawnedEvent(loop.planet, loop.detectionType, obj));
                }
            }

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[Wave] Spawned {spawned}/{count} minions em {loop.planet.ActorName}");

            loop.strategy?.OnEngaged(loop.planet, loop.detectionType);
        }

        private static Vector3 ResolveSpawnOffset(float radius, float heightOffset)
        {
            if (radius <= 0f && Mathf.Approximately(heightOffset, 0f)) return Vector3.zero;
            var planar = UnityEngine.Random.insideUnitCircle * radius;
            return new Vector3(planar.x, heightOffset, planar.y);
        }

        private static bool EnsureWaveProfileAvailable(PlanetsMaster planet, PlanetDefenseSetupContext context)
        {
            if (context?.WaveProfile != null) return true;
            DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                $"DefenseWaveProfileSO ausente para {planet?.ActorName ?? "Unknown"}");
            return false;
        }

        private IDefenseStrategy ResolveStrategy(PlanetsMaster planet)
            => _strategies.GetValueOrDefault(planet);

        private static void DisposeIfPossible(Timer timer)
        {
            if (timer is IDisposable d) d.Dispose();
        }
    }
}