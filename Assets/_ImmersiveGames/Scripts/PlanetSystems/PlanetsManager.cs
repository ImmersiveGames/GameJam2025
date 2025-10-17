using System.Collections.Generic;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    /// <summary>
    /// Gerencia o ciclo de vida dos planetas e mantém um dicionário de recursos associados.
    /// </summary>
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Verbose)]
    public sealed class PlanetsManager : Singleton<PlanetsManager>
    {
        [Header("Spawn Settings")]
        [SerializeField] private PlanetsMaster planetPrefab;
        [SerializeField, Min(0)] private int initialPlanetCount = 1;
        [SerializeField] private Transform planetsParent;
        [SerializeField] private GameObject planetCanvasPrefab;

        [Header("Resources")]
        [SerializeField] private List<PlanetResourcesSo> planetResources = new();

        private readonly List<PlanetsMaster> _spawnedPlanets = new();
        private readonly Dictionary<PlanetsMaster, PlanetResourcesSo> _planetResourcesMap = new();

        private EventBinding<PlanetCreatedEvent> _planetCreatedBinding;
        private EventBinding<PlanetDestroyedEvent> _planetDestroyedBinding;
        private EventBinding<PlanetResourceAssignedEvent> _resourceAssignedBinding;
        private EventBinding<PlanetResourceClearedEvent> _resourceClearedBinding;

        private void OnEnable()
        {
            _planetCreatedBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreatedBinding);

            _planetDestroyedBinding = new EventBinding<PlanetDestroyedEvent>(OnPlanetDestroyed);
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyedBinding);

            _resourceAssignedBinding = new EventBinding<PlanetResourceAssignedEvent>(OnPlanetResourceAssigned);
            EventBus<PlanetResourceAssignedEvent>.Register(_resourceAssignedBinding);

            _resourceClearedBinding = new EventBinding<PlanetResourceClearedEvent>(OnPlanetResourceCleared);
            EventBus<PlanetResourceClearedEvent>.Register(_resourceClearedBinding);
        }

        private void Start()
        {
            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta não definido. Configure o PlanetPrefab no inspector.", this);
                return;
            }

            if (initialPlanetCount <= 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Nenhum planeta configurado para spawn inicial ({nameof(initialPlanetCount)} = {initialPlanetCount}).", "yellow", this);
                return;
            }

            SpawnPlanets(initialPlanetCount);
        }

        private void OnDisable()
        {
            if (_planetCreatedBinding != null)
            {
                EventBus<PlanetCreatedEvent>.Unregister(_planetCreatedBinding);
            }

            if (_planetDestroyedBinding != null)
            {
                EventBus<PlanetDestroyedEvent>.Unregister(_planetDestroyedBinding);
            }

            if (_resourceAssignedBinding != null)
            {
                EventBus<PlanetResourceAssignedEvent>.Unregister(_resourceAssignedBinding);
            }

            if (_resourceClearedBinding != null)
            {
                EventBus<PlanetResourceClearedEvent>.Unregister(_resourceClearedBinding);
            }
        }

        private void SpawnPlanets(int count)
        {
            var parent = planetsParent != null ? planetsParent : transform;

            for (int i = 0; i < count; i++)
            {
                var planetInstance = Instantiate(planetPrefab, parent);
                planetInstance.transform.localPosition = Vector3.zero;
                planetInstance.transform.localRotation = Quaternion.identity;
                planetInstance.transform.localScale = Vector3.one;

                EnsureResourceCanvas(planetInstance);

                var detectable = planetInstance.GetComponent<IDetectable>();
                if (detectable != null)
                {
                    EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(detectable));
                }
                else
                {
                    RegisterPlanet(planetInstance);
                }
            }
        }

        private void EnsureResourceCanvas(PlanetsMaster planet)
        {
            if (planetCanvasPrefab == null)
            {
                return;
            }

            var controller = planet.GetResourceController();
            if (controller == null)
            {
                return;
            }

            var existingViews = planet.GetComponentsInChildren<PlanetResourceCanvasView>(true);
            if (existingViews.Length > 0)
            {
                foreach (var view in existingViews)
                {
                    view.SetController(controller);
                }

                return;
            }

            var canvasInstance = Instantiate(planetCanvasPrefab, planet.transform);
            canvasInstance.transform.localPosition = Vector3.zero;
            canvasInstance.transform.localRotation = Quaternion.identity;
            canvasInstance.transform.localScale = Vector3.one;

            var canvasViews = canvasInstance.GetComponentsInChildren<PlanetResourceCanvasView>(true);

            foreach (var view in canvasViews)
            {
                view.SetController(controller);
            }
        }

        public bool RegisterPlanet(PlanetsMaster planetMaster, PlanetResourcesSo resourceOverride = null)
        {
            if (planetMaster == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Tentativa de registrar um planeta nulo.", this);
                return false;
            }

            if (_spawnedPlanets.Contains(planetMaster))
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.name} já está registrado.", "yellow", this);
                return false;
            }

            var controller = planetMaster.GetResourceController();
            if (controller == null)
            {
                DebugUtility.LogError<PlanetsManager>($"Planeta {planetMaster.name} não possui {nameof(PlanetResourceController)}.", this);
                return false;
            }

            _spawnedPlanets.Add(planetMaster);

            EnsureResourceCanvas(planetMaster);

            var resource = resourceOverride ?? GetRandomResource();
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetsManager>($"Nenhum recurso disponível para atribuir ao planeta {planetMaster.name}.", this);
                return true;
            }

            controller.AssignResource(resource);
            return true;
        }

        public void UnregisterPlanet(PlanetsMaster planetMaster)
        {
            if (planetMaster == null)
            {
                return;
            }

            _spawnedPlanets.Remove(planetMaster);
            _planetResourcesMap.Remove(planetMaster);
        }

        public IReadOnlyDictionary<PlanetsMaster, PlanetResourcesSo> GetPlanetResources() => _planetResourcesMap;

        public PlanetResourcesSo GetResource(PlanetsMaster planet)
        {
            return planet != null && _planetResourcesMap.TryGetValue(planet, out var resource) ? resource : null;
        }

        [ContextMenu("Log Spawned Planets Resources")]
        private void LogResources()
        {
            if (_planetResourcesMap.Count == 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>("Nenhum planeta registrado no momento.", "yellow", this);
                return;
            }

            foreach (var pair in _planetResourcesMap)
            {
                var planetName = pair.Key != null ? pair.Key.name : "<null>";
                var resourceName = pair.Value != null ? pair.Value.ResourceId.ToString() : "<none>";
                DebugUtility.Log<PlanetsManager>($"Planeta {planetName} => Recurso {resourceName}", "cyan", this);
            }
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

        private void OnPlanetCreated(PlanetCreatedEvent evt)
        {
            if (evt?.Detected == null)
            {
                DebugUtility.LogVerbose<PlanetsManager>("PlanetCreatedEvent recebido sem IDetectable válido.", "yellow", this);
                return;
            }

            if (evt.Detected.Owner == null)
            {
                DebugUtility.LogVerbose<PlanetsManager>("PlanetCreatedEvent sem Owner definido.", "yellow", this);
                return;
            }

            var ownerTransform = evt.Detected.Owner.Transform;
            if (ownerTransform == null)
            {
                DebugUtility.LogVerbose<PlanetsManager>("Owner sem Transform ao processar PlanetCreatedEvent.", "yellow", this);
                return;
            }

            var planetMaster = ownerTransform.GetComponent<PlanetsMaster>();
            if (planetMaster == null)
            {
                DebugUtility.LogVerbose<PlanetsManager>("PlanetCreatedEvent recebido sem PlanetsMaster válido.", "yellow", this);
                return;
            }

            RegisterPlanet(planetMaster);
        }

        private void OnPlanetDestroyed(PlanetDestroyedEvent evt)
        {
            if (evt?.Detected?.Owner == null)
            {
                return;
            }

            var ownerTransform = evt.Detected.Owner.Transform;
            if (ownerTransform == null)
            {
                return;
            }

            var planetMaster = ownerTransform.GetComponent<PlanetsMaster>();
            if (planetMaster != null)
            {
                UnregisterPlanet(planetMaster);
            }
        }

        private void OnPlanetResourceAssigned(PlanetResourceAssignedEvent evt)
        {
            if (evt.Planet == null)
            {
                return;
            }

            _planetResourcesMap[evt.Planet] = evt.Resource;
        }

        private void OnPlanetResourceCleared(PlanetResourceClearedEvent evt)
        {
            if (evt.Planet == null)
            {
                return;
            }

            if (_planetResourcesMap.TryGetValue(evt.Planet, out var currentResource) && currentResource == evt.PreviousResource)
            {
                _planetResourcesMap.Remove(evt.Planet);
            }
        }
    }
}
