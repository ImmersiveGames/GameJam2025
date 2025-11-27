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
        private readonly Dictionary<PlanetsMaster, Action> _callbacks = new();

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

            var timer = new FrequencyTimer(GetIntervalSeconds(_config.DebugLoopIntervalSeconds));
            Action callback = () => LogWaveDebug(state, Time.time);
            timer.OnTick += callback;
            timer.Start();

            _debugTimers[state.Planet] = timer;
            _callbacks[state.Planet] = callback;
        }

        public void StopLogging(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_debugTimers.TryGetValue(planet, out var timer))
            {
                if (_callbacks.TryGetValue(planet, out var callback))
                {
                    timer.OnTick -= callback;
                    _callbacks.Remove(planet);
                }
                timer.Stop();
                DisposeIfPossible(timer);
                _debugTimers.Remove(planet);
            }
        }

        public void StopAll()
        {
            foreach (var pair in _debugTimers)
            {
                if (_callbacks.TryGetValue(pair.Key, out var callback))
                {
                    pair.Value.OnTick -= callback;
                }
                pair.Value.Stop();
                DisposeIfPossible(pair.Value);
            }

            _debugTimers.Clear();
            _callbacks.Clear();
        }

        private void LogWaveDebug(DefenseState state, float timestamp)
        {
            DebugUtility.LogVerbose<DefenseDebugLogger>(
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_config.DebugWaveDurationSeconds:0.##}s | Spawns previstos: {_config.DebugWaveSpawnCount}. (@ {timestamp:0.00}s)");
        }

        private static int GetIntervalSeconds(float seconds)
        {
            // FrequencyTimer espera o intervalo em segundos como inteiro.
            var clampedSeconds = Mathf.Max(seconds, 1f);
            return Mathf.Max(1, Mathf.RoundToInt(clampedSeconds));
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
