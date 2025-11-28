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
        private sealed class LogLoop
        {
            public FrequencyTimer Timer;
            public Action Tick;
            public int WaveIntervalSeconds;
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
            _intervalSeconds = Mathf.Max(1, waveProfile?.waveIntervalSeconds ?? 5);
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

            var loop = BuildLogLoop(state);

            loop.Timer.OnTick += loop.Tick;
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
                if (loop.Timer != null && loop.Tick != null)
                {
                    loop.Timer.OnTick -= loop.Tick;
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
                if (pair.Value.Timer != null && pair.Value.Tick != null)
                {
                    pair.Value.Timer.OnTick -= pair.Value.Tick;
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

        private static void DisposeIfPossible(FrequencyTimer timer)
        {
            if (timer is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private LogLoop BuildLogLoop(DefenseState state)
        {
            var loop = new LogLoop
            {
                WaveIntervalSeconds = _intervalSeconds
            };

            loop.Tick = () => TickLog(state);
            // FrequencyTimer aqui representa o intervalo entre logs (igual ao intervalo da wave).
            // O asset trabalha em segundos inteiros, então usamos diretamente o valor do ScriptableObject.
            loop.Timer = new FrequencyTimer(loop.WaveIntervalSeconds);
            return loop;
        }
    }
}
