using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Verbose)]
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        [SerializeField] private PlanetsMaster planetPrefab;
        [SerializeField, Min(0)] private int initialPlanetCount = 1;
        [SerializeField] private Transform planetsParent;
        [SerializeField] private List<PlanetResourcesSo> planetResources = new();

        private IDetectable _targetToEater;
        private readonly List<IDetectable> _activePlanets = new();
        private readonly List<PlanetsMaster> _spawnedPlanets = new();
        private readonly Dictionary<PlanetsMaster, PlanetResourcesSo> _planetResourcesMap = new();

        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        private EventBinding<PlanetCreatedEvent> _planetCreatedBinding;
        private EventBinding<PlanetDestroyedEvent> _planetDestroyedBinding;
        private EventBinding<PlanetResourceAssignedEvent> _resourceAssignedBinding;
        private EventBinding<PlanetResourceClearedEvent> _resourceClearedBinding;
        // private EventBinding<OrbitsSpawnedEvent> _orbitsSpawnedBinding;

        private void Start()
        {
            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta não definido. Configure o PlanetPrefab no inspector.", this);
                return;
            }

            if (initialPlanetCount <= 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>(
                    $"Nenhum planeta configurado para spawn inicial ({nameof(initialPlanetCount)} = {initialPlanetCount}).",
                    "yellow",
                    this);
                return;
            }

            SpawnPlanets(initialPlanetCount);
        }

        private void OnEnable()
        {
            /*_planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(MarkPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(ClearMarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);*/

            //_orbitsSpawnedBinding = new EventBinding<OrbitsSpawnedEvent>(OnOrbitsSpawned);
            //EventBus<OrbitsSpawnedEvent>.Register(_orbitsSpawnedBinding);

            _planetCreatedBinding = new EventBinding<PlanetCreatedEvent>(OnPlanetCreated);
            EventBus<PlanetCreatedEvent>.Register(_planetCreatedBinding);

            _planetDestroyedBinding = new EventBinding<PlanetDestroyedEvent>(OnPlanetDestroyed);
            EventBus<PlanetDestroyedEvent>.Register(_planetDestroyedBinding);

            _resourceAssignedBinding = new EventBinding<PlanetResourceAssignedEvent>(OnPlanetResourceAssigned);
            EventBus<PlanetResourceAssignedEvent>.Register(_resourceAssignedBinding);

            _resourceClearedBinding = new EventBinding<PlanetResourceClearedEvent>(OnPlanetResourceCleared);
            EventBus<PlanetResourceClearedEvent>.Register(_resourceClearedBinding);
        }

        private void OnDisable()
        {
            if (_planetMarkedBinding != null)
            {
                EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            }

            if (_planetUnmarkedBinding != null)
            {
                EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            }

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

            //EventBus<OrbitsSpawnedEvent>.Unregister(_orbitsSpawnedBinding);
        }

        private void OnValidate()
        {
            if (planetResources == null || planetResources.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetsManager)}] PlanetResources está vazio! Adicione pelo menos um recurso.");
            }

            if (planetPrefab == null)
            {
                Debug.LogWarning($"[{nameof(PlanetsManager)}] PlanetPrefab não definido! Atribua um prefab como Planet1.");
            }
        }

        /*private void OnOrbitsSpawned(OrbitsSpawnedEvent evt)
        {
            DebugUtility.LogVerbose<PlanetsManager>($"Received OrbitsSpawnedEvent with {evt.SpawnedObjects.Count} objects from SpawnSystem {evt.SpawnSystem?.name ?? "null"}.", "cyan", this);

            foreach (var poolable in evt.SpawnedObjects)
            {
                var planetMaster = poolable.GetGameObject().GetComponent<PlanetsMaster>();
                if (planetMaster != null && !_activePlanets.Contains(planetMaster))
                {
                    _activePlanets.Add(planetMaster);
                    DebugUtility.Log<PlanetsManager>($"Planeta {poolable.GetGameObject().name} adicionado à lista de ativos com recurso {planetMaster.GetResource()?.ResourceType }.", "green", this);
                }
            }
        }*/

        public List<PlanetResourcesSo> GenerateResourceList(int numPlanets)
        {
            if (planetResources == null || planetResources.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum recurso disponível!", this);
                return new List<PlanetResourcesSo>();
            }

            var resourceList = new List<PlanetResourcesSo>();
            for (int i = 0; i < numPlanets; i++)
            {
                var resource = GetRandomResource();
                if (resource != null)
                {
                    resourceList.Add(resource);
                }
            }
            DebugUtility.Log<PlanetsManager>($"Gerada lista de recursos com {resourceList.Count} itens para {numPlanets} planetas.", "cyan", this);
            return resourceList.OrderBy(_ => Random.value).ToList();
        }

        public bool RegisterPlanet(PlanetsMaster planetMaster, PlanetResourcesSo resourceOverride = null)
        {
            if (planetMaster == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Tentativa de registrar um planeta nulo.", this);
                return false;
            }

            if (_planetResourcesMap.ContainsKey(planetMaster))
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.name} já possui recurso registrado.", "yellow", this);
                return true;
            }

            var resourceModule = planetMaster.GetResourceModule();
            if (resourceModule == null)
            {
                DebugUtility.LogError<PlanetsManager>($"Planeta {planetMaster.name} não possui PlanetResourceModule.", this);
                return false;
            }

            if (!_spawnedPlanets.Contains(planetMaster))
            {
                _spawnedPlanets.Add(planetMaster);
            }

            var currentResource = resourceModule.CurrentResource;
            var resource = resourceOverride ?? GetRandomResource(currentResource);
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetsManager>($"Nenhum recurso disponível para atribuir ao planeta {planetMaster.name}.", this);
                return false;
            }

            if (currentResource != null && currentResource != resource)
            {
                DebugUtility.LogVerbose<PlanetsManager>(
                    $"Substituindo recurso {currentResource.ResourceType} do planeta {planetMaster.name} por {resource.ResourceType}.",
                    "cyan",
                    this);
            }

            resourceModule.AssignResource(resource);
            return true;
        }

        public void UnregisterPlanet(PlanetsMaster planetMaster)
        {
            if (planetMaster == null)
            {
                return;
            }

            var resourceModule = planetMaster.GetResourceModule();
            bool removed = _planetResourcesMap.Remove(planetMaster);
            resourceModule?.ClearResource();
            _spawnedPlanets.Remove(planetMaster);

            if (removed)
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.name} removido do mapa de recursos.", "yellow", this);
            }
        }

        public bool TryGetResource(PlanetsMaster planetMaster, out PlanetResourcesSo resource)
        {
            return _planetResourcesMap.TryGetValue(planetMaster, out resource);
        }

        public IReadOnlyDictionary<PlanetsMaster, PlanetResourcesSo> GetPlanetResourceMap() => _planetResourcesMap;

        public bool IsMarkedPlanet(IDetectable planetMaster)
        {
            if (planetMaster == null) return false;
            if (!_activePlanets.Contains(planetMaster)) return false;
            DebugUtility.LogVerbose<PlanetsManager>($"Verificando se {planetMaster.Owner.ActorName ?? "nulo"} está marcado: {_targetToEater == planetMaster}.", "cyan", this);
            return _targetToEater == planetMaster;
        }

        public void RemovePlanet(IDetectable planetMaster)
        {
            if (planetMaster == null) return;
            if (!_activePlanets.Remove(planetMaster)) return;
            DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.Owner.ActorName} removido. Planetas ativos: {_activePlanets.Count}.", "yellow", this);

            if (planetMaster.Owner is PlanetsMaster master)
            {
                UnregisterPlanet(master);
            }
        }

        /*private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (evt.Detected == null) return;
            if (_targetToEater == evt.Detected) return;
            if (_targetToEater != null)
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(_targetToEater));
            }
            _targetToEater = evt.Detected;
            DebugUtility.Log<PlanetsManager>($"Planeta marcado: {evt.Detected.Owner.ActorName}", "yellow", this);
        }

        private void ClearMarkedPlanet(PlanetUnmarkedEvent evt)
        {
            if (evt.Detected == null) return;
            _targetToEater = null;
            DebugUtility.Log<PlanetsManager>($"Planeta desmarcado: {evt.Detected.Owner.ActorName}", "cyan", this);
        }*/

        public List<IDetectable> GetActivePlanets() => _activePlanets;

        public IDetectable GetPlanetMarked() => _targetToEater;

        public IReadOnlyList<PlanetsMaster> GetSpawnedPlanets() => _spawnedPlanets;

        public void SpawnPlanets(int amount)
        {
            if (amount <= 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Solicitado spawn de {amount} planetas. Nada será feito.", "yellow", this);
                return;
            }

            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta não definido. Configure o PlanetPrefab no inspector.", this);
                return;
            }

            var parent = planetsParent != null ? planetsParent : transform;
            var spawnPosition = parent.position;
            var spawnRotation = parent.rotation;

            for (int i = 0; i < amount; i++)
            {
                var instance = Instantiate(planetPrefab, spawnPosition, spawnRotation, parent);

                var spawnIndex = _spawnedPlanets.Count + 1;
                instance.name = $"{planetPrefab.name}_{spawnIndex}";

                var detectable = instance.GetComponent<IDetectable>() ?? instance.GetComponentInChildren<IDetectable>();
                if (detectable == null)
                {
                    DebugUtility.LogWarning<PlanetsManager>(
                        $"Prefab {planetPrefab.name} não possui um componente que implemente {nameof(IDetectable)}.",
                        this);
                    RegisterPlanet(instance);
                    continue;
                }

                EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(detectable));
            }
        }

        private PlanetResourcesSo GetRandomResource(PlanetResourcesSo resourceToExclude = null)
        {
            if (planetResources == null || planetResources.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Lista de PlanetResources está vazia.", this);
                return null;
            }

            var validResources = new List<PlanetResourcesSo>();
            foreach (var resource in planetResources)
            {
                if (resource == null)
                {
                    continue;
                }

                if (resourceToExclude != null && planetResources.Count > 1 && resource == resourceToExclude)
                {
                    continue;
                }

                validResources.Add(resource);
            }

            if (validResources.Count == 0)
            {
                foreach (var resource in planetResources)
                {
                    if (resource != null)
                    {
                        validResources.Add(resource);
                    }
                }
            }

            if (validResources.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum PlanetResources válido encontrado para atribuição.", this);
                return null;
            }

            return validResources[Random.Range(0, validResources.Count)];
        }

        private void OnPlanetCreated(PlanetCreatedEvent evt)
        {
            var detectable = evt?.Detected;
            if (detectable == null)
            {
                DebugUtility.LogVerbose<PlanetsManager>("PlanetCreatedEvent recebido sem IDetectable válido.", "yellow", this);
                return;
            }

            if (!_activePlanets.Contains(detectable))
            {
                _activePlanets.Add(detectable);
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta detectável {detectable.Owner.ActorName} adicionado à lista de ativos.", "cyan", this);
            }

            var planetMaster = detectable.Owner as PlanetsMaster;
            if (planetMaster == null)
            {
                DebugUtility.LogVerbose<PlanetsManager>("PlanetCreatedEvent recebido sem PlanetsMaster válido.", "yellow", this);
                return;
            }

            RegisterPlanet(planetMaster);
        }

        private void OnPlanetDestroyed(PlanetDestroyedEvent evt)
        {
            var detectable = evt?.Detected;
            if (detectable != null && _activePlanets.Remove(detectable))
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Detectável {detectable.Owner.ActorName} removido da lista de ativos.", "yellow", this);
            }

            var planetMaster = detectable?.Owner as PlanetsMaster;
            if (planetMaster == null)
            {
                return;
            }

            UnregisterPlanet(planetMaster);
        }

        private void OnPlanetResourceAssigned(PlanetResourceAssignedEvent evt)
        {
            if (evt.Planet == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Evento de recurso recebido sem planeta válido.", this);
                return;
            }

            _planetResourcesMap[evt.Planet] = evt.Resource;
            DebugUtility.LogVerbose<PlanetsManager>($"Mapa de recursos atualizado: {evt.Planet.name} -> {evt.Resource?.ResourceType}", "cyan", this);
        }

        private void OnPlanetResourceCleared(PlanetResourceClearedEvent evt)
        {
            if (evt.Planet == null)
            {
                return;
            }

            if (_planetResourcesMap.Remove(evt.Planet))
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Recurso removido do planeta {evt.Planet.name}.", "yellow", this);
            }
        }

        [ContextMenu("Log Spawned Planets Resources")]
        private void LogSpawnedPlanetsResources()
        {
            if (_spawnedPlanets.Count == 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>("Nenhum planeta instanciado para registrar.", "yellow", this);
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"[{nameof(PlanetsManager)}] Recursos atribuídos aos planetas instanciados:");

            foreach (var planet in _spawnedPlanets)
            {
                if (planet == null)
                {
                    continue;
                }

                _planetResourcesMap.TryGetValue(planet, out var resource);
                var resourceName = resource != null ? resource.ResourceType.ToString() : "Sem Recurso";
                builder.AppendLine($"- {planet.name}: {resourceName}");
            }

            DebugUtility.Log<PlanetsManager>(builder.ToString(), "green", this);
        }
    }
}
