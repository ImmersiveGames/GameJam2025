using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável por logs periódicos de defesa por planeta utilizando CountdownTimer
    /// dedicado, evitando dependência em Update ou corrotinas.
    /// </summary>
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseDebugLogger
    {
        private sealed class LogLoop
        {
            public CountdownTimer Timer;
            public Action TimerHandler;
            public int WaveIntervalSeconds;
            public bool IsActive;
        }

        private DefenseWaveProfileSO _waveProfile;
        private int _intervalSeconds = 5;
        private int _spawnCount = 6;
        private readonly Dictionary<PlanetsMaster, LogLoop> _debugTimers = new();

        public DefenseDebugLogger(DefenseWaveProfileSO waveProfile)
        {
            Configure(waveProfile);
        }

        public void Configure(DefenseWaveProfileSO waveProfile)
        {
            _waveProfile = waveProfile;
            // Intervalos e contagens vêm exclusivamente do ScriptableObject configurado no Inspector.
            _intervalSeconds = Mathf.Max(1, waveProfile?.secondsBetweenWaves ?? 5);
            _spawnCount = Mathf.Max(1, waveProfile?.enemiesPerWave ?? 6);
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

            var loop = new LogLoop
            {
                WaveIntervalSeconds = _intervalSeconds,
                Timer = new CountdownTimer(_intervalSeconds),
                IsActive = true
            };

            loop.TimerHandler = () =>
            {
                if (!loop.IsActive)
                {
                    return;
                }

                TickLog(state);

                if (!loop.IsActive)
                {
                    return;
                }

                loop.Timer.Reset();
                loop.Timer.Start();
            };

            loop.Timer.OnTimerStop += loop.TimerHandler;
            loop.Timer.Start();

            _debugTimers[state.Planet] = loop;
        }

        public void StopLogging(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_debugTimers.TryGetValue(planet, out var loop))
            {
                loop.IsActive = false;

                if (loop.Timer != null && loop.TimerHandler != null)
                {
                    loop.Timer.OnTimerStop -= loop.TimerHandler;
                }

                loop.Timer?.Stop();
                DisposeIfPossible(loop.Timer);
                _debugTimers.Remove(planet);
            }
        }

        public void StopAll()
        {
            foreach (var pair in _debugTimers)
            {
                pair.Value.IsActive = false;

                if (pair.Value.Timer != null && pair.Value.TimerHandler != null)
                {
                    pair.Value.Timer.OnTimerStop -= pair.Value.TimerHandler;
                }
                pair.Value.Timer?.Stop();
                DisposeIfPossible(pair.Value.Timer);
            }

            _debugTimers.Clear();
        }

        private void LogWaveDebug(DefenseState state, float timestamp)
        {
            DebugUtility.LogVerbose<DefenseDebugLogger>(
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_intervalSeconds}s | Spawns previstos: {_spawnCount}. (@ {timestamp:0.00}s)");
        }

        private void TickLog(DefenseState state)
        {
            if (state?.Planet == null)
            {
                return;
            }

            LogWaveDebug(state, Time.time);
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
