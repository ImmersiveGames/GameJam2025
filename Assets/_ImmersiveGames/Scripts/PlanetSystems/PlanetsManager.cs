using System.Collections.Generic;
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

        private IDetectable _targetToEater;
        private readonly List<IDetectable> _activePlanets = new();
        private readonly List<IPlanetActor> _planetActors = new();

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

            if (_planetActors.Count > 0)
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
                new PlanetsInitializationCompletedEvent(_planetActors.AsReadOnly()));
        }

        private void RegisterPlanet(PlanetsMaster planetInstance)
        {
            if (planetInstance == null)
            {
                return;
            }

            _planetActors.Add(planetInstance);
        }

        public IReadOnlyList<IPlanetActor> GetPlanetActors() => _planetActors;
        public List<IDetectable> GetActivePlanets() => _activePlanets;
        public IDetectable GetPlanetMarked() => _targetToEater;

        public bool IsMarkedPlanet(IDetectable planet) => _targetToEater == planet;
    }
}
