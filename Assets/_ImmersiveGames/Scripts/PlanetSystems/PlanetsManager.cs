using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Verbose)]
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
            if (!_activePlanets.Contains(planetMaster))
            {
                _activePlanets.Add(planetMaster);
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetGo.name} adicionado à lista de ativos com recurso {planetResource?.name ?? "nenhum"}.");
            }
            
            return planetMaster;
        }

        public void ConfigurePlanet(GameObject planetGo, PlanetData planetInfo, int index, PlanetResourcesSo resource, float orbitRadius, int scaleMult, float initialAngleRad)
        {
            if (!planetGo || !planetInfo)
            {
                DebugUtility.LogError<PlanetsManager>($"Erro: GameObject ({planetGo}) ou PlanetData ({planetInfo}) é nulo!");
                return;
            }

            planetGo.name = $"Planet_{planetInfo.name}_{index}";
            planetGo.transform.localPosition = Vector3.zero;
            planetGo.transform.localScale = Vector3.one * scaleMult;
            float tilt = Random.Range(planetInfo.minTiltAngle, planetInfo.maxTiltAngle);
            planetGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            DebugUtility.LogVerbose<PlanetsManager>($"Configurando planeta {planetGo.name}: escala {scaleMult}, inclinação {tilt} graus, diâmetro escalado {planetInfo.size * scaleMult}, ângulo inicial {initialAngleRad * Mathf.Rad2Deg} graus.");

            var motion = planetGo.GetComponent<PlanetMotion>() ?? planetGo.AddComponent<PlanetMotion>();
            bool randomOrbit = Random.value > 0.5f;
            bool randomRotate = Random.value > 0.5f;
            motion.Initialize(
                center: planetInfo.orbitCenter ?? Vector3.zero,
                radius: orbitRadius,
                orbitSpeedDegPerSec: Random.Range(planetInfo.minOrbitSpeed, planetInfo.maxOrbitSpeed),
                orbitClockwise: randomOrbit,
                selfRotationSpeedDegPerSec: Random.Range(planetInfo.minRotationSpeed, planetInfo.maxRotationSpeed) * (randomRotate ? -1f : 1f),
                initialAngleRad: initialAngleRad
            );
            DebugUtility.LogVerbose<PlanetsManager>($"Movimento configurado para {planetGo.name}: raio {orbitRadius}, velocidade orbital {motion.OrbitSpeedDegPerSec}, rotação {motion.SelfRotationSpeedDegPerSec}.");

            var planets = planetGo.GetComponent<PlanetsMaster>();
            if (planets)
            {
                planets.Initialize(index, null, planetInfo, resource);
                if (!_activePlanets.Contains(planets))
                {
                    _activePlanets.Add(planets);
                    DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetGo.name} adicionado à lista de ativos com recurso {resource?.name ?? "nenhum"}.");
                }
                var healthResource = planetGo.GetComponent<HealthResource>();
                IResettable resettable = healthResource;
                if (!healthResource || resettable == null) return;
                resettable.Reset();
                DebugUtility.LogVerbose<PlanetsManager>($"HealthResource resetado para {planetGo.name}.");
            }
            else
            {
                DebugUtility.LogError<PlanetsManager>($"Componente PlanetsMaster não encontrado em {planetGo.name}!");
            }
        }

        public List<PlanetResourcesSo> GenerateResourceList(int numPlanets, List<PlanetResourcesSo> availableResources)
        {
            if (availableResources == null || availableResources.Count == 0)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum recurso disponível para gerar lista!");
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
            if (planetMaster == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Tentativa de verificar planeta nulo!");
                return false;
            }

            // Verifica se o planeta está na lista de planetas ativos
            if (!_activePlanets.Contains(planetMaster))
            {
                DebugUtility.LogWarning<PlanetsManager>($"Planeta {planetMaster.Name} não está na lista de planetas ativos!");
                return false;
            }
            // Verifica se há um planeta marcado e se é o planetMaster
            DebugUtility.LogVerbose<PlanetsManager>($"Verificando se {planetMaster.Name ?? "nulo"} está marcado: {_targetToEater == planetMaster}.");
            return _targetToEater == planetMaster;
        }

        public void RemovePlanet(IDetectable planetMaster)
        {
            if (planetMaster == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Tentativa de remover planeta nulo!");
                return;
            }
            if (!_activePlanets.Remove(planetMaster))
            {
                DebugUtility.LogWarning<PlanetsManager>($"Planeta {planetMaster.Name} não encontrado na lista de ativos!");
                return;
            }
            DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.Name} removido. Planetas ativos: {_activePlanets.Count}.");
        }

        private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (evt.Detected == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Evento PlanetMarkedEvent com planeta nulo!");
                return;
            }

            if (_targetToEater == evt.Detected)
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {evt.Detected.Name} já está marcado.");
                return;
            }

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
            if (evt.Detected == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Evento PlanetUnmarkedEvent com planeta nulo!");
                return;
            }
            _targetToEater = null;
            DebugUtility.Log<PlanetsManager>($"Planeta desmarcado: {evt.Detected.Name}");
        }

        public List<IDetectable> GetActivePlanets() => _activePlanets;

        public IDetectable GetPlanetMarked() => _targetToEater;
    }
}