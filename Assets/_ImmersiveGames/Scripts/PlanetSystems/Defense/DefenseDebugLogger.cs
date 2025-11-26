using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável por logs periódicos de defesa por planeta utilizando FrequencyTimer
    /// dedicado, evitando dependência em Update ou corrotinas.
    /// </summary>
    public sealed class DefenseDebugLogger
    {
        private PlanetDefenseSpawnConfig _config;
        private readonly Dictionary<PlanetsMaster, FrequencyTimer> _debugTimers = new();

        public DefenseDebugLogger(PlanetDefenseSpawnConfig config)
        {
            _config = config ?? new PlanetDefenseSpawnConfig();
        }

        public void Configure(PlanetDefenseSpawnConfig config)
        {
            _config = config ?? new PlanetDefenseSpawnConfig();
        }

        public void StartLogging(DefenseState state)
        {
            if (state?.Planet == null)
            {
                return;
            }

            if (_debugTimers.ContainsKey(state.Planet))
            {
                return;
            }

            LogWaveDebug(state, Time.time);

            var timer = new FrequencyTimer(Mathf.Max(_config.DebugLoopIntervalSeconds, 0.05f));
            SubscribeToTick(timer, () => LogWaveDebug(state, Time.time));
            timer.Start();

            _debugTimers[state.Planet] = timer;
        }

        public void StopLogging(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_debugTimers.TryGetValue(planet, out var timer))
            {
                timer.Stop();
                DisposeIfPossible(timer);
                _debugTimers.Remove(planet);
            }
        }

        public void StopAll()
        {
            foreach (var timer in _debugTimers.Values)
            {
                timer.Stop();
                DisposeIfPossible(timer);
            }

            _debugTimers.Clear();
        }

        private void LogWaveDebug(DefenseState state, float timestamp)
        {
            DebugUtility.LogVerbose<DefenseDebugLogger>(
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_config.DebugWaveDurationSeconds:0.##}s | Spawns previstos: {_config.DebugWaveSpawnCount}. (@ {timestamp:0.00}s)");
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
