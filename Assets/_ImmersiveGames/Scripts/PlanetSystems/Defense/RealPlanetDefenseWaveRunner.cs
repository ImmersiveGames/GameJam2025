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
    public sealed class RealPlanetDefenseWaveRunner : IPlanetDefenseWaveRunner, IInjectableComponent
    {
        private readonly Dictionary<PlanetsMaster, FrequencyTimer> _running = new();
        private readonly Dictionary<PlanetsMaster, IDefenseStrategy> _strategies = new();
        private readonly Dictionary<PlanetsMaster, Action> _callbacks = new();

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

            var timer = new FrequencyTimer(GetIntervalSeconds(ResolveIntervalSeconds(context)));
            Action callback = () => SpawnWave(planet, resolvedDetection, pool, strategy, context);
            timer.OnTick += callback;
            timer.Start();

            _running[planet] = timer;
            _callbacks[planet] = callback;
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.TryGetValue(planet, out var timer))
            {
                if (_callbacks.TryGetValue(planet, out var callback))
                {
                    timer.OnTick -= callback;
                    _callbacks.Remove(planet);
                }
                timer.Stop();
                DisposeIfPossible(timer);
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
            for (int i = 0; i < spawns; i++)
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

        private static int GetIntervalSeconds(float seconds)
        {
            // FrequencyTimer espera o intervalo em segundos como inteiro.
            var clampedSeconds = Mathf.Max(seconds, 1f);
            return Mathf.Max(1, Mathf.RoundToInt(clampedSeconds));
        }

        private static float ResolveIntervalSeconds(PlanetDefenseSetupContext context)
        {
            return Mathf.Max(1f, context?.WaveProfile?.waveIntervalSeconds ?? 5f);
        }

        private static int ResolveSpawnCount(PlanetDefenseSetupContext context)
        {
            return Mathf.Max(1, context?.WaveProfile?.minionsPerWave ?? 6);
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
