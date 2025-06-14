using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Warning)]
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        private IDetectable _targetToEater;
        private readonly List<IDetectable> _activePlanets = new();
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;

        private void OnEnable()
        {
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(MarkPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);

            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(ClearMarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
        }

        public PlanetsMaster ConfigurePlanet(IPoolable poolableObject, PlanetData planetInfo, int index, PlanetResourcesSo planetResource)
        {
            if (poolableObject == null || !planetInfo)
            {
                DebugUtility.LogError<PlanetsManager>($"Erro: GameObject ({nameof(poolableObject)}) ou PlanetData ({planetInfo}) é nulo!");
                return null;
            }
            var planetGo = poolableObject.GetGameObject();
            var planetMaster = planetGo.GetOrAdd<PlanetsMaster>();
            planetMaster.Initialize(index,poolableObject, planetInfo, planetResource);
            if (_activePlanets.Contains(planetMaster)) return planetMaster;
            _activePlanets.Add(planetMaster);
            DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetGo.name} adicionado à lista de ativos com recurso {planetResource?.name ?? "nenhum"}.");

            return planetMaster;
        }

        public List<PlanetResourcesSo> GenerateResourceList(int numPlanets, List<PlanetResourcesSo> availableResources)
        {
            if (availableResources == null || availableResources.Count == 0)
            {
                return new List<PlanetResourcesSo>();
            }

            var resourceList = new List<PlanetResourcesSo>();
            for (int i = 0; i < numPlanets; i++)
            {
                resourceList.Add(availableResources[Random.Range(0, availableResources.Count)]);
            }
            DebugUtility.Log<PlanetsManager>($"Gerada lista de recursos com {resourceList.Count} itens para {numPlanets} planetas.");
            return resourceList.OrderBy(_ => Random.value).ToList();
        }

        public bool IsMarkedPlanet(IDetectable planetMaster)
        {
            // Verifica se planetMaster é nulo
            if (planetMaster == null)return false;
           
            // Verifica se o planeta está na lista de planetas ativos
            if (!_activePlanets.Contains(planetMaster))return false;
            // Verifica se há um planeta marcado e se é o planetMaster
            DebugUtility.LogVerbose<PlanetsManager>($"Verificando se {planetMaster.Name ?? "nulo"} está marcado: {_targetToEater == planetMaster}.");
            return _targetToEater == planetMaster;
        }

        public void RemovePlanet(IDetectable planetMaster)
        {
            if (planetMaster == null)return;
           
            if (!_activePlanets.Remove(planetMaster))return;
            
            DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.Name} removido. Planetas ativos: {_activePlanets.Count}.");
        }

        private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (evt.Detected == null) return;

            if (_targetToEater == evt.Detected) return;

            // Desmarca o planeta anterior, se houver
            if (_targetToEater != null)
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(_targetToEater));
            }
            _targetToEater = evt.Detected;
            DebugUtility.Log<PlanetsManager>($"Planeta marcado: {evt.Detected.Name}");
        }

        private void ClearMarkedPlanet(PlanetUnmarkedEvent evt)
        {
            if (evt.Detected == null)return;
            _targetToEater = null;
            DebugUtility.Log<PlanetsManager>($"Planeta desmarcado: {evt.Detected.Name}");
        }

        public List<IDetectable> GetActivePlanets() => _activePlanets;

        public IDetectable GetPlanetMarked() => _targetToEater;
    }
}