using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DefaultExecutionOrder(-80), DebugLevel(DebugLevel.Logs)]
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        [SerializeField] private PlanetsMaster targetToEater;
        private readonly List<PlanetsMaster> _activePlanets = new();
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
                planets.Initialize(index, planetInfo, resource);
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

        public bool IsMarkedPlanet(PlanetsMaster planetMaster)
        {
            if(!planetMaster)
            {
                DebugUtility.LogWarning<PlanetsManager>("Tentativa de verificar planeta nulo!");
                return false;
            }
            bool isMarked = targetToEater == planetMaster;
            DebugUtility.LogVerbose<PlanetsManager>($"Verificando se {planetMaster?.name ?? "nulo"} está marcado: {isMarked}.");
            return isMarked;
        }

        public void RemovePlanet(PlanetsMaster planetMaster)
        {
            if (!planetMaster)
            {
                DebugUtility.LogWarning<PlanetsManager>("Tentativa de remover planeta nulo!");
                return;
            }

            if (_activePlanets.Remove(planetMaster))
            {
                if (targetToEater == planetMaster)
                {
                    targetToEater = null;
                    DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.name} era o alvo do Eater. Alvo limpo.");
                }
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {planetMaster.name} removido. Planetas ativos: {_activePlanets.Count}.");
            }
            else
            {
                DebugUtility.LogWarning<PlanetsManager>($"Planeta {planetMaster.name} não encontrado na lista de ativos!");
            }
        }

        private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (!evt.PlanetMaster)
            {
                DebugUtility.LogWarning<PlanetsManager>("Evento PlanetMarkedEvent com planeta nulo!");
                return;
            }

            if (targetToEater == evt.PlanetMaster)
            {
                DebugUtility.LogVerbose<PlanetsManager>($"Planeta {evt.PlanetMaster.name} já está marcado.");
                return;
            }

            if (targetToEater)
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(targetToEater));
            }

            targetToEater = evt.PlanetMaster;
            DebugUtility.Log<PlanetsManager>($"Planeta marcado: {evt.PlanetMaster.name}");
        }

        private void ClearMarkedPlanet(PlanetUnmarkedEvent evt)
        {
            if (!evt.PlanetMaster)
            {
                DebugUtility.LogWarning<PlanetsManager>("Evento PlanetUnmarkedEvent com planeta nulo!");
                return;
            }

            if (targetToEater != evt.PlanetMaster) return;
            targetToEater = null;
            DebugUtility.Log<PlanetsManager>($"Planeta desmarcado: {evt.PlanetMaster.name}");
        }

        public List<PlanetsMaster> GetActivePlanets() => _activePlanets;

        public PlanetsMaster GetPlanetMarked() => targetToEater;
    }
}