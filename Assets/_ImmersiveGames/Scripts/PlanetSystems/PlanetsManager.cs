using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityUtils;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Core;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.PlanetSystems.Managers;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;

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

        // Detectáveis de planetas atualmente ativos (para sistemas de detecção).
        private readonly List<IDetectable> _activePlanetDetectables = new();

        // Mapa de ator de planeta -> tipo de recurso atribuído.
        private readonly Dictionary<IPlanetActor, PlanetResources> _planetResourcesMap = new();

        // Lista de instâncias concretas de planetas gerenciados.
        private readonly List<PlanetsMaster> _spawnedPlanetMasters = new();

        // Raio de órbita calculado para cada planeta (mesmo índice de _spawnedPlanetMasters).
        private readonly List<float> _calculatedOrbitRadii = new();

        // Cache de planetas por ActorId para resolução rápida.
        private readonly Dictionary<string, PlanetsMaster> _planetsByActorId = new();

        // Binding para remoção de planetas quando recebem evento de morte.
        private EventBinding<DeathEvent> _planetDeathBinding;

        // Mapa de tipo de recurso -> definição ScriptableObject.
        private readonly Dictionary<PlanetResources, PlanetResourcesSo> _resourceDefinitions = new();

        // Serviço responsável por organizar órbitas e configurar PlanetMotion.
        private PlanetOrbitArranger _orbitArranger;

        protected override void Awake()
        {
            base.Awake();

            if (planetsRoot == null)
            {
                planetsRoot = transform;
            }

            // Inicializa o arranjador de órbitas com a configuração atual.
            _orbitArranger = new PlanetOrbitArranger(
                planetsRoot != null ? planetsRoot : transform,
                initialOrbitRadius,
                minimumOrbitSpacing,
                randomizeInitialAngle,
                orbitSpeedRange,
                selfRotationSpeedRange,
                randomizeOrbitDirection,
                defaultOrbitClockwise);

            RebuildResourceDefinitions();

            _planetDeathBinding = new EventBinding<DeathEvent>(OnPlanetDeath);
            EventBus<DeathEvent>.Register(_planetDeathBinding);
        }

        private void OnValidate()
        {
            RebuildResourceDefinitions();
        }

        private void Start()
        {
            StartCoroutine(InitializePlanetsRoutine());
        }

        private IEnumerator InitializePlanetsRoutine()
        {
            if (planetPrefab == null)
            {
                DebugUtility.LogError<PlanetsManager>("Prefab de planeta não configurado no PlanetsManager.");
                yield break;
            }

            if (_planetResourcesMap.Count > 0)
            {
                DebugUtility.LogVerbose<PlanetsManager>(
                    "Planetas já foram inicializados anteriormente, evitando duplicação.");
                yield break;
            }

            // 1) Cria as instâncias de planeta.
            for (int i = 0; i < initialPlanetCount; i++)
            {
                var planetInstance = Instantiate(planetPrefab, planetsRoot);
                planetInstance.name = $"{planetPrefab.name}_{i + 1}";

                RegisterPlanet(planetInstance);
            }

            // 2) Aguarda um frame para garantir que modificadores de escala/skin tenham aplicado transformações.
            yield return null;

            // 3) Calcula órbitas e posiciona.
            ArrangePlanetsInOrbits();

            // 4) Notifica que a inicialização foi concluída.
            EventBus<PlanetsInitializationCompletedEvent>.Raise(
                new PlanetsInitializationCompletedEvent(
                    new ReadOnlyDictionary<IPlanetActor, PlanetResources>(_planetResourcesMap)));
        }

        /// <summary>
        /// Registra um novo planeta instanciado no sistema:
        /// - Armazena na lista local
        /// - Sorteia e atribui um recurso
        /// - Atualiza mapas de consulta
        /// - Registra detectáveis para uso em sistemas de detecção
        /// </summary>
        private void RegisterPlanet(PlanetsMaster planetInstance)
        {
            if (planetInstance == null)
            {
                return;
            }

            _spawnedPlanetMasters.Add(planetInstance);

            var resource = DrawPlanetResource();
            if (resource == null)
            {
                DebugUtility.LogWarning<PlanetsManager>(
                    "Nenhum recurso disponível para atribuir ao planeta instanciado.");
            }

            planetInstance.AssignResource(resource);
            CacheResourceDefinition(resource);

            var resourceType = resource != null ? resource.ResourceType : default;
            _planetResourcesMap[planetInstance] = resourceType;
            _planetsByActorId[planetInstance.ActorId] = planetInstance;

            RegisterActiveDetectables(planetInstance);
        }

        /// <summary>
        /// Calcula as órbitas e posiciona cada planeta em torno do centro,
        /// delegando para o serviço PlanetOrbitArranger.
        /// </summary>
        private void ArrangePlanetsInOrbits()
        {
            _calculatedOrbitRadii.Clear();

            if (_spawnedPlanetMasters.Count == 0 || _orbitArranger == null)
            {
                return;
            }

            var radii = _orbitArranger.ArrangePlanetsInOrbits(_spawnedPlanetMasters);
            _calculatedOrbitRadii.AddRange(radii);
        }

        private PlanetResourcesSo DrawPlanetResource()
        {
            if (availableResources == null || availableResources.Count == 0)
            {
                return null;
            }

            int randomIndex = Random.Range(0, availableResources.Count);
            var resource = availableResources[randomIndex];
            CacheResourceDefinition(resource);
            return resource;
        }

        private void RebuildResourceDefinitions()
        {
            _resourceDefinitions.Clear();
            if (availableResources == null)
            {
                return;
            }

            foreach (var definition in availableResources)
            {
                CacheResourceDefinition(definition);
            }
        }

        private void CacheResourceDefinition(PlanetResourcesSo resource)
        {
            if (resource == null)
            {
                return;
            }

            var resourceType = resource.ResourceType;
            if (!_resourceDefinitions.TryAdd(resourceType, resource))
            {
                // Já existe uma definição para este tipo de recurso.
                // Não é um erro, mas pode indicar configuração redundante.
            }
        }

        public IReadOnlyDictionary<IPlanetActor, PlanetResources> GetPlanetResourcesMap() => _planetResourcesMap;
        public IReadOnlyCollection<IPlanetActor> GetPlanetActors() => _planetResourcesMap.Keys;
        public List<IDetectable> GetActivePlanets() => _activePlanetDetectables;

        /// <summary>
        /// Retorna o detectável associado ao planeta atualmente marcado,
        /// com base no PlanetMarkingManager. Caso não haja planeta
        /// marcado, retorna null.
        /// </summary>
        public IDetectable GetPlanetMarked()
        {
            var markingManager = PlanetMarkingManager.Instance;
            if (!markingManager.HasMarkedPlanet)
            {
                return null;
            }

            var markPlanet = markingManager.CurrentlyMarkedPlanet;
            if (markPlanet == null)
            {
                return null;
            }

            // Procura um IDetectable na hierarquia do planeta marcado.
            var detectable = markPlanet.GetComponentInChildren<IDetectable>(true);
            return detectable;
        }

        /// <summary>
        /// Resolve o <see cref="PlanetsMaster"/> associado a um ator de planeta ativo.
        /// </summary>
        public bool TryGetPlanet(IActor planetActor, out PlanetsMaster planet)
        {
            planet = null;

            if (planetActor == null)
            {
                return false;
            }

            string actorId = planetActor.ActorId;
            if (string.IsNullOrEmpty(actorId))
            {
                return false;
            }

            if (_planetsByActorId.TryGetValue(actorId, out var cached) && cached != null)
            {
                planet = cached;
                return true;
            }

            if (planetActor is PlanetsMaster master && master != null)
            {
                planet = master;
                _planetsByActorId[actorId] = master;
                return true;
            }

            foreach (var candidate in _spawnedPlanetMasters.Where(candidate => candidate != null)
                         .Where(candidate => candidate.ActorId == actorId))
            {
                planet = candidate;
                _planetsByActorId[actorId] = candidate;
                return true;
            }

            return false;
        }

        public bool TryGetDetectable(IActor planetActor, out IDetectable detectable)
        {
            detectable = null;

            if (planetActor == null)
            {
                return false;
            }

            string actorId = planetActor.ActorId;
            foreach (var candidate in _activePlanetDetectables
                         .Where(candidate => candidate?.Owner != null)
                         .Where(candidate => candidate.Owner.ActorId == actorId))
            {
                detectable = candidate;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Verifica se o detectável informado é o planeta atualmente marcado.
        /// </summary>
        public bool IsMarkedPlanet(IDetectable planet)
        {
            if (planet == null)
            {
                return false;
            }

            var marked = GetPlanetMarked();
            return marked == planet;
        }

        public bool TryGetResourceDefinition(PlanetResources resourceType, out PlanetResourcesSo definition)
        {
            if (_resourceDefinitions.TryGetValue(resourceType, out definition) && definition != null)
            {
                return true;
            }

            definition = null;
            return false;
        }

        private void OnPlanetDeath(DeathEvent evt)
        {
            if (evt.entityId == null)
            {
                return;
            }

            if (!_planetsByActorId.TryGetValue(evt.entityId, out var planet) || planet == null)
            {
                _planetsByActorId.Remove(evt.entityId);
                return;
            }

            RemovePlanet(planet);
        }

        private void RemovePlanet(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            ForceUnmarkPlanet(planet);
            _planetsByActorId.Remove(planet.ActorId);
            _planetResourcesMap.Remove(planet);
            _spawnedPlanetMasters.Remove(planet);
            UnregisterActiveDetectables(planet);

            DebugUtility.LogVerbose<PlanetsManager>(
                $"Planeta removido do gerenciamento: {planet.ActorName}.");
        }

        private static void ForceUnmarkPlanet(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            var markPlanet = planet.GetComponentInChildren<MarkPlanet>(true);
            if (markPlanet == null)
            {
                return;
            }

            markPlanet.Unmark();
        }

        private void RegisterActiveDetectables(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            IDetectable[] detectables = planet.GetComponentsInChildren<IDetectable>(true);
            foreach (var detectable in detectables)
            {
                RegisterActiveDetectable(detectable);
            }
        }

        private void UnregisterActiveDetectables(PlanetsMaster planet)
        {
            if (planet == null)
            {
                return;
            }

            IDetectable[] detectables = planet.GetComponentsInChildren<IDetectable>(true);
            foreach (var detectable in detectables)
            {
                UnregisterActiveDetectable(detectable);
            }
        }

        private void RegisterActiveDetectable(IDetectable detectable)
        {
            if (detectable == null || _activePlanetDetectables.Contains(detectable))
            {
                return;
            }

            _activePlanetDetectables.Add(detectable);
        }

        private void UnregisterActiveDetectable(IDetectable detectable)
        {
            if (detectable == null)
            {
                return;
            }

            _activePlanetDetectables.Remove(detectable);
        }

        private void OnDestroy()
        {
            if (_planetDeathBinding == null)
            {
                return;
            }

            EventBus<DeathEvent>.Unregister(_planetDeathBinding);
            _planetDeathBinding = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawOrbitGizmos || _calculatedOrbitRadii == null || _calculatedOrbitRadii.Count == 0)
            {
                return;
            }

            var centerPosition = planetsRoot != null ? planetsRoot.position : transform.position;
            Gizmos.color = orbitGizmosColor;

            foreach (float radius in _calculatedOrbitRadii)
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

            var previousPoint = center + new Vector3(radius, 0f, 0f);
            float step = Mathf.PI * 2f / segments;

            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                var nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previousPoint, nextPoint);
                previousPoint = nextPoint;
            }
        }
#endif
    }
}

