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
        [Header("Spawn Settings")]
        [SerializeField] private GameObject planetPrefab;
        [SerializeField] private Transform planetsParent;
        [SerializeField, Min(0)] private int initialPlanetCount = 0;
        [SerializeField] private bool spawnOnStart = true;

        [Header("Resources")]
        [SerializeField] private List<PlanetResourcesSo> planetResources = new();

        private readonly Dictionary<PlanetsMaster, PlanetResourcesSo> _resourceLookup = new();
        private readonly List<IDetectable> _activePlanets = new();
        private IDetectable _targetToEater;

        private EventBinding<PlanetResourceChangedEvent> _resourceChangedBinding;
        private EventBinding<PlanetDestroyedEvent> _planetDestroyedBinding;

        private void Awake()
        {
            ValidateConfiguration();
        }

        private void OnEnable()
        {
            _resourceChangedBinding ??= new EventBinding<PlanetResourceChangedEvent>(OnPlanetResourceChanged);
            EventBus<PlanetResourceChangedEvent>.Register(_resourceChangedBinding);

            _planetDestroyedBinding ??= new EventBinding<PlanetDestroyedEvent>(OnPlanetDestroyed);
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyedBinding);
        }

        private void Start()
        {
            if (spawnOnStart && initialPlanetCount > 0)
            {
                SpawnPlanets(initialPlanetCount);
            }
        }

        private void OnDisable()
        {
            if (_resourceChangedBinding != null)
            {
                EventBus<PlanetResourceChangedEvent>.Unregister(_resourceChangedBinding);
            }

            if (_planetDestroyedBinding != null)
            {
                EventBus<PlanetDestroyedEvent>.Unregister(_planetDestroyedBinding);
            }
        }

        private void OnValidate()
        {
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (planetPrefab == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Prefab de planeta não configurado no PlanetsManager.", this);
            }

            if (planetResources == null || planetResources.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum PlanetResourcesSo configurado; atribuições aleatórias não serão possíveis.", this);
            }
        }

        public void SpawnPlanets(int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta ausente. Não é possível instanciar novos planetas.", this);
                return;
            }

            var parent = planetsParent != null ? planetsParent : transform;

            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(planetPrefab, parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                RegisterSpawnedPlanet(instance);
            }
        }

        private void RegisterSpawnedPlanet(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!instance.TryGetComponent(out PlanetsMaster planetMaster))
            {
                DebugUtility.LogError<PlanetsManager>($"Prefab {instance.name} não possui PlanetsMaster. A instância será destruída.", this);
                Destroy(instance);
                return;
            }

            var detectable = instance.GetComponent<IDetectable>();
            if (detectable != null && !_activePlanets.Contains(detectable))
            {
                _activePlanets.Add(detectable);
                EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(detectable));
            }

            AssignRandomResource(planetMaster);
        }

        private void AssignRandomResource(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            var controller = planet.ResourceController;
            if (controller == null)
            {
                DebugUtility.LogWarning<PlanetsManager>($"Planeta {planet.ActorName} não possui PlanetResourceController.", this);
                return;
            }

            var resource = GetRandomResource();
            if (resource == null)
            {
                controller.ClearResource();
                return;
            }

            controller.AssignResource(resource);
        }

        private PlanetResourcesSo GetRandomResource()
        {
            if (planetResources == null || planetResources.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, planetResources.Count);
            return planetResources[index];
        }

        private void OnPlanetResourceChanged(PlanetResourceChangedEvent evt)
        {
            if (evt.Planet == null)
            {
                return;
            }

            if (evt.HasResource)
            {
                _resourceLookup[evt.Planet] = evt.Resource;
                DebugUtility.LogVerbose<PlanetsManager>($"Recurso {evt.Resource.name} associado ao planeta {evt.Planet.ActorName}.", "green", this);
            }
            else
            {
                _resourceLookup.Remove(evt.Planet);
                DebugUtility.LogVerbose<PlanetsManager>($"Recurso removido do planeta {evt.Planet.ActorName}.", "yellow", this);
            }
        }

        private void OnPlanetDestroyed(PlanetDestroyedEvent evt)
        {
            if (evt.Detected != null)
            {
                _activePlanets.Remove(evt.Detected);

                if (evt.Detected.Owner is PlanetsMaster master)
                {
                    _resourceLookup.Remove(master);
                }
            }
        }

        public bool TryGetResource(PlanetsMaster planet, out PlanetResourcesSo resource) => _resourceLookup.TryGetValue(planet, out resource);

        public IReadOnlyDictionary<PlanetsMaster, PlanetResourcesSo> GetResourceLookup() => _resourceLookup;

        public List<IDetectable> GetActivePlanets() => _activePlanets;

        public IDetectable GetPlanetMarked() => _targetToEater;

        public void RemovePlanet(IDetectable planet)
        {
            if (planet == null)
            {
                return;
            }

            if (_activePlanets.Remove(planet) && planet.Owner is PlanetsMaster master)
            {
                _resourceLookup.Remove(master);
            }
        }

        [ContextMenu("Log Spawned Planets Resources")]
        private void LogSpawnedPlanetsResources()
        {
            if (_resourceLookup.Count == 0)
            {
                DebugUtility.Log<PlanetsManager>("Nenhum planeta com recurso registrado.", "yellow", this);
                return;
            }

            foreach (var pair in _resourceLookup)
            {
                var resourceName = pair.Value != null ? pair.Value.name : "Nenhum";
                DebugUtility.Log<PlanetsManager>($"Planeta {pair.Key.ActorName} -> Recurso {resourceName}", "cyan", this);
            }
        }
    }
}
