using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityUtils;

namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public class PlanetsManager : Singleton<PlanetsManager>
    {
        [SerializeField] private Planets targetToEater;
        private readonly List<Planets> _activePlanets = new List<Planets>();
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
            if (planetGo == null || planetInfo == null)
            {
                Debug.LogError($"Erro: GameObject ({planetGo}) ou PlanetData ({planetInfo}) é nulo!");
                return;
            }

            planetGo.name = $"Planet_{planetInfo.name}_{index}";
            planetGo.transform.localPosition = Vector3.zero;
            planetGo.transform.localScale = Vector3.one * scaleMult;
            float tilt = Random.Range(planetInfo.minTiltAngle, planetInfo.maxTiltAngle);
            planetGo.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            Debug.Log($"Configurando planeta {planetGo.name}: escala {scaleMult}, inclinação {tilt} graus, diâmetro escalado {planetInfo.size * scaleMult}, ângulo inicial {initialAngleRad * Mathf.Rad2Deg} graus.");

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
            Debug.Log($"Movimento configurado para {planetGo.name}: raio {orbitRadius}, velocidade orbital {motion.OrbitSpeedDegPerSec}, rotação {motion.SelfRotationSpeedDegPerSec}.");

            var planets = planetGo.GetComponent<Planets>();
            if (planets != null)
            {
                planets.Initialize(index, planetInfo, resource);
                if (!_activePlanets.Contains(planets))
                {
                    _activePlanets.Add(planets);
                    Debug.Log($"Planeta {planetGo.name} adicionado à lista de ativos com recurso {resource?.name ?? "nenhum"}.");
                }
                var healthResource = planetGo.GetComponent<HealthResource>();
                if (healthResource != null && healthResource is IResettable resettable)
                {
                    resettable.Reset();
                    Debug.Log($"HealthResource resetado para {planetGo.name}.");
                }
            }
            else
            {
                Debug.LogError($"Componente Planets não encontrado em {planetGo.name}!");
            }
        }

        public List<PlanetResourcesSo> GenerateResourceList(int numPlanets, List<PlanetResourcesSo> availableResources)
        {
            if (availableResources == null || availableResources.Count == 0)
            {
                Debug.LogWarning("Nenhum recurso disponível para gerar lista!");
                return new List<PlanetResourcesSo>();
            }

            var resourceList = new List<PlanetResourcesSo>();
            for (int i = 0; i < numPlanets; i++)
            {
                resourceList.Add(availableResources[Random.Range(0, availableResources.Count)]);
            }
            Debug.Log($"Gerada lista de recursos com {resourceList.Count} itens para {numPlanets} planetas.");
            return resourceList.OrderBy(_ => Random.value).ToList();
        }

        public bool IsMarkedPlanet(Planets planet)
        {
            bool isMarked = targetToEater == planet;
            Debug.Log($"Verificando se {planet?.name ?? "nulo"} está marcado: {isMarked}.");
            return isMarked;
        }

        public void RemovePlanet(Planets planet)
        {
            if (planet == null)
            {
                Debug.LogWarning("Tentativa de remover planeta nulo!");
                return;
            }

            if (_activePlanets.Remove(planet))
            {
                if (targetToEater == planet)
                {
                    targetToEater = null;
                    Debug.Log($"Planeta {planet.name} era o alvo do Eater. Alvo limpo.");
                }
                Debug.Log($"Planeta {planet.name} removido. Planetas ativos: {_activePlanets.Count}.");
            }
            else
            {
                Debug.LogWarning($"Planeta {planet.name} não encontrado na lista de ativos!");
            }
        }

        private void MarkPlanet(PlanetMarkedEvent evt)
        {
            if (evt.Planet == null)
            {
                Debug.LogWarning("Evento PlanetMarkedEvent com planeta nulo!");
                return;
            }

            if (targetToEater == evt.Planet)
            {
                Debug.Log($"Planeta {evt.Planet.name} já está marcado.");
                return;
            }

            if (targetToEater != null)
            {
                EventBus<PlanetUnmarkedEvent>.Raise(new PlanetUnmarkedEvent(targetToEater));
            }

            targetToEater = evt.Planet;
            Debug.Log($"Planeta marcado: {evt.Planet.name}");
        }

        private void ClearMarkedPlanet(PlanetUnmarkedEvent evt)
        {
            if (evt.Planet == null)
            {
                Debug.LogWarning("Evento PlanetUnmarkedEvent com planeta nulo!");
                return;
            }

            if (targetToEater == evt.Planet)
            {
                targetToEater = null;
                Debug.Log($"Planeta desmarcado: {evt.Planet.name}");
            }
        }

        public List<Planets> GetActivePlanets() => _activePlanets;

        public Transform GetTargetTransform()
        {
            if (targetToEater != null)
            {
                return targetToEater.transform;
            }
            Debug.Log("Nenhum planeta marcado para o Eater.");
            return null;
        }
    }
}