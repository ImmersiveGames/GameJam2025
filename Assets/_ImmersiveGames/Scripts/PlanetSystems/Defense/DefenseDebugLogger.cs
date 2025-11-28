// DefenseDebugLogger.cs
using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseDebugLogger
    {
        private sealed class LogLoop
        {
            public CountdownTimer Timer;
            public Action TimerHandler;
            public bool IsActive;
        }

        private DefenseWaveProfileSO _waveProfile;
        private readonly Dictionary<PlanetsMaster, LogLoop> _loops = new();

        public DefenseDebugLogger(DefenseWaveProfileSO waveProfile) => Configure(waveProfile);

        public void Configure(DefenseWaveProfileSO waveProfile)
        {
            _waveProfile = waveProfile;
        }

        public void StartLogging(DefenseState state)
        {
            if (state?.Planet == null || _waveProfile == null) return;
            if (_loops.ContainsKey(state.Planet)) return;

            var interval = Mathf.Max(1, _waveProfile.secondsBetweenWaves);
            var count    = Mathf.Max(1, _waveProfile.enemiesPerWave);

            Log(state, interval, count);

            var loop = new LogLoop
            {
                Timer = new CountdownTimer(interval),
                IsActive = true
            };

            loop.TimerHandler = () =>
            {
                if (!loop.IsActive) return;
                Log(state, interval, count);
                if (loop.IsActive) { loop.Timer.Reset(); loop.Timer.Start(); }
            };

            loop.Timer.OnTimerStop += loop.TimerHandler;
            loop.Timer.Start();
            _loops[state.Planet] = loop;
        }

        public void StopLogging(PlanetsMaster planet)
        {
            if (planet == null || !_loops.TryGetValue(planet, out var loop)) return;

            loop.IsActive = false;
            loop.Timer.OnTimerStop -= loop.TimerHandler;
            loop.Timer?.Stop();
            DisposeIfPossible(loop.Timer);
            _loops.Remove(planet);
        }

        private void Log(DefenseState state, int interval, int count)
        {
            DebugUtility.LogVerbose<DefenseDebugLogger>(
                $"[DefenseLog] {state.Planet.ActorName} | Onda a cada {interval}s | {count} minions previstos");
        }

        private static void DisposeIfPossible(Timer timer)
        {
            if (timer is IDisposable d) d.Dispose();
        }
    }
}