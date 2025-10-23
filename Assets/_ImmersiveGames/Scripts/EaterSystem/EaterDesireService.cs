using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using ImprovedTimers;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    /// <summary>
    /// Serviço responsável por sortear e administrar os desejos do Eater.
    /// Mantém um temporizador baseado no ImprovedTimers para controlar a duração de cada desejo.
    /// </summary>
    internal sealed class EaterDesireService
    {
        private readonly EaterMaster _master;
        private readonly EaterConfigSo _config;
        private readonly Queue<PlanetResources> _recentDesires = new();
        private readonly PlanetResources[] _resourcePool;
        private readonly Dictionary<PlanetResources, int> _availabilityBuffer = new();
        private readonly List<WeightedDesireCandidate> _candidateBuffer = new();
        private readonly HashSet<PlanetResources> _recentLookup = new();
        private readonly StringBuilder _debugBuilder = new();

        private CountdownTimer _timer;
        private bool _active;
        private bool _waitingDelay;
        private PlanetResources? _currentDesire;
        private bool _currentDesireAvailable;
        private float _currentDuration;
        private int _currentDesireAvailableCount;
        private float _currentDesireWeight;

        public EaterDesireService(EaterMaster master, EaterConfigSo config)
        {
            _master = master;
            _config = config;
            _resourcePool = (PlanetResources[])Enum.GetValues(typeof(PlanetResources));
        }

        public bool IsActive => _active;
        public bool HasActiveDesire => _active && _currentDesire.HasValue;
        public PlanetResources? CurrentDesire => _currentDesire;
        public bool CurrentDesireAvailable => _currentDesireAvailable;
        public float CurrentDesireDuration => _currentDuration;
        public float CurrentDesireRemainingTime => _timer != null ? Mathf.Max(_timer.CurrentTime, 0f) : 0f;
        public int CurrentDesireAvailableCount => _currentDesireAvailableCount;
        public float CurrentDesireWeight => _currentDesireWeight;

        public void Update()
        {
            if (!_active || _timer == null)
            {
                return;
            }

            if (_timer.IsRunning || !_timer.IsFinished)
            {
                return;
            }

            if (_waitingDelay)
            {
                _waitingDelay = false;
                DebugUtility.LogVerbose<EaterDesireService>(
                    "⏱️ Atraso inicial concluído, selecionando primeiro desejo.",
                    context: _master,
                    instance: this);
                PickNextDesire();
                return;
            }

            if (HasActiveDesire)
            {
                DebugUtility.LogVerbose<EaterDesireService>(
                    $"⏳ Desejo {_currentDesire.Value} expirou, sorteando outro.",
                    context: _master,
                    instance: this);
                PickNextDesire();
            }
        }

        public bool Start()
        {
            if (_active)
            {
                return false;
            }

            EnsureTimerInstance();

            _active = true;
            _currentDesire = null;
            _currentDuration = 0f;
            _currentDesireAvailable = false;
            _currentDesireAvailableCount = 0;
            _currentDesireWeight = 0f;

            float delay = Mathf.Max(_config.DelayTimer, 0f);
            if (delay > 0f)
            {
                _waitingDelay = true;
                RestartTimer(delay);
                DebugUtility.LogVerbose<EaterDesireService>(
                    $"⌛ Iniciando desejos após atraso de {delay:F2}s.",
                    context: _master,
                    instance: this);
            }
            else
            {
                PickNextDesire();
            }

            return true;
        }

        public bool Stop()
        {
            if (!_active)
            {
                return false;
            }

            _active = false;
            _waitingDelay = false;
            _currentDesire = null;
            _currentDesireAvailable = false;
            _currentDuration = 0f;
            _currentDesireAvailableCount = 0;
            _currentDesireWeight = 0f;

            if (_timer != null)
            {
                _timer.Stop();
            }

            DebugUtility.LogVerbose<EaterDesireService>("🛑 Desejos do Eater pausados.", context: _master, instance: this);
            return true;
        }

        private void EnsureTimerInstance()
        {
            if (_timer != null)
            {
                return;
            }

            float baseDuration = Mathf.Max(_config.DesireDuration, 0.1f);
            _timer = new CountdownTimer(baseDuration);
        }

        private void RestartTimer(float duration)
        {
            if (_timer == null)
            {
                EnsureTimerInstance();
            }

            float safeDuration = Mathf.Max(duration, 0.05f);
            _timer.Stop();
            _timer.Reset(safeDuration);
            _timer.Start();
        }

        private void PickNextDesire()
        {
            if (!_active)
            {
                return;
            }

            if (!TrySelectDesire(out PlanetResources desire, out bool available, out int availableCount, out float selectionWeight))
            {
                DebugUtility.LogWarning<EaterDesireService>(
                    "Não foi possível selecionar um desejo válido para o Eater.",
                    context: _master,
                    instance: this);
                _currentDesire = null;
                _currentDesireAvailable = false;
                _currentDuration = 0f;
                _currentDesireAvailableCount = 0;
                _currentDesireWeight = 0f;
                return;
            }

            _currentDesire = desire;
            _currentDesireAvailable = available;
            _currentDesireAvailableCount = Mathf.Max(availableCount, 0);
            _currentDesireWeight = Mathf.Max(selectionWeight, 0f);

            float baseDuration = Mathf.Max(_config.DesireDuration, 0.1f);
            float unavailableFactor = Mathf.Clamp(_config.UnavailableDesireDurationMultiplier, 0.05f, 1f);
            _currentDuration = available ? baseDuration : Mathf.Max(baseDuration * unavailableFactor, 0.05f);

            RestartTimer(_currentDuration);

            _recentDesires.Enqueue(desire);
            int maxRecent = Mathf.Max(_config.MaxRecentDesires, 0);
            if (maxRecent <= 0)
            {
                _recentDesires.Clear();
            }
            while (_recentDesires.Count > maxRecent && maxRecent > 0)
            {
                _recentDesires.Dequeue();
            }

            string actorName = string.IsNullOrEmpty(_master.ActorName) ? _master.name : _master.ActorName;
            string availability = available
                ? $"disponível ({_currentDesireAvailableCount} planeta(s))"
                : "indisponível";
            float timestamp = Time.timeSinceLevelLoad;
            DebugUtility.Log<EaterDesireService>(
                $"✨ {actorName} deseja {desire} ({availability}) por {_currentDuration:F2}s (peso {_currentDesireWeight:F2}, t={timestamp:F2}s).",
                context: _master,
                instance: this);

            if (!available)
            {
                DebugUtility.LogVerbose<EaterDesireService>(
                    $"Nenhum planeta com {desire} detectado, mantendo desejo com duração reduzida para {_currentDuration:F2}s.",
                    context: _master,
                    instance: this);
            }
        }

        private bool TrySelectDesire(out PlanetResources desire, out bool available, out int availableCount, out float selectionWeight)
        {
            desire = default;
            available = false;
            availableCount = 0;
            selectionWeight = 0f;

            if (_resourcePool == null || _resourcePool.Length == 0)
            {
                return false;
            }

            Dictionary<PlanetResources, int> availability = BuildAvailabilityMap();
            _candidateBuffer.Clear();

            bool shouldPenalizeRecents = _recentDesires.Count > 0 && _recentDesires.Count < _resourcePool.Length;
            _recentLookup.Clear();
            if (_recentDesires.Count > 0)
            {
                foreach (PlanetResources recent in _recentDesires)
                {
                    _recentLookup.Add(recent);
                }
            }

            float totalWeight = 0f;

            for (int i = 0; i < _resourcePool.Length; i++)
            {
                PlanetResources resource = _resourcePool[i];
                availability.TryGetValue(resource, out int resourceCount);
                bool isAvailable = resourceCount > 0;
                bool penalize = shouldPenalizeRecents && _recentLookup.Contains(resource);

                float weight = CalculateCandidateWeight(isAvailable, resourceCount, penalize);
                if (weight <= 0f)
                {
                    continue;
                }

                var candidate = new WeightedDesireCandidate(resource, isAvailable, resourceCount, weight);
                _candidateBuffer.Add(candidate);
                totalWeight += weight;
            }

            if (_candidateBuffer.Count == 0)
            {
                if (_recentDesires.Count > 0)
                {
                    _recentDesires.Clear();
                    return TrySelectDesire(out desire, out available, out availableCount, out selectionWeight);
                }

                PlanetResources fallbackResource = _resourcePool[UnityEngine.Random.Range(0, _resourcePool.Length)];
                availability.TryGetValue(fallbackResource, out int fallbackCount);
                desire = fallbackResource;
                available = fallbackCount > 0;
                availableCount = fallbackCount;
                selectionWeight = 0f;
                return true;
            }

            if (_candidateBuffer.Count > 0)
            {
                _debugBuilder.Clear();
                _debugBuilder.Append("🎯 Pool de desejos: ");
                for (int i = 0; i < _candidateBuffer.Count; i++)
                {
                    if (i > 0)
                    {
                        _debugBuilder.Append(" | ");
                    }

                    WeightedDesireCandidate candidate = _candidateBuffer[i];
                    _debugBuilder
                        .Append(candidate.Resource)
                        .Append(" (disp=").Append(candidate.IsAvailable)
                        .Append(", planetas=").Append(candidate.AvailableCount)
                        .Append(", peso=").Append(candidate.Weight.ToString("F2"))
                        .Append(')');
                }

                DebugUtility.LogVerbose<EaterDesireService>(
                    _debugBuilder.ToString(),
                    context: _master,
                    instance: this,
                    deduplicate: true);
            }

            if (totalWeight <= 0f)
            {
                WeightedDesireCandidate fallbackCandidate = _candidateBuffer[UnityEngine.Random.Range(0, _candidateBuffer.Count)];
                desire = fallbackCandidate.Resource;
                available = fallbackCandidate.IsAvailable;
                availableCount = fallbackCandidate.AvailableCount;
                selectionWeight = fallbackCandidate.Weight;
                return true;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < _candidateBuffer.Count; i++)
            {
                WeightedDesireCandidate candidate = _candidateBuffer[i];
                roll -= candidate.Weight;
                if (roll <= 0f)
                {
                    desire = candidate.Resource;
                    available = candidate.IsAvailable;
                    availableCount = candidate.AvailableCount;
                    selectionWeight = candidate.Weight;
                    return true;
                }
            }

            WeightedDesireCandidate lastCandidate = _candidateBuffer[_candidateBuffer.Count - 1];
            desire = lastCandidate.Resource;
            available = lastCandidate.IsAvailable;
            availableCount = lastCandidate.AvailableCount;
            selectionWeight = lastCandidate.Weight;
            return true;
        }

        private Dictionary<PlanetResources, int> BuildAvailabilityMap()
        {
            _availabilityBuffer.Clear();

            PlanetsManager manager = PlanetsManager.Instance;
            if (manager == null)
            {
                return _availabilityBuffer;
            }

            var map = manager.GetPlanetResourcesMap();
            if (map == null || map.Count == 0)
            {
                return _availabilityBuffer;
            }

            foreach (var pair in map)
            {
                PlanetResources resourceType = pair.Value;
                if (_availabilityBuffer.TryGetValue(resourceType, out int count))
                {
                    _availabilityBuffer[resourceType] = count + 1;
                }
                else
                {
                    _availabilityBuffer.Add(resourceType, 1);
                }
            }

            return _availabilityBuffer;
        }

        private float CalculateCandidateWeight(bool hasAvailablePlanets, int availablePlanets, bool penalizeRecent)
        {
            float weight;
            if (hasAvailablePlanets)
            {
                float baseWeight = Mathf.Max(_config.AvailableDesireWeight, 0f);
                float perPlanet = Mathf.Max(_config.PerPlanetAvailableWeight, 0f);
                weight = baseWeight + Mathf.Max(availablePlanets, 0) * perPlanet;
            }
            else
            {
                weight = Mathf.Max(_config.UnavailableDesireWeight, 0f);
            }

            if (penalizeRecent)
            {
                float multiplier = Mathf.Clamp01(_config.RecentDesireWeightMultiplier);
                weight *= multiplier;
            }

            return Mathf.Max(weight, 0f);
        }

        private readonly struct WeightedDesireCandidate
        {
            public WeightedDesireCandidate(PlanetResources resource, bool isAvailable, int availableCount, float weight)
            {
                Resource = resource;
                IsAvailable = isAvailable;
                AvailableCount = availableCount;
                Weight = weight;
            }

            public PlanetResources Resource { get; }
            public bool IsAvailable { get; }
            public int AvailableCount { get; }
            public float Weight { get; }
        }
    }
}
