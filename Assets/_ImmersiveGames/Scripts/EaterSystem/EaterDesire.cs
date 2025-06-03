using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterHunger))]
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterDesire : MonoBehaviour, IResettable
    {
        [SerializeField] private EaterDesireConfigSo desireConfig;
        private PlanetResourcesSo _desiredResource;
        private float _desireChangeTimer;
        private bool _isDesireLocked;
        private bool _isDesireActive;
        private List<PlanetResourcesSo> _lastDesiredResources;
        private EventBinding<DesireActivatedEvent> _desireActivatedBinding;
        private EventBinding<DesireDeactivatedEvent> _desireDeactivatedBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void Awake()
        {
            _lastDesiredResources = new List<PlanetResourcesSo>();
            _desireChangeTimer = 0f;
            _isDesireActive = false;
            _isDesireLocked = false;
        }

        private void OnEnable()
        {
            _desireActivatedBinding = new EventBinding<DesireActivatedEvent>(OnDesireActivated);
            _desireDeactivatedBinding = new EventBinding<DesireDeactivatedEvent>(OnDesireDeactivated);
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<DesireActivatedEvent>.Register(_desireActivatedBinding);
            EventBus<DesireDeactivatedEvent>.Register(_desireDeactivatedBinding);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<DesireActivatedEvent>.Unregister(_desireActivatedBinding);
            EventBus<DesireDeactivatedEvent>.Unregister(_desireDeactivatedBinding);
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        private void Update()
        {
            if (_isDesireActive && !_isDesireLocked)
            {
                UpdateDesire();
            }
        }

        private void UpdateDesire()
        {
            _desireChangeTimer += Time.deltaTime;
            float changeInterval = GetDesireChangeInterval();
            if (_desireChangeTimer < changeInterval) return;
            ChooseNewDesire();
            _desireChangeTimer = 0f;
            DebugUtility.LogVerbose<EaterDesire>($"Eater nova vontade: {_desiredResource?.name ?? "nenhum"} (intervalo: {changeInterval}s).");
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
                EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
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

            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(_desiredResource));
        }

        private List<PlanetResourcesSo> GetAvailableResources()
        {
            return PlanetsManager.Instance.GetActivePlanets()
                .Select(p => p.GetResources())
                .Where(r => r)
                .Distinct()
                .ToList();
        }

        private void OnDesireActivated(DesireActivatedEvent evt)
        {
            _isDesireActive = true;
            ChooseNewDesire();
            DebugUtility.LogVerbose<EaterDesire>("Sistema de desejo ativado.");
        }

        private void OnDesireDeactivated(DesireDeactivatedEvent evt)
        {
            _isDesireActive = false;
            _desiredResource = null;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.LogVerbose<EaterDesire>("Sistema de desejo desativado.");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            _isDesireLocked = true;
            DebugUtility.LogVerbose<EaterDesire>($"Vontade do Eater travada: {_desiredResource?.name ?? "nenhum"}.");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            _isDesireLocked = false;
            if (_isDesireActive)
            {
                ChooseNewDesire();
            }
            DebugUtility.LogVerbose<EaterDesire>($"Vontade do Eater destravada. Nova vontade: {_desiredResource?.name ?? "nenhum"}.");
        }

        public void ConsumePlanet(PlanetResourcesSo consumedResource)
        {
            bool isDesired = consumedResource == _desiredResource;
            float hungerRestored = isDesired ? desireConfig.DesiredHungerRestored : desireConfig.NonDesiredHungerRestored;
            float healthRestored = isDesired ? desireConfig.DesiredHealthRestored : 0f;

            //TODO: Implementar lógica de consumo de planeta
            /*GetComponent<EaterHunger>().ConsumePlanet(hungerRestored);
            if (healthRestored > 0)
            {
                GetComponent<EaterHealth>().Increase(healthRestored);
            }*/

            if (isDesired)
            {
                EventBus<EaterConsumptionSatisfiedEvent>.Raise(new EaterConsumptionSatisfiedEvent(consumedResource, hungerRestored));
                DebugUtility.LogVerbose<EaterDesire>($"Eater consumiu planeta com recurso desejado ({consumedResource.name}): +{hungerRestored} fome, +{healthRestored} HP.");
            }
            else
            {
                EventBus<EaterConsumptionUnsatisfiedEvent>.Raise(new EaterConsumptionUnsatisfiedEvent(consumedResource, hungerRestored));
                DebugUtility.LogVerbose<EaterDesire>($"Eater consumiu planeta com recurso indesejado ({consumedResource.name}): +{hungerRestored} fome, sem HP.");
            }

            _desiredResource = null;
            _isDesireLocked = false;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            if (_isDesireActive)
            {
                ChooseNewDesire();
            }
        }

        public PlanetResourcesSo GetDesiredResource() => _desiredResource;
        public EaterDesireConfigSo DesireConfig => desireConfig;

        public void Reset()
        {
            _desiredResource = null;
            _isDesireActive = false;
            _isDesireLocked = false;
            _lastDesiredResources.Clear();
            _desireChangeTimer = 0f;
            EventBus<DesireChangedEvent>.Raise(new DesireChangedEvent(null));
            DebugUtility.LogVerbose<EaterDesire>("EaterDesire resetado.");
        }
    }
}