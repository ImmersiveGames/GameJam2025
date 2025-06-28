using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Warning)]
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        [SerializeField] private List<PlanetData> planetOptions = new List<PlanetData>();
        [SerializeField] private List<PlanetResourcesSo> planetResources = new List<PlanetResourcesSo>();

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

        public PlanetsMaster ConfigurePlanet(IPoolable poolableObject, PlanetData planetInfo, int index, PlanetResourcesSo planetResource)
        {
            if (poolableObject == null || !planetInfo)
            {
                DebugUtility.LogError<PlanetsManager>($"Erro: Actor ({nameof(poolableObject)}) ou PlanetData ({planetInfo}) é nulo!");
                return null;
            }
            var planetGo = poolableObject.GetGameObject();
            var planetMaster = planetGo.GetOrAdd<PlanetsMaster>();
            planetMaster.Initialize(index, poolableObject, planetInfo, planetResource);
            if (_activePlanets.Contains(planetMaster)) return planetMaster;
            _activePlanets.Add(planetMaster);
            DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetGo.name} adicionado à lista de ativos com recurso {planetResource?.name ?? "nenhum"}.");
            return planetMaster;
        }

        public List<PlanetResourcesSo> GenerateResourceList(int numPlanets)
        {
            if (planetResources == null || planetResources.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum recurso disponível!");
                return new List<PlanetResourcesSo>();
            }

            var resourceList = new List<PlanetResourcesSo>();
            for (int i = 0; i < numPlanets; i++)
            {
                resourceList.Add(planetResources[Random.Range(0, planetResources.Count)]);
            }
            DebugUtility.Log<PlanetsManager>($"Gerada lista de recursos com {resourceList.Count} itens para {numPlanets} planetas.");
            return resourceList.OrderBy(_ => Random.value).ToList();
        }

        public void CreatePlanetsFromOrbitPositions(List<OrbitPlanetInfo> orbitInfos, ObjectPool pool)
        {
            if (orbitInfos == null || orbitInfos.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Lista de OrbitPlanetInfo vazia!");
                return;
            }

            for (int index = 0; index < orbitInfos.Count; index++)
            {
                var orbitInfo = orbitInfos[index];
                var poolable = pool.GetObject(orbitInfo.orbitPosition);
                if (poolable == null)
                {
                    DebugUtility.LogWarning<PlanetsManager>($"Falha ao obter objeto do pool para planeta {index}!");
                    continue;
                }

                DebugUtility.Log<PlanetsManager>($"Objeto obtido do pool para planeta {index} na posição {orbitInfo.orbitPosition}.", "cyan");

                var planetData = GetRandomPlanetData();
                if (planetData == null)
                {
                    DebugUtility.LogWarning<PlanetsManager>("Nenhum PlanetData válido encontrado!");
                    poolable.Deactivate();
                    continue;
                }

                var resourceList = GenerateResourceList(1);
                if (resourceList.Count == 0)
                {
                    DebugUtility.LogWarning<PlanetsManager>("Nenhum recurso disponível!");
                    poolable.Deactivate();
                    continue;
                }

                var planetMaster = ConfigurePlanet(poolable, planetData, index, resourceList[0]);
                if (planetMaster == null)
                {
                    DebugUtility.LogWarning<PlanetsManager>($"Falha ao configurar planeta {index}!");
                    poolable.Deactivate();
                    continue;
                }

                var planetInfo = planetMaster.GetPlanetInfo();
                planetInfo.orbitPosition = orbitInfo.orbitPosition;
                planetInfo.planetRadius = orbitInfo.planetRadius;
                planetInfo.initialAngle = orbitInfo.initialAngle;
                planetInfo.orbitSpeed = orbitInfo.orbitSpeed;

                poolable.Activate(orbitInfo.orbitPosition);
                planetMaster.transform.position = orbitInfo.orbitPosition;
                DebugUtility.Log<PlanetsManager>($"Planeta {index} ativado na posição {orbitInfo.orbitPosition}.", "green");

                EventBus<PlanetCreatedEvent>.Raise(new PlanetCreatedEvent(planetMaster));
            }
        }

        public PlanetData GetRandomPlanetData()
        {
            if (planetOptions != null && planetOptions.Count != 0)
                return planetOptions[Random.Range(0, planetOptions.Count)];
            DebugUtility.LogError<PlanetsManager>("Lista de opções de planetas é nula ou vazia!");
            return null;
        }

        public bool IsMarkedPlanet(IDetectable planetMaster)
        {
            if (planetMaster == null) return false;
            if (!_activePlanets.Contains(planetMaster)) return false;
            DebugUtility.LogVerbose<PlanetsManager>($"Verificando se {planetMaster.Detectable.Name ?? "nulo"} está marcado: {_targetToEater == planetMaster}.");
            return _targetToEater == planetMaster;
        }

        public void RemovePlanet(IDetectable planetMaster)
        {
            if (planetMaster == null) return;
            if (!_activePlanets.Remove(planetMaster)) return;
            DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.Detectable.Name} removido. Planetas ativos: {_activePlanets.Count}.");
        }

        private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (evt.Detected == null) return;
            if (_targetToEater == evt.Detected) return;
            if (_targetToEater != null)
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(_targetToEater));
            }
            _targetToEater = evt.Detected;
            DebugUtility.Log<PlanetsManager>($"Planeta marcado: {evt.Detected.Detectable.Name}");
        }

        private void ClearMarkedPlanet(PlanetUnmarkedEvent evt)
        {
            if (evt.Detected == null) return;
            _targetToEater = null;
            DebugUtility.Log<PlanetsManager>($"Planeta desmarcado: {evt.Detected.Detectable.Name}");
        }

        public List<IDetectable> GetActivePlanets() => _activePlanets;

        public IDetectable GetPlanetMarked() => _targetToEater;
    }
}