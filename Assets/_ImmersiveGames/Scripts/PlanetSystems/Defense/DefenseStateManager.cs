using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Centraliza o estado de defesas ativas por planeta, mantendo contadores
    /// e metadados necessários para orquestrar spawn e debug sem acoplamento
    /// direto à camada de MonoBehaviour.
    /// </summary>
    public sealed class DefenseStateManager
    {
        private readonly Dictionary<PlanetsMaster, DefenseState> _states = new();
        private readonly object _stateLock = new();

        public IReadOnlyDictionary<PlanetsMaster, DefenseState> States => _states;

        public DefenseState RegisterEngagement(
            PlanetsMaster planet,
            DetectionType detectionType,
            string detectorName,
            int activeDetectors)
        {
            if (planet == null)
            {
                return null;
            }

            lock (_stateLock)
            {
                if (!_states.TryGetValue(planet, out var state))
                {
                    state = new DefenseState(planet, detectionType, detectorName, Mathf.Max(1, activeDetectors));
                    _states.Add(planet, state);
                    return state;
                }

                state.DetectionType ??= detectionType;
                state.LastDetectorName = detectorName ?? state.LastDetectorName;
                state.ActiveDetectors = Mathf.Max(state.ActiveDetectors, activeDetectors);
                return state;
            }
        }

        public DefenseState RegisterDisengagement(
            PlanetsMaster planet,
            DetectionType detectionType,
            string detectorName,
            int activeDetectors,
            out bool removed)
        {
            removed = false;

            if (planet == null)
            {
                return null;
            }

            lock (_stateLock)
            {
                if (!_states.TryGetValue(planet, out var state))
                {
                    return null;
                }

                state.DetectionType ??= detectionType;
                state.LastDetectorName = detectorName ?? state.LastDetectorName;
                state.ActiveDetectors = Mathf.Max(0, activeDetectors);

                if (state.ActiveDetectors <= 0)
                {
                    _states.Remove(planet);
                    removed = true;
                }

                return state;
            }
        }

        public void ClearPlanet(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            lock (_stateLock)
            {
                _states.Remove(planet);
            }
        }

        /// <summary>
        /// Tenta recuperar o tipo de detecção associado ao planeta, de forma thread-safe.
        /// Útil para eventos tardios (ex.: disable) que não carregam DetectionType.
        /// </summary>
        public DetectionType TryGetDetectionType(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return null;
            }

            lock (_stateLock)
            {
                if (_states.TryGetValue(planet, out var state))
                {
                    return state.DetectionType;
                }
            }

            return null;
        }

        public void ClearAll()
        {
            lock (_stateLock)
            {
                _states.Clear();
            }
        }
    }

    /// <summary>
    /// Estado de defesa ativo por planeta.
    /// </summary>
    public sealed class DefenseState
    {
        public DefenseState(PlanetsMaster planet, DetectionType detectionType, string detectorName, int activeDetectors)
        {
            Planet = planet;
            DetectionType = detectionType;
            LastDetectorName = detectorName;
            ActiveDetectors = activeDetectors;
            EngagedAt = Time.time;
        }

        public PlanetsMaster Planet { get; }
        public DetectionType DetectionType { get; set; }
        public string LastDetectorName { get; set; }
        public int ActiveDetectors { get; set; }
        public float EngagedAt { get; }
    }
}
