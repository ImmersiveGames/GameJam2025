using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.EaterSystem.Configs;
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
        private readonly EntityAudioEmitter _audioEmitter;
        private readonly string _actorLabel;
        private readonly Queue<PlanetResources> _recentDesires = new();
        private readonly PlanetResources[] _resourcePool;
        private readonly Dictionary<PlanetResources, int> _availabilityBuffer = new();
        private readonly List<WeightedDesireCandidate> _candidateBuffer = new();
        private readonly HashSet<PlanetResources> _recentLookup = new();
        private readonly StringBuilder _debugBuilder = new();

        private CountdownTimer _timer;
        private DesireCyclePhase _phase = DesireCyclePhase.Inactive;
        private float _scheduledResumeDuration;
        private PlanetResources? _currentDesire;
        private bool _currentDesireAvailable;
        private float _currentDuration;
        private int _currentDesireAvailableCount;
        private float _currentDesireWeight;
        private LockedDesireSnapshot? _lockedSnapshot;

        public event Action<EaterDesireInfo> EventDesireChanged;

        private bool _missingEmitterLogged;
        private bool _missingSoundLogged;

        public EaterDesireService(EaterMaster master, EaterConfigSo config, EntityAudioEmitter audioEmitter)
        {
            _master = master;
            _config = config;
            _audioEmitter = audioEmitter;
            _actorLabel = ResolveActorLabel(master);
            _resourcePool = (PlanetResources[])Enum.GetValues(typeof(PlanetResources));
        }

        private bool IsActive => _phase != DesireCyclePhase.Inactive;
        private bool HasActiveDesire => (_phase != DesireCyclePhase.Inactive && _currentDesire.HasValue) || HasLockedDesire;
        private bool HasLockedDesire => _lockedSnapshot.HasValue;

        public void Update()
        {
            if (!IsActive || _timer == null)
            {
                return;
            }

            if (_timer.IsRunning || !_timer.IsFinished)
            {
                return;
            }

            switch (_phase)
            {
                case DesireCyclePhase.WaitingInitialDelay:
                    HandleWaitingInitialDelay();
                    break;
                case DesireCyclePhase.WaitingResumeDelay:
                    HandleWaitingResumeDelay();
                    break;
                case DesireCyclePhase.Active:
                    HandleActivePhase();
                    break;
                case DesireCyclePhase.Inactive:
                default:
                    break;
            }
        }

        private void HandleWaitingInitialDelay()
        {
            DebugUtility.LogVerbose(
                "⏱️ Atraso inicial concluído, selecionando primeiro desejo.",
                context: _master,
                instance: this);
            _phase = DesireCyclePhase.Active;
            PickNextDesire();
        }

        private void HandleWaitingResumeDelay()
        {
            float resumeDuration = Mathf.Max(_scheduledResumeDuration, 0.05f);
            _scheduledResumeDuration = 0f;
            string resumeLabel = _currentDesire.HasValue ? _currentDesire.Value.ToString() : "Desconhecido";

            DebugUtility.LogVerbose(
                $"▶️ Desejo {resumeLabel} retomado após atraso inicial por {resumeDuration:F2}s.",
                context: _master,
                instance: this);

            BeginActiveCycle(resumeDuration);
        }

        private void HandleActivePhase()
        {
            if (!HasActiveDesire)
            {
                return;
            }

            if (_currentDesire != null)
            {
                DebugUtility.LogVerbose(
                    $"⏳ Desejo {_currentDesire.Value} expirou, sorteando outro.",
                    context: _master,
                    instance: this);
            }

            PickNextDesire();
        }

        public bool Start()
        {
            if (IsActive)
            {
                return false;
            }

            EnsureTimerInstance();

            _phase = DesireCyclePhase.Inactive;
            _lockedSnapshot = null;
            ClearCurrentDesire();
            _scheduledResumeDuration = 0f;

            float delay = _config.InitialDesireDelay;
            if (delay > 0f)
            {
                _phase = DesireCyclePhase.WaitingInitialDelay;
                RestartTimer(delay);
                DebugUtility.LogVerbose(
                    $"⌛ Iniciando desejos após atraso de {delay:F2}s.",
                    context: _master,
                    instance: this);
                NotifyDesireChanged();
            }
            else
            {
                _phase = DesireCyclePhase.Active;
                PickNextDesire();
            }

            return true;
        }

        public bool TryResume()
        {
            if (IsActive)
            {
                return false;
            }

            if (!HasLockedDesire)
            {
                return false;
            }

            EnsureTimerInstance();

            if (_lockedSnapshot != null)
            {
                float resumeDuration = ApplySnapshot(_lockedSnapshot.Value);
                _lockedSnapshot = null;

                resumeDuration = Mathf.Max(resumeDuration, 0.05f);
                if (resumeDuration <= 0f)
                {
                    resumeDuration = Mathf.Max(_config.DesireDuration, 0.05f);
                }

                float resumeDelay = _config.InitialDesireDelay;
                if (resumeDelay > 0f)
                {
                    _phase = DesireCyclePhase.WaitingResumeDelay;
                    _scheduledResumeDuration = resumeDuration;
                    RestartTimer(resumeDelay);

                    if (_currentDesire.HasValue)
                    {
                        DebugUtility.LogVerbose(
                            $"⌛ Desejo {_currentDesire.Value} retomará o ciclo em {resumeDelay:F2}s.",
                            context: _master,
                            instance: this);
                    }
                }
                else
                {
                    BeginActiveCycle(resumeDuration);
                }
            }

            NotifyDesireChanged();
            return true;
        }

        public bool Stop()
        {
            if (!IsActive)
            {
                return false;
            }

            _phase = DesireCyclePhase.Inactive;
            _lockedSnapshot = null;
            ClearCurrentDesire();
            _scheduledResumeDuration = 0f;

            StopTimer();

            DebugUtility.LogVerbose("🛑 Desejos do Eater pausados.", context: _master, instance: this);
            NotifyDesireChanged();
            return true;
        }

        public bool Suspend()
        {
            if (!IsActive)
            {
                return false;
            }

            _phase = DesireCyclePhase.Inactive;
            _lockedSnapshot = CaptureLockedSnapshot();
            _scheduledResumeDuration = 0f;

            StopTimer();

            DebugUtility.LogVerbose(
                "⏸️ Desejos do Eater suspensos mantendo o desejo atual.",
                context: _master,
                instance: this);

            return true;
        }

        private void EnsureTimerInstance()
        {
            if (_timer != null)
            {
                return;
            }

            float baseDuration = _config.DesireDuration;
            _timer = new CountdownTimer(baseDuration);
        }

        private void RestartTimer(float duration)
        {
            EnsureTimerInstance();
            float safeDuration = Mathf.Max(duration, 0.05f);
            _timer.Stop();
            _timer.Reset(safeDuration);
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
        }

        private void BeginActiveCycle(float duration)
        {
            float safeDuration = Mathf.Max(duration, 0.05f);
            _phase = DesireCyclePhase.Active;
            _currentDuration = safeDuration;
            RestartTimer(safeDuration);

            if (_currentDesire.HasValue)
            {
                DebugUtility.LogVerbose(
                    $"▶️ Desejo {_currentDesire.Value} retomado por {safeDuration:F2}s.",
                    context: _master,
                    instance: this);
            }
        }

        private void PickNextDesire()
        {
            if (_phase == DesireCyclePhase.Inactive)
            {
                return;
            }

            if (!TrySelectDesire(out var desire, out bool available, out int availableCount, out float selectionWeight))
            {
                DebugUtility.LogWarning(
                    "Não foi possível selecionar um desejo válido para o Eater.",
                    context: _master,
                    instance: this);
                _currentDesire = null;
                _currentDesireAvailable = false;
                _currentDuration = 0f;
                _currentDesireAvailableCount = 0;
                _currentDesireWeight = 0f;
                NotifyDesireChanged();
                return;
            }

            _currentDesire = desire;
            _currentDesireAvailable = available;
            _currentDesireAvailableCount = Mathf.Max(availableCount, 0);
            _currentDesireWeight = Mathf.Max(selectionWeight, 0f);

            float baseDuration = _config.DesireDuration;
            float unavailableFactor = _config.UnavailableDesireDurationMultiplier;
            _currentDuration = available ? baseDuration : Mathf.Max(baseDuration * unavailableFactor, 0.05f);

            RestartTimer(_currentDuration);

            PlayDesireSelectedAudio();

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

            string actorName = _actorLabel;
            string availability = available
                ? $"disponível ({_currentDesireAvailableCount} planeta(s))"
                : "indisponível";
            float timestamp = Time.timeSinceLevelLoad;
            DebugUtility.LogVerbose(
                $"✨ {actorName} deseja {desire} ({availability}) por {_currentDuration:F2}s (peso {_currentDesireWeight:F2}, t={timestamp:F2}s).",
                context: _master,
                instance: this);

            if (!available)
            {
                DebugUtility.LogVerbose(
                    $"Nenhum planeta com {desire} detectado, mantendo desejo com duração reduzida para {_currentDuration:F2}s.",
                    context: _master,
                    instance: this);
            }

            NotifyDesireChanged();
        }

        private static string ResolveActorLabel(EaterMaster master)
        {
            if (master == null)
            {
                return "Eater";
            }

            string actorName = master.ActorName;
            if (string.IsNullOrWhiteSpace(actorName))
            {
                actorName = master.name;
            }

            return string.IsNullOrWhiteSpace(actorName) ? "Eater" : actorName;
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
                foreach (var recent in _recentDesires)
                {
                    _recentLookup.Add(recent);
                }
            }

            float totalWeight = 0f;

            foreach (var resource in _resourcePool)
            {
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

                var fallbackResource = _resourcePool[UnityEngine.Random.Range(0, _resourcePool.Length)];
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

                    var candidate = _candidateBuffer[i];
                    _debugBuilder
                        .Append(candidate.Resource)
                        .Append(" (disp=").Append(candidate.IsAvailable)
                        .Append(", planetas=").Append(candidate.AvailableCount)
                        .Append(", peso=").Append(candidate.Weight.ToString("F2"))
                        .Append(')');
                }

                DebugUtility.LogVerbose(
                    _debugBuilder.ToString(),
                    context: _master,
                    instance: this,
                    deduplicate: true);
            }

            if (totalWeight <= 0f)
            {
                var fallbackCandidate = _candidateBuffer[UnityEngine.Random.Range(0, _candidateBuffer.Count)];
                desire = fallbackCandidate.Resource;
                available = fallbackCandidate.IsAvailable;
                availableCount = fallbackCandidate.AvailableCount;
                selectionWeight = fallbackCandidate.Weight;
                return true;
            }

            float roll = UnityEngine.Random.value * totalWeight;
            foreach (var candidate in _candidateBuffer)
            {
                roll -= candidate.Weight;
                if (!(roll <= 0f)) continue;
                desire = candidate.Resource;
                available = candidate.IsAvailable;
                availableCount = candidate.AvailableCount;
                selectionWeight = candidate.Weight;
                return true;
            }

            var lastCandidate = _candidateBuffer[^1];
            desire = lastCandidate.Resource;
            available = lastCandidate.IsAvailable;
            availableCount = lastCandidate.AvailableCount;
            selectionWeight = lastCandidate.Weight;
            return true;
        }

        private Dictionary<PlanetResources, int> BuildAvailabilityMap()
        {
            _availabilityBuffer.Clear();

            var manager = PlanetsManager.Instance;
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
                var resourceType = pair.Value;
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
                float baseWeight = _config.AvailableDesireWeight;
                float perPlanet = _config.PerPlanetAvailableWeight;
                weight = baseWeight + Mathf.Max(availablePlanets, 0) * perPlanet;
            }
            else
            {
                weight = _config.UnavailableDesireWeight;
            }

            if (penalizeRecent)
            {
                float multiplier = _config.RecentDesireWeightMultiplier;
                weight *= multiplier;
            }

            return Mathf.Max(weight, 0f);
        }

        private void NotifyDesireChanged()
        {
            EventDesireChanged?.Invoke(BuildDesireInfo());
        }

        private void ClearCurrentDesire()
        {
            _currentDesire = null;
            _currentDesireAvailable = false;
            _currentDuration = 0f;
            _currentDesireAvailableCount = 0;
            _currentDesireWeight = 0f;
        }

        private LockedDesireSnapshot? CaptureLockedSnapshot()
        {
            if (!_currentDesire.HasValue)
            {
                return null;
            }

            float remaining = _timer != null ? Mathf.Max(_timer.CurrentTime, 0f) : Mathf.Max(_currentDuration, 0f);
            float totalDuration = Mathf.Max(_currentDuration, 0f);

            return new LockedDesireSnapshot(
                _currentDesire.Value,
                _currentDesireAvailable,
                _currentDesireAvailableCount,
                _currentDesireWeight,
                remaining,
                totalDuration);
        }

        private float ApplySnapshot(LockedDesireSnapshot snapshot)
        {
            _currentDesire = snapshot.Resource;
            _currentDesireAvailable = snapshot.IsAvailable;
            _currentDesireAvailableCount = snapshot.AvailableCount;
            _currentDesireWeight = snapshot.Weight;
            _currentDuration = Mathf.Max(snapshot.TotalDuration, 0f);
            return Mathf.Max(snapshot.RemainingTime, 0f);
        }

        private void PlayDesireSelectedAudio()
        {
            if (_config == null)
            {
                return;
            }

            var sound = _config.DesireSelectedSound;
            if (sound == null || sound.clip == null)
            {
                if (!_missingSoundLogged)
                {
                    DebugUtility.LogVerbose(
                        "Nenhum som configurado para desejos do Eater.",
                        context: _master,
                        instance: this);
                    _missingSoundLogged = true;
                }

                return;
            }

            if (_audioEmitter == null)
            {
                if (!_missingEmitterLogged)
                {
                    DebugUtility.LogWarning(
                        "EntityAudioEmitter não encontrado — som de desejo não reproduzido.",
                        context: _master,
                        instance: this);
                    _missingEmitterLogged = true;
                }

                return;
            }

            var position = _master != null ? _master.transform.position : Vector3.zero;
            var context = AudioContext.Default(position, _audioEmitter.UsesSpatialBlend);
            _audioEmitter.Play(sound, context);
        }

        private EaterDesireInfo BuildDesireInfo()
        {
            bool serviceActive = IsActive || HasLockedDesire;
            bool hasDesire = serviceActive && _currentDesire.HasValue;
            PlanetResources? resource = hasDesire ? _currentDesire : null;
            bool available = hasDesire && _currentDesireAvailable;
            int availableCount = hasDesire ? _currentDesireAvailableCount : 0;
            float weight = hasDesire ? _currentDesireWeight : 0f;
            float duration = hasDesire ? _currentDuration : 0f;
            float remaining = IsActive && _timer != null ? Mathf.Max(_timer.CurrentTime, 0f) : 0f;

            if (!hasDesire)
            {
                remaining = 0f;
            }

            return new EaterDesireInfo(serviceActive, hasDesire, resource, available, availableCount, weight, duration, remaining);
        }

        private enum DesireCyclePhase
        {
            Inactive = 0,
            WaitingInitialDelay = 1,
            WaitingResumeDelay = 2,
            Active = 3
        }

        private readonly struct LockedDesireSnapshot
        {
            public LockedDesireSnapshot(
                PlanetResources resource,
                bool isAvailable,
                int availableCount,
                float weight,
                float remainingTime,
                float totalDuration)
            {
                Resource = resource;
                IsAvailable = isAvailable;
                AvailableCount = availableCount;
                Weight = weight;
                RemainingTime = remainingTime;
                TotalDuration = totalDuration;
            }

            public PlanetResources Resource { get; }
            public bool IsAvailable { get; }
            public int AvailableCount { get; }
            public float Weight { get; }
            public float RemainingTime { get; }
            public float TotalDuration { get; }
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
