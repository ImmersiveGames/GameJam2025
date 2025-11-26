using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Gerencia o estado de defesas ativas por planeta, permitindo que
    /// serviços de spawn consultem contagens e detectores por tipo sem
    /// duplicar lógica de controle.
    /// </summary>
    public sealed class DefenseStateManager
    {
        private readonly Dictionary<PlanetsMaster, PlanetDefenseState> _states = new();

        public IReadOnlyDictionary<PlanetsMaster, PlanetDefenseState> ActiveStates => _states;

        public bool TryEngage(PlanetDefenseEngagedEvent engagedEvent, out PlanetDefenseState state)
        {
            state = null;
            if (engagedEvent.Planet == null || engagedEvent.Detector == null)
            {
                return false;
            }

            if (!_states.TryGetValue(engagedEvent.Planet, out state))
            {
                state = new PlanetDefenseState(engagedEvent.Planet, engagedEvent.DetectionType);
                _states.Add(engagedEvent.Planet, state);
            }

            if (!state.AddDetector(engagedEvent.Detector, engagedEvent.DetectionType))
            {
                DefenseUtils.LogDuplicateDetector(engagedEvent.Detector, engagedEvent.DetectionType);
                return false;
            }

            return true;
        }

        public bool TryDisengage(PlanetDefenseDisengagedEvent disengagedEvent, out PlanetDefenseState state)
        {
            state = null;
            if (disengagedEvent.Planet == null || disengagedEvent.Detector == null)
            {
                return false;
            }

            if (!_states.TryGetValue(disengagedEvent.Planet, out state))
            {
                return false;
            }

            if (!state.RemoveDetector(disengagedEvent.Detector, disengagedEvent.DetectionType))
            {
                return false;
            }

            if (state.ActiveDetectors <= 0)
            {
                _states.Remove(disengagedEvent.Planet);
            }

            return true;
        }

        public int ClearPlanet(PlanetsMaster planet)
        {
            if (planet == null || !_states.TryGetValue(planet, out var state))
            {
                return 0;
            }

            _states.Remove(planet);
            return state.ActiveDetectors;
        }

        public void ClearAll()
        {
            _states.Clear();
        }
    }

    /// <summary>
    /// Estado associado a um planeta específico, mantendo rastreamento
    /// por detector e por tipo de detecção para facilitar decisões de
    /// estratégia ou métricas.
    /// </summary>
    public sealed class PlanetDefenseState
    {
        private readonly HashSet<IDetector> _detectors = new();
        private readonly Dictionary<DetectionType, HashSet<IDetector>> _detectorsByType = new();

        public PlanetDefenseState(PlanetsMaster planet, DetectionType primaryDetection)
        {
            Planet = planet;
            PrimaryDetectionType = primaryDetection;
        }

        public PlanetsMaster Planet { get; }

        public DetectionType PrimaryDetectionType { get; private set; }

        public int ActiveDetectors => _detectors.Count;

        public IReadOnlyDictionary<DetectionType, HashSet<IDetector>> DetectorsByType => _detectorsByType;

        public bool AddDetector(IDetector detector, DetectionType detectionType)
        {
            if (!_detectors.Add(detector))
            {
                return false;
            }

            DetectionType resolvedType = detectionType ?? PrimaryDetectionType;
            if (PrimaryDetectionType == null)
            {
                PrimaryDetectionType = resolvedType;
            }

            DefenseUtils.TryAddToLookup(_detectorsByType, resolvedType, detector);
            return true;
        }

        public bool RemoveDetector(IDetector detector, DetectionType detectionType)
        {
            if (!_detectors.Remove(detector))
            {
                return false;
            }

            DetectionType resolvedType = detectionType ?? PrimaryDetectionType;
            DefenseUtils.TryRemoveFromLookup(_detectorsByType, resolvedType, detector);
            return true;
        }
    }
}
