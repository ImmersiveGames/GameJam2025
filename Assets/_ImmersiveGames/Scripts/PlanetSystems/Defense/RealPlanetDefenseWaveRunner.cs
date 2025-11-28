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
    /// Runner concreto que gerencia waves de spawn utilizando CountdownTimer
    /// para reduzir overhead e facilitar pausa/retomada com cadência em segundos.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class RealPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private sealed class WaveLoop
        {
            public PlanetsMaster Planet;
            public DetectionType DetectionType;
            public IDefenseStrategy Strategy;
            public DefenseWaveProfileSO WaveProfile;
            public ObjectPool Pool;
            public CountdownTimer Timer;
            public Action TimerHandler;
            public bool IsActive;
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

            // Garantir que qualquer loop anterior seja encerrado antes de iniciar um novo para o mesmo planeta.
            if (_running.ContainsKey(planet))
            {
                StopWaves(planet);
            }

            strategy ??= ResolveStrategy(planet);
            if (!poolRunner.TryGetConfiguration(planet, out var context))
            {
                context = new PlanetDefenseSetupContext(planet, detectionType, strategy: strategy);
                poolRunner.ConfigureForPlanet(context);
            }

            if (!EnsureWaveProfileAvailable(planet, context))
            {
                return;
            }

            var resolvedDetection = context.DetectionType ?? detectionType;
            var intervalSeconds = ResolveIntervalSeconds(context);
            var spawnCount = ResolveSpawnCount(context);

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[WaveDebug] StartWaves em {planet.ActorName} | Intervalo: {intervalSeconds}s | Minions/Onda: {spawnCount}.");

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

            var loop = new WaveLoop
            {
                Planet = planet,
                DetectionType = resolvedDetection,
                Strategy = strategy,
                WaveProfile = context?.WaveProfile,
                Pool = pool,
                Timer = new CountdownTimer(intervalSeconds),
                IsActive = true
            };

            loop.TimerHandler = () =>
            {
                if (!loop.IsActive)
                {
                    return;
                }

                TickWave(loop.Planet, loop.DetectionType, loop.Pool, loop.Strategy, context);

                if (loop.IsActive)
                {
                    loop.Timer.Reset();
                    loop.Timer.Start();
                }
            };

            loop.Timer.OnTimerStop += loop.TimerHandler;

            // Dispara uma wave imediata ao iniciar para garantir resposta instantânea.
            SpawnWave(planet, resolvedDetection, pool, strategy, context);

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
                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[WaveDebug] StopWaves chamado para {planet.ActorName}; timer será parado e removido.");

                loop.IsActive = false;

                if (loop.Timer != null && loop.TimerHandler != null)
                {
                    loop.Timer.OnTimerStop -= loop.TimerHandler;
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

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[WaveDebug] SpawnWave em {planet.ActorName} | Tentando spawnar {spawns} minions.");

            int spawnedCount = 0;
            for (int i = 0; i < spawns; i++)
            {
                var offset = ResolveSpawnOffset(radius, heightOffset);
                var poolable = pool.GetObject(spawnPosition + offset, planet);
                if (poolable == null)
                {
                    DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>($"Falha ao obter minion da pool para {planet.ActorName}.");
                    continue;
                }

                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[WaveDebug] Minion spawnado com sucesso para {planet.ActorName}.");

                spawnedCount++;
                EventBus<PlanetDefenseMinionSpawnedEvent>.Raise(new PlanetDefenseMinionSpawnedEvent(planet, detectionType, poolable));
            }

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[WaveDebug] SpawnWave finalizada em {planet.ActorName} | Spawnados: {spawnedCount}/{spawns}.");

            strategy?.OnEngaged(planet, detectionType);
        }

        private IDefenseStrategy ResolveStrategy(PlanetsMaster planet)
        {
            return _strategies.TryGetValue(planet, out var strategy) ? strategy : null;
        }

        private static int ResolveSpawnCount(PlanetDefenseSetupContext context)
        {
            return Mathf.Max(1, context?.WaveProfile?.enemiesPerWave ?? 6);
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

            DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                $"[WaveDebug] TickWave chamado para {planet.ActorName}.");

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

        private static int ResolveIntervalSeconds(PlanetDefenseSetupContext context)
        {
            var rawInterval = context?.WaveProfile?.secondsBetweenWaves ?? 5;
            return Mathf.Max(1, rawInterval);
        }

        private static bool EnsureWaveProfileAvailable(PlanetsMaster planet, PlanetDefenseSetupContext context)
        {
            if (context?.WaveProfile != null)
            {
                return true;
            }

            DebugUtility.LogWarning<RealPlanetDefenseWaveRunner>(
                $"DefenseWaveProfileSO ausente para {planet?.ActorName ?? "Unknown"}; waves não serão iniciadas.");
            return false;
        }

        private static void DisposeIfPossible(Timer timer)
        {
            if (timer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
