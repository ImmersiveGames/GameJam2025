using System.Collections.Generic;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Responsável por logs periódicos de defesa por planeta utilizando
    /// corrotinas dedicadas, evitando dependência em Update.
    /// </summary>
    public sealed class DefenseDebugLogger
    {
        private readonly MonoBehaviour _host;
        private PlanetDefenseSpawnConfig _config;
        private readonly Dictionary<PlanetsMaster, Coroutine> _debugLoops = new();

        public DefenseDebugLogger(MonoBehaviour host, PlanetDefenseSpawnConfig config)
        {
            _host = host;
            _config = config ?? new PlanetDefenseSpawnConfig();
        }

        public void Configure(PlanetDefenseSpawnConfig config)
        {
            _config = config ?? new PlanetDefenseSpawnConfig();
        }

        public void StartLogging(DefenseState state)
        {
            if (_host == null || state == null || state.Planet == null)
            {
                return;
            }

            if (_debugLoops.ContainsKey(state.Planet))
            {
                return;
            }

            LogWaveDebug(state, Time.time);
            var routine = _host.StartCoroutine(LogLoop(state));
            _debugLoops[state.Planet] = routine;
        }

        public void StopLogging(PlanetsMaster planet)
        {
            if (planet == null || _host == null)
            {
                return;
            }

            if (_debugLoops.TryGetValue(planet, out var routine))
            {
                _host.StopCoroutine(routine);
                _debugLoops.Remove(planet);
            }
        }

        public void StopAll()
        {
            if (_host == null)
            {
                return;
            }

            foreach (var loop in _debugLoops.Values)
            {
                _host.StopCoroutine(loop);
            }

            _debugLoops.Clear();
        }

        private System.Collections.IEnumerator LogLoop(DefenseState state)
        {
            var wait = new WaitForSeconds(_config.DebugLoopIntervalSeconds);
            while (true)
            {
                LogWaveDebug(state, Time.time);
                yield return wait;
            }
        }

        private void LogWaveDebug(DefenseState state, float timestamp)
        {
            DebugUtility.LogVerbose<DefenseDebugLogger>(
                $"[Debug] Defesa ativa em {state.Planet.ActorName} contra {state.DetectionType?.TypeName ?? "Unknown"} | Onda: {_config.DebugWaveDurationSeconds:0.##}s | Spawns previstos: {_config.DebugWaveSpawnCount}. (@ {timestamp:0.00}s)");
        }
    }
}
