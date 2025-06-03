using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterAIController))]
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterHunger : ResourceSystem, IResettable
    {
        [SerializeField] private EaterDesireConfigSo desireConfig;
        private PlanetResourcesSo _desiredResource;
        private float _desireChangeTimer;
        private bool _isDesireLocked;
        private bool _isBelowThreshold;
        private List<PlanetResourcesSo> _lastDesiredResources;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        protected override void Awake()
        {
            base.Awake();
            _lastDesiredResources = new List<PlanetResourcesSo>();
            _desireChangeTimer = 0f;
            onThresholdReached.AddListener(OnThresholdReached);
            onDepleted.AddListener(OnStarved);
        }

        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        protected override void Update()
        {
            base.Update();
            if (_isBelowThreshold && !_isDesireLocked)
            {
                UpdateDesire();
            }
        }

        private void UpdateDesire()
        {
            _desireChangeTimer += Time.deltaTime;
            float changeInterval = GetDesireChangeInterval();
            if (!(_desireChangeTimer >= changeInterval)) return;
            ChooseNewDesire();
            _desireChangeTimer = 0f;
            DebugUtility.Log<EaterHunger>($"Eater nova vontade: {_desiredResource?.name ?? "nenhum"} (intervalo: {changeInterval}s).");
        }

        private float GetDesireChangeInterval()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            return availableResources.Contains(_desiredResource) ?
                desireConfig.DesireChangeInterval :
                desireConfig.NoResourceDesireChangeInterval;
        }

        private void ChooseNewDesire()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            if (availableResources.Count == 0)
            {
                _desiredResource = null;
                return;
            }

            var candidates = availableResources
                .Where(r => !_lastDesiredResources.Contains(r))
                .ToList();

            if (candidates.Count == 0)
            {
                _lastDesiredResources.Clear();
                candidates = availableResources;
            }

            _desiredResource = candidates[Random.Range(0, candidates.Count)];
            _lastDesiredResources.Add(_desiredResource);
            if (_lastDesiredResources.Count > desireConfig.MaxRecentDesires)
            {
                _lastDesiredResources.RemoveAt(0);
            }

            EventBus<EaterDesireChangedEvent>.Raise(new EaterDesireChangedEvent(_desiredResource));
        }

        private List<PlanetResourcesSo> GetAvailableResources()
        {
            return PlanetsManager.Instance.GetActivePlanets()
                .Select(p => p.GetResources())
                .Where(r => r)
                .Distinct()
                .ToList();
        }

        private void OnThresholdReached(float threshold)
        {
            if (Mathf.Approximately(threshold, config.Thresholds.FirstOrDefault(t => Mathf.Approximately(t, 0.5f))))
            {
                _isBelowThreshold = true;
                ChooseNewDesire();
                DebugUtility.Log<EaterHunger>($"Eater atingiu limiar de fome ({threshold * 100}%): vontade ativada.");
            }
            else if (GetPercentage() > 0.5f)
            {
                _isBelowThreshold = false;
                _desiredResource = null;
                _lastDesiredResources.Clear();
                _desireChangeTimer = 0f;
                EventBus<EaterDesireChangedEvent>.Raise(new EaterDesireChangedEvent(null));
                DebugUtility.Log<EaterHunger>("Eater acima de 50% de fome: vontade desativada.");
            }
        }

        private void OnStarved()
        {
            EventBus<EaterStarvedEvent>.Raise(new EaterStarvedEvent());
            DebugUtility.Log<EaterHunger>("Eater morreu de fome! Fim de jogo.");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            _isDesireLocked = true;
            DebugUtility.Log<EaterHunger>($"Vontade do Eater travada: {_desiredResource?.name ?? "nenhum"}.");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            _isDesireLocked = false;
            if (_isBelowThreshold)
            {
                ChooseNewDesire();
            }
            DebugUtility.Log<EaterHunger>($"Vontade do Eater destravada. Nova vontade: {_desiredResource?.name ?? "nenhum"}.");
        }

        public void ConsumePlanet(PlanetResourcesSo consumedResource)
        {
            bool isDesired = consumedResource == _desiredResource;
            float hungerRestored = isDesired ? desireConfig.DesiredHungerRestored : desireConfig.NonDesiredHungerRestored;
            Increase(hungerRestored);

            if (isDesired)
            {
                EventBus<EaterConsumptionSatisfiedEvent>.Raise(new EaterConsumptionSatisfiedEvent(consumedResource, hungerRestored));
                DebugUtility.Log<EaterHunger>($"Eater consumiu planeta com recurso desejado ({consumedResource.name}): +{hungerRestored} fome.");
            }
            else
            {
                EventBus<EaterConsumptionUnsatisfiedEvent>.Raise(new EaterConsumptionUnsatisfiedEvent(consumedResource, hungerRestored));
                DebugUtility.Log<EaterHunger>($"Eater consumiu planeta com recurso indesejado ({consumedResource.name}): +{hungerRestored} fome, sem HP.");
            }

            _desiredResource = null;
            _isDesireLocked = false;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            if (_isBelowThreshold)
            {
                ChooseNewDesire();
            }
        }

        public PlanetResourcesSo GetDesiredResource() => _desiredResource;
        public EaterDesireConfigSo DesireConfig => desireConfig;

        public void Reset()
        {
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            _desiredResource = null;
            _isBelowThreshold = false;
            _isDesireLocked = false;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            onValueChanged.Invoke(GetPercentage());
            CheckThresholds();
            DebugUtility.Log<EaterHunger>("EaterHunger resetado.");
        }
        
        
    }
}