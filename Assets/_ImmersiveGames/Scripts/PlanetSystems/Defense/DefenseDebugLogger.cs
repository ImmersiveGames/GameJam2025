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
        private float _debugLoopIntervalSeconds = 5f;
        private int _debugWaveDurationSeconds = 5;
        private int _debugWaveSpawnCount = 6;
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
            _debugLoopIntervalSeconds = Mathf.Max(0.1f, waveProfile?.waveIntervalSeconds ?? 5f);
            _debugWaveDurationSeconds = Mathf.Max(1, Mathf.RoundToInt(waveProfile?.waveIntervalSeconds ?? 5f));
            _debugWaveSpawnCount = Mathf.Max(1, waveProfile?.minionsPerWave ?? 6);
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

            // FrequencyTimer recebe intervalo em segundos (inteiro); usamos o valor do perfil.
            var timerIntervalSeconds = Mathf.Max(1, Mathf.RoundToInt(_debugLoopIntervalSeconds));
            var timer = new FrequencyTimer(timerIntervalSeconds);
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
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_debugWaveDurationSeconds}s | Spawns previstos: {_debugWaveSpawnCount}. (@ {timestamp:0.00}s)");
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
