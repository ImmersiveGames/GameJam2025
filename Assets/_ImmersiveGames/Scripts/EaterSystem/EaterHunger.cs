using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterAIController))]
    public class EaterHunger : ResourceSystem, IResettable
    {
        [SerializeField] private EaterDesireConfigSO desireConfig;
        private PlanetResourcesSo desiredResource;
        private float desireChangeTimer;
        private bool isDesireLocked;
        private bool isBelowThreshold;
        private List<PlanetResourcesSo> lastDesiredResources;
        private EventBinding<PlanetMarkedEvent> planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> planetUnmarkedBinding;

        protected override void Awake()
        {
            base.Awake();
            lastDesiredResources = new List<PlanetResourcesSo>();
            desireChangeTimer = 0f;
            onThresholdReached.AddListener(OnThresholdReached);
            onDepleted.AddListener(OnStarved);
        }

        private void OnEnable()
        {
            planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(OnPlanetMarked);
            planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(OnPlanetUnmarked);
            EventBus<PlanetMarkedEvent>.Register(planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(planetUnmarkedBinding);
        }

        protected override void Update()
        {
            base.Update();
            if (isBelowThreshold && !isDesireLocked)
            {
                UpdateDesire();
            }
        }

        private void UpdateDesire()
        {
            desireChangeTimer += Time.deltaTime;
            float changeInterval = GetDesireChangeInterval();
            if (desireChangeTimer >= changeInterval)
            {
                ChooseNewDesire();
                desireChangeTimer = 0f;
                Debug.Log($"Eater nova vontade: {desiredResource?.name ?? "nenhum"} (intervalo: {changeInterval}s).");
            }
        }

        private float GetDesireChangeInterval()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            return availableResources.Contains(desiredResource) ?
                desireConfig.DesireChangeInterval :
                desireConfig.NoResourceDesireChangeInterval;
        }

        private void ChooseNewDesire()
        {
            List<PlanetResourcesSo> availableResources = GetAvailableResources();
            if (availableResources.Count == 0)
            {
                desiredResource = null;
                return;
            }

            List<PlanetResourcesSo> candidates = availableResources
                .Where(r => !lastDesiredResources.Contains(r))
                .ToList();

            if (candidates.Count == 0)
            {
                lastDesiredResources.Clear();
                candidates = availableResources;
            }

            desiredResource = candidates[Random.Range(0, candidates.Count)];
            lastDesiredResources.Add(desiredResource);
            if (lastDesiredResources.Count > desireConfig.MaxRecentDesires)
            {
                lastDesiredResources.RemoveAt(0);
            }

            EventBus<EaterDesireChangedEvent>.Raise(new EaterDesireChangedEvent(desiredResource));
        }

        private List<PlanetResourcesSo> GetAvailableResources()
        {
            return PlanetsManager.Instance.GetActivePlanets()
                .Select(p => p.GetResources())
                .Where(r => r != null)
                .Distinct()
                .ToList();
        }

        private void OnThresholdReached(float threshold)
        {
            if (threshold == config.Thresholds.FirstOrDefault(t => t == 0.5f))
            {
                isBelowThreshold = true;
                ChooseNewDesire();
                Debug.Log($"Eater atingiu limiar de fome ({threshold * 100}%): vontade ativada.");
            }
            else if (GetPercentage() > 0.5f)
            {
                isBelowThreshold = false;
                desiredResource = null;
                lastDesiredResources.Clear();
                desireChangeTimer = 0f;
                EventBus<EaterDesireChangedEvent>.Raise(new EaterDesireChangedEvent(null));
                Debug.Log("Eater acima de 50% de fome: vontade desativada.");
            }
        }

        private void OnStarved()
        {
            EventBus<EaterStarvedEvent>.Raise(new EaterStarvedEvent());
            Debug.Log("Eater morreu de fome! Fim de jogo.");
        }

        private void OnPlanetMarked(PlanetMarkedEvent evt)
        {
            isDesireLocked = true;
            Debug.Log($"Vontade do Eater travada: {desiredResource?.name ?? "nenhum"}.");
        }

        private void OnPlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            isDesireLocked = false;
            if (isBelowThreshold)
            {
                ChooseNewDesire();
            }
            Debug.Log($"Vontade do Eater destravada. Nova vontade: {desiredResource?.name ?? "nenhum"}.");
        }

        public void ConsumePlanet(PlanetResourcesSo consumedResource)
        {
            bool isDesired = consumedResource == desiredResource;
            float hungerRestored = isDesired ? desireConfig.DesiredHungerRestored : desireConfig.NonDesiredHungerRestored;
            Increase(hungerRestored);

            if (isDesired)
            {
                EventBus<EaterConsumptionSatisfiedEvent>.Raise(new EaterConsumptionSatisfiedEvent(consumedResource, hungerRestored));
                Debug.Log($"Eater consumiu planeta com recurso desejado ({consumedResource.name}): +{hungerRestored} fome.");
            }
            else
            {
                EventBus<EaterConsumptionUnsatisfiedEvent>.Raise(new EaterConsumptionUnsatisfiedEvent(consumedResource, hungerRestored));
                Debug.Log($"Eater consumiu planeta com recurso indesejado ({consumedResource.name}): +{hungerRestored} fome, sem HP.");
            }

            desiredResource = null;
            isDesireLocked = false;
            lastDesiredResources.Clear();
            desireChangeTimer = 0f;
            if (isBelowThreshold)
            {
                ChooseNewDesire();
            }
        }

        public PlanetResourcesSo GetDesiredResource() => desiredResource;
        public EaterDesireConfigSO DesireConfig => desireConfig;

        public void Reset()
        {
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            desiredResource = null;
            isBelowThreshold = false;
            isDesireLocked = false;
            lastDesiredResources.Clear();
            desireChangeTimer = 0f;
            onValueChanged.Invoke(GetPercentage());
            CheckThresholds();
            Debug.Log("EaterHunger resetado.");
        }
        
        
    }
}