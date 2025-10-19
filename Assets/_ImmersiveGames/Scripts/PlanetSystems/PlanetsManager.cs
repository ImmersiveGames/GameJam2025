using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Verbose)]
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        [Header("Planet Setup")]
        [Tooltip("Prefab principal de planeta que será instanciado na inicialização.")]
        [SerializeField] private PlanetsMaster planetPrefab;

        [Tooltip("Quantidade de planetas que serão criados automaticamente.")]
        [SerializeField, Min(0)] private int initialPlanetCount = 3;

        [Tooltip("Transform que receberá os planetas criados. Quando vazio, usa o próprio manager.")]
        [SerializeField] private Transform planetsRoot;

        [Header("Resources Setup")]
        [Tooltip("Lista de recursos disponíveis para sortear ao instanciar planetas.")]
        [SerializeField] private List<PlanetResourcesSo> availableResources = new();

        private IDetectable _targetToEater;
        private readonly List<IDetectable> _activePlanets = new();
        private readonly Dictionary<IPlanetActor, PlanetResources> _planetResourcesMap = new();

        protected override void Awake()
        {
            base.Awake();
            if (planetsRoot == null)
            {
                planetsRoot = transform;
            }
        }

        private void Start()
        {
            InitializePlanets();
        }

        private void InitializePlanets()
        {
            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta não configurado no PlanetsManager.");
                return;
            }

            if (_planetResourcesMap.Count > 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>("Planetas já foram inicializados anteriormente, evitando duplicação.");
                return;
            }

            for (int i = 0; i < initialPlanetCount; i++)
            {
                PlanetsMaster planetInstance = Instantiate(planetPrefab, planetsRoot);
                planetInstance.name = $"{planetPrefab.name}_{i + 1}";

                RegisterPlanet(planetInstance);
            }

            EventBus<PlanetsInitializationCompletedEvent>.Raise(
                new PlanetsInitializationCompletedEvent(new ReadOnlyDictionary<IPlanetActor, PlanetResources>(_planetResourcesMap)));
        }

        private void RegisterPlanet(PlanetsMaster planetInstance)
        {
            if (planetInstance == null)
            {
                return;
            }

            PlanetResourcesSo resource = DrawPlanetResource();
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum recurso disponível para atribuir ao planeta instanciado.");
            }

            planetInstance.AssignResource(resource);

            PlanetResources resourceType = resource != null ? resource.ResourceType : default;
            _planetResourcesMap[planetInstance] = resourceType;
        }

        private PlanetResourcesSo DrawPlanetResource()
        {
            if (availableResources == null || availableResources.Count == 0)
            {
                return null;
            }

            int randomIndex = Random.Range(0, availableResources.Count);
            return availableResources[randomIndex];
        }

        public IReadOnlyDictionary<IPlanetActor, PlanetResources> GetPlanetResourcesMap() => _planetResourcesMap;
        public IReadOnlyCollection<IPlanetActor> GetPlanetActors() => _planetResourcesMap.Keys;
        public List<IDetectable> GetActivePlanets() => _activePlanets;
        public IDetectable GetPlanetMarked() => _targetToEater;

        public bool IsMarkedPlanet(IDetectable planet) => _targetToEater == planet;
    }
}
