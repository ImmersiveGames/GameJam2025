using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils;

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

        [Header("Orbit Setup")]
        [Tooltip("Raio mínimo da órbita mais interna.")]
        [SerializeField, Min(0f)] private float initialOrbitRadius = 5f;

        [Tooltip("Distância mínima entre a superfície de um planeta e o próximo na órbita subsequente.")]
        [SerializeField, Min(0f)] private float minimumOrbitSpacing = 2f;

        [Tooltip("Sorteia um ângulo inicial aleatório para cada planeta ao posicioná-lo.")]
        [SerializeField] private bool randomizeInitialAngle = true;

        [Tooltip("Intervalo (em graus por segundo) para sorteio da velocidade orbital.")]
        [SerializeField] private Vector2 orbitSpeedRange = new(5f, 15f);

        [Tooltip("Intervalo (em graus por segundo) para sorteio da rotação própria do planeta.")]
        [SerializeField] private Vector2 selfRotationSpeedRange = new(-20f, 20f);

        [Tooltip("Quando verdadeiro, alterna de forma aleatória entre órbita horário e anti-horário.")]
        [SerializeField] private bool randomizeOrbitDirection = true;

        [Tooltip("Direção padrão utilizada quando não houver sorteio de sentido orbital.")]
        [SerializeField] private bool defaultOrbitClockwise;

        [Header("Debug")]
        [Tooltip("Desenha gizmos representando as órbitas calculadas.")]
        [SerializeField] private bool drawOrbitGizmos = true;

        [SerializeField] private Color orbitGizmosColor = new(0.1f, 0.65f, 1f, 0.75f);

        [Header("Resources Setup")]
        [Tooltip("Lista de recursos disponíveis para sortear ao instanciar planetas.")]
        [SerializeField] private List<PlanetResourcesSo> availableResources = new();

        private IDetectable _targetToEater;
        private readonly List<IDetectable> _activePlanets = new();
        private readonly Dictionary<IPlanetActor, PlanetResources> _planetResourcesMap = new();
        private readonly List<PlanetsMaster> _spawnedPlanets = new();
        private readonly List<float> _orbitRadii = new();

        protected override void Awake()
        {
            base.Awake();
            if (planetsRoot == null)
            {
                planetsRoot = transform;
            }
        }

        private void Start() => StartCoroutine(InitializePlanetsRoutine());

        private IEnumerator InitializePlanetsRoutine()
        {
            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta não configurado no PlanetsManager.");
                yield break;
            }

            if (_planetResourcesMap.Count > 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>("Planetas já foram inicializados anteriormente, evitando duplicação.");
                yield break;
            }

            for (int i = 0; i < initialPlanetCount; i++)
            {
                PlanetsMaster planetInstance = Instantiate(planetPrefab, planetsRoot);
                planetInstance.name = $"{planetPrefab.name}_{i + 1}";

                RegisterPlanet(planetInstance);
            }

            // Aguarda um frame para garantir que modificadores de escala/skin tenham aplicado suas transformações.
            yield return null;

            ArrangePlanetsInOrbits();

            EventBus<PlanetsInitializationCompletedEvent>.Raise(
                new PlanetsInitializationCompletedEvent(new ReadOnlyDictionary<IPlanetActor, PlanetResources>(_planetResourcesMap)));
        }

        private void RegisterPlanet(PlanetsMaster planetInstance)
        {
            if (planetInstance == null)
            {
                return;
            }

            _spawnedPlanets.Add(planetInstance);

            PlanetResourcesSo resource = DrawPlanetResource();
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetsManager>("Nenhum recurso disponível para atribuir ao planeta instanciado.");
            }

            planetInstance.AssignResource(resource);

            PlanetResources resourceType = resource != null ? resource.ResourceType : default;
            _planetResourcesMap[planetInstance] = resourceType;
        }

        private void ArrangePlanetsInOrbits()
        {
            _orbitRadii.Clear();

            if (_spawnedPlanets.Count == 0)
            {
                return;
            }

            Vector3 centerPosition = planetsRoot != null ? planetsRoot.position : transform.position;
            float previousOrbitRadius = 0f;
            float previousPlanetRadius = 0f;

            for (int i = 0; i < _spawnedPlanets.Count; i++)
            {
                PlanetsMaster planet = _spawnedPlanets[i];
                if (planet == null)
                {
                    continue;
                }

                float planetRadius = CalculatePlanetRadius(planet.gameObject);

                float orbitRadius = i == 0
                    ? Mathf.Max(initialOrbitRadius, planetRadius)
                    : Mathf.Max(initialOrbitRadius,
                        previousOrbitRadius + minimumOrbitSpacing + previousPlanetRadius + planetRadius);

                float angle = randomizeInitialAngle
                    ? Random.Range(0f, Mathf.PI * 2f)
                    : Mathf.PI * 2f * (i / (float)_spawnedPlanets.Count);

                Vector3 offset = new(Mathf.Cos(angle) * orbitRadius, 0f, Mathf.Sin(angle) * orbitRadius);
                Vector3 targetPosition = centerPosition + offset;
                planet.transform.position = targetPosition;

                ConfigurePlanetMotion(planet, orbitRadius, angle);

                _orbitRadii.Add(orbitRadius);

                previousOrbitRadius = orbitRadius;
                previousPlanetRadius = planetRadius;
            }
        }

        private void ConfigurePlanetMotion(PlanetsMaster planet, float orbitRadius, float startAngle)
        {
            if (!planet.TryGetComponent(out PlanetMotion motion))
            {
                return;
            }

            float orbitSpeed = GetRandomSpeedFromRange(orbitSpeedRange);
            float selfRotationSpeed = GetRandomSpeedFromRange(selfRotationSpeedRange);
            bool orbitClockwise = randomizeOrbitDirection ? Random.value > 0.5f : defaultOrbitClockwise;

            motion.ConfigureOrbit(planetsRoot != null ? planetsRoot : transform, orbitRadius, startAngle, orbitSpeed,
                selfRotationSpeed, orbitClockwise);
        }

        private static float CalculatePlanetRadius(GameObject planetObject)
        {
            if (planetObject == null)
            {
                return 0f;
            }

            Bounds bounds = CalculateRealLength.GetBounds(planetObject);
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

            if (radius > 0f)
            {
                return radius;
            }

            // Fallback para casos onde ainda não há renderizadores válidos (ex.: placeholders).
            Vector3 scale = planetObject.transform.lossyScale;
            radius = Mathf.Max(scale.x, scale.z) * 0.5f;
            return radius > 0f ? radius : 0.5f;
        }

        private static float GetRandomSpeedFromRange(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
        }

        private PlanetResourcesSo DrawPlanetResource()
        {
            if (availableResources == null || availableResources.Count == 0)
            {
                return null;
            }

            int randomIndex = Random.Range(0, availableResources.Count);
            return availableResources[randomIndex];
        }

        public IReadOnlyDictionary<IPlanetActor, PlanetResources> GetPlanetResourcesMap() => _planetResourcesMap;
        public IReadOnlyCollection<IPlanetActor> GetPlanetActors() => _planetResourcesMap.Keys;
        public List<IDetectable> GetActivePlanets() => _activePlanets;
        public IDetectable GetPlanetMarked() => _targetToEater;

        public bool IsMarkedPlanet(IDetectable planet) => _targetToEater == planet;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawOrbitGizmos || _orbitRadii == null || _orbitRadii.Count == 0)
            {
                return;
            }

            Vector3 centerPosition = planetsRoot != null ? planetsRoot.position : transform.position;
            Gizmos.color = orbitGizmosColor;

            foreach (float radius in _orbitRadii)
            {
                DrawOrbitGizmo(centerPosition, radius);
            }
        }

        private static void DrawOrbitGizmo(Vector3 center, float radius, int segments = 64)
        {
            if (radius <= 0f)
            {
                return;
            }

            Vector3 previousPoint = center + new Vector3(radius, 0f, 0f);
            float step = Mathf.PI * 2f / segments;

            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
#endif
    }
}
