// DefenseDebugLogger.cs
using System;
using System.Collections.Generic;
using ImprovedTimers;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    public interface IDefenseLogger
    {
        void Configure(DefenseWaveProfileSo waveProfile);
        void OnEngaged(DefenseState state, bool isFirstEngagement);
        void OnDisengaged(PlanetsMaster planet, bool shouldStopLogging);
        void OnDisabled(PlanetsMaster planet);
    }

    [DebugLevel(level: DebugLevel.Verbose)]
    public sealed class DefenseDebugLogger : IDefenseLogger
    {
        private sealed class LogLoop
        {
            public CountdownTimer timer;
            public Action timerHandler;
            public bool isActive;
        }

        private DefenseWaveProfileSo _waveProfile;
        private readonly Dictionary<PlanetsMaster, LogLoop> _loops = new();

        public DefenseDebugLogger(DefenseWaveProfileSo waveProfile) => Configure(waveProfile);

        public void Configure(DefenseWaveProfileSo waveProfile)
        {
            _waveProfile = waveProfile;
        }

        public void OnEngaged(DefenseState state, bool isFirstEngagement)
        {
            if (!isFirstEngagement)
            {
                return;
            }

            StartLogging(state);
        }

        public void OnDisengaged(PlanetsMaster planet, bool shouldStopLogging)
        {
            if (!shouldStopLogging)
            {
                return;
            }

            StopLogging(planet);
        }

        public void OnDisabled(PlanetsMaster planet)
        {
            StopLogging(planet);
        }

        private void StartLogging(DefenseState state)
        {
            if (state?.Planet == null || _waveProfile == null) return;
            if (_loops.ContainsKey(state.Planet)) return;

            var interval = Mathf.Max(1, _waveProfile.secondsBetweenWaves);
            var count    = Mathf.Max(1, _waveProfile.enemiesPerWave);

            Log(state, interval, count);

            var loop = new LogLoop
            {
                timer = new CountdownTimer(interval),
                isActive = true
            };

            loop.timerHandler = () =>
            {
                if (!loop.isActive) return;
                Log(state, interval, count);
                if (loop.isActive) { loop.timer.Reset(); loop.timer.Start(); }
            };

            loop.timer.OnTimerStop += loop.timerHandler;
            loop.timer.Start();
            _loops[state.Planet] = loop;
        }

        private void StopLogging(PlanetsMaster planet)
        {
            if (planet == null || !_loops.TryGetValue(planet, out var loop)) return;

            loop.isActive = false;
            loop.timer.OnTimerStop -= loop.timerHandler;
            loop.timer?.Stop();
            DisposeIfPossible(loop.timer);
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