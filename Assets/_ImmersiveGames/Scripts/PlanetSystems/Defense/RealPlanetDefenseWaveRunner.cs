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

            SpawnWave(planet, detectionType, pool, strategy);

            var timer = new FrequencyTimer(GetIntervalSeconds(config.DebugWaveDurationSeconds));
            SubscribeToTick(timer, () => SpawnWave(planet, detectionType, pool, strategy));
            timer.Start();

            _running[planet] = timer;
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_running.TryGetValue(planet, out var timer))
            {
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

        private void SpawnWave(PlanetsMaster planet, DetectionType detectionType, ObjectPool pool, IDefenseStrategy strategy)
        {
            if (planet == null || pool == null)
            {
                return;
            }

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

        private static int GetIntervalSeconds(float seconds)
        {
            // FrequencyTimer espera o intervalo em segundos como inteiro.
            var clampedSeconds = Mathf.Max(seconds, 1f);
            return Mathf.Max(1, Mathf.RoundToInt(clampedSeconds));
        }

        private static void SubscribeToTick(FrequencyTimer timer, Action callback)
        {
            if (timer == null || callback == null)
            {
                return;
            }

            var eventNames = new[] { "OnTick", "Tick", "OnTimerTick", "OnFrequency" };
            foreach (var name in eventNames)
            {
                var eventInfo = typeof(FrequencyTimer).GetEvent(name);
                if (eventInfo == null)
                {
                    continue;
                }

                var handlerType = eventInfo.EventHandlerType;
                Delegate handler = null;

                if (handlerType == typeof(Action))
                {
                    handler = Delegate.CreateDelegate(handlerType, callback.Target, callback.Method, false);
                }
                else if (handlerType == typeof(Action<float>))
                {
                    Action<float> wrapper = _ => callback();
                    handler = Delegate.CreateDelegate(handlerType, wrapper.Target, wrapper.Method, false);
                }

                if (handler != null)
                {
                    eventInfo.AddEventHandler(timer, handler);
                    return;
                }
            }
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
