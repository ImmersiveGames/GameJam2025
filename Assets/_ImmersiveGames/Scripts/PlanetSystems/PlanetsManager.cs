using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private List<PlanetData> planetOptions = new List<PlanetData>();
        [SerializeField] private List<PlanetResourcesSo> planetResources = new List<PlanetResourcesSo>();

        private IDetectable _targetToEater;
        private readonly List<IDetectable> _activePlanets = new();
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
       // private EventBinding<OrbitsSpawnedEvent> _orbitsSpawnedBinding;

        private void OnEnable()
        {
            /*_planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(MarkPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(ClearMarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);*/

            //_orbitsSpawnedBinding = new EventBinding<OrbitsSpawnedEvent>(OnOrbitsSpawned);
            //EventBus<OrbitsSpawnedEvent>.Register(_orbitsSpawnedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            //EventBus<OrbitsSpawnedEvent>.Unregister(_orbitsSpawnedBinding);
        }

        private void OnValidate()
        {
            if (planetOptions == null || planetOptions.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetsManager)}] PlanetOptions está vazio! Adicione pelo menos uma configuração de planeta.");
            }

            if (planetResources == null || planetResources.Count == 0)
            {
                Debug.LogWarning($"[{nameof(PlanetsManager)}] PlanetResources está vazio! Adicione pelo menos um recurso.");
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
                resourceList.Add(planetResources[Random.Range(0, planetResources.Count)]);
            }
            DebugUtility.Log<PlanetsManager>($"Gerada lista de recursos com {resourceList.Count} itens para {numPlanets} planetas.", "cyan", this);
            return resourceList.OrderBy(_ => Random.value).ToList();
        }

        public PlanetData GetRandomPlanetData()
        {
            if (planetOptions != null && planetOptions.Count != 0)
                return planetOptions[Random.Range(0, planetOptions.Count)];
            DebugUtility.LogError<PlanetsManager>("Lista de opções de planetas é nula ou vazia!", this);
            return null;
        }

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
    }
}