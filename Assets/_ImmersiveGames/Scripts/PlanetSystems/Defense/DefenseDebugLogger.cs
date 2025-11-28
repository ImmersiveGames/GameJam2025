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
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseDebugLogger
    {
        private DefenseWaveProfileSO _waveProfile;
        private int _intervalSeconds = 5;
        private int _spawnCount = 6;
        private readonly Dictionary<PlanetsMaster, FrequencyTimer> _debugTimers = new();
        private readonly Dictionary<PlanetsMaster, Action> _callbacks = new();

        public DefenseDebugLogger(DefenseWaveProfileSO waveProfile)
        {
            Configure(waveProfile);
        }

        public void Configure(DefenseWaveProfileSO waveProfile)
        {
            _waveProfile = waveProfile;
            // Intervalos e contagens vêm exclusivamente do ScriptableObject configurado no Inspector.
            _intervalSeconds = Mathf.Max(1, Mathf.RoundToInt(waveProfile?.waveIntervalSeconds ?? 5f));
            _spawnCount = Mathf.Max(1, waveProfile?.minionsPerWave ?? 6);
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

            var tickFrequency = ResolveTickFrequency(_intervalSeconds);
            var timer = new FrequencyTimer(tickFrequency);
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
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_intervalSeconds}s | Spawns previstos: {_spawnCount}. (@ {timestamp:0.00}s)");
        }

        private static float ResolveTickFrequency(int intervalSeconds)
        {
            // FrequencyTimer recebe frequência (ticks por segundo); para intervalo em segundos, usamos 1 / intervalo.
            intervalSeconds = Mathf.Max(1, intervalSeconds);
            return 1f / intervalSeconds;
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
