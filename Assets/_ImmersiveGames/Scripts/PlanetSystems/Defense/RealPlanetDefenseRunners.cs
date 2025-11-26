using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Runner real de pools que integra com o PoolManager padrão. Ainda
    /// não instancia minions reais, mas garante que o PoolData seja
    /// registrado e preparado para uso pelos wave runners.
    /// </summary>
    public sealed class RealPlanetDefensePoolRunner : MonoBehaviour, IPlanetDefensePoolRunner
    {
        [SerializeField] private PoolManager poolManager;

        private readonly Dictionary<PlanetsMaster, ObjectPool> _planetPools = new();

        private void Awake()
        {
            poolManager ??= PoolManager.Instance;
        }

        public void ConfigureForPlanet(PlanetsMaster planet, DefenseStrategyResult strategy)
        {
            WarmUp(planet, strategy);
        }

        public void WarmUp(PlanetsMaster planet, DetectionType detectionType, DefenseStrategyResult strategy)
        {
            WarmUp(planet, strategy);
        }

        public void WarmUp(PlanetsMaster planet, DefenseStrategyResult strategy)
        {
            if (planet == null || strategy.PoolData == null || poolManager == null)
            {
                return;
            }

            if (_planetPools.ContainsKey(planet))
            {
                return;
            }

            if (!PoolData.Validate(strategy.PoolData, this))
            {
                return;
            }

            var pool = poolManager.RegisterPool(strategy.PoolData);
            if (pool != null)
            {
                _planetPools[planet] = pool;
            }
        }

        public void Release(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_planetPools.Remove(planet))
            {
                DebugUtility.LogVerbose<RealPlanetDefensePoolRunner>($"Pool liberado para {planet.ActorName}.");
            }
        }
    }

    /// <summary>
    /// Runner real de ondas; administra coroutines individuais por planeta
    /// para permitir spawn periódico. Neste ponto ele apenas registra logs
    /// e prepara o terreno para integração de minions.
    /// </summary>
    public sealed class RealPlanetDefenseWaveRunner : MonoBehaviour, IPlanetDefenseWaveRunner
    {
        private readonly Dictionary<PlanetsMaster, Coroutine> _running = new();
        private DefenseDebugLogger _logger;

        private void Awake()
        {
            _logger ??= new DefenseDebugLogger();
        }

        public void ConfigureForPlanet(PlanetsMaster planet, DefenseStrategyResult strategy)
        {
            // No momento não há configuração incremental além do start/stop.
        }

        public void StartWaves(PlanetsMaster planet, DetectionType detectionType, DefenseStrategyResult strategy)
        {
            if (planet == null || _running.ContainsKey(planet))
            {
                return;
            }

            Coroutine routine = StartCoroutine(RunWaveLoop(planet, strategy));
            if (routine != null)
            {
                _running[planet] = routine;
                _logger.LogWaveStart(planet, strategy);
            }
        }

        public void StopWaves(PlanetsMaster planet)
        {
            if (planet == null || !_running.TryGetValue(planet, out var routine))
            {
                return;
            }

            StopCoroutine(routine);
            _running.Remove(planet);
            _logger.LogWaveStop(planet);
        }

        public bool IsRunning(PlanetsMaster planet)
        {
            return planet != null && _running.ContainsKey(planet);
        }

        private IEnumerator RunWaveLoop(PlanetsMaster planet, DefenseStrategyResult strategy)
        {
            var wait = new WaitForSeconds(strategy.WaveIntervalSeconds > 0f ? strategy.WaveIntervalSeconds : 1f);
            while (true)
            {
                DebugUtility.LogVerbose<RealPlanetDefenseWaveRunner>(
                    $"[Wave] Preparando spawn de {strategy.MinionsPerWave} minions em {planet.ActorName} (raio {strategy.SpawnRadius:0.##}).");
                yield return wait;
            }
        }
    }
}
