using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.EaterSystem.Predicates;
using UnityEngine;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.GameManagerSystems.EventsBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterHealth))]
    [RequireComponent(typeof(EaterDetectable))]
    [RequireComponent(typeof(EaterHunger))]
    [RequireComponent(typeof(EaterDesire))]
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterAIController : MonoBehaviour
    {
        [Header("Wander Settings")]
        [SerializeField] private float minWanderSpeed = 1f;
        [SerializeField] private float maxWanderSpeed = 3f;
        [SerializeField] private float directionChangeInterval = 2f;

        [Header("Chase Settings")]
        [SerializeField] private float baseChaseSpeed = 6f;
        [SerializeField] private float reachDistance = 1.5f;

        [Header("Eating Settings")]
        [SerializeField] private float eatingDuration = 3f;

        private StateMachine.StateMachine _stateMachine;
        private EaterHealth _health;
        private EaterDetectable _detector;
        private EaterHunger _hunger;
        private EaterDesire _desire; // Nova referência
        private Transform _currentTarget;
        private bool _isEating;
        private bool _targetReached;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        private EventBinding<EaterStarvedEvent> _starvedBinding;
        private EventBinding<DeathEvent> _deathBinding;

        private void Awake()
        {
            _health = GetComponent<EaterHealth>();
            _detector = GetComponent<EaterDetectable>();
            _hunger = GetComponent<EaterHunger>();
            _desire = GetComponent<EaterDesire>(); // Obtém EaterDesire
            _detector.OnTargetUpdated += HandleTargetUpdated;
            _detector.OnEatPlanet += HandleEatPlanet;
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(HandlePlanetUnmarked);
            _starvedBinding = new EventBinding<EaterStarvedEvent>(HandleStarved);
            _deathBinding = new EventBinding<DeathEvent>(HandleDeath);
        }

        private void OnEnable()
        {
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
            EventBus<EaterStarvedEvent>.Register(_starvedBinding);
            EventBus<DeathEvent>.Register(_deathBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedBinding);
            EventBus<EaterStarvedEvent>.Unregister(_starvedBinding);
            EventBus<DeathEvent>.Unregister(_deathBinding);
        }

        private void OnDestroy()
        {
            _detector.OnTargetUpdated -= HandleTargetUpdated;
            _detector.OnEatPlanet -= HandleEatPlanet;
        }

        private void Start()
        {
            var wanderState = new WanderState(transform, minWanderSpeed, maxWanderSpeed, directionChangeInterval);
            var chaseState = new ChaseState(
                transform,
                () => _currentTarget,
                GetChaseSpeed,
                reachDistance
            );
            var eatingState = new EatingState(
                eatingDuration,
                _hunger,
                _detector,
                OnFinishEating
            );

            chaseState.OnTargetReached += () =>
            {
                _targetReached = true;
                _isEating = true;
            };

            _stateMachine = new StateMachineBuilder()
                .AddState(wanderState, out var wanderRef)
                .AddState(chaseState, out var chaseRef)
                .AddState(eatingState, out var eatingRef)
                .At(wanderRef, chaseRef, new BoolPredicate(() => _currentTarget))
                .At(chaseRef, eatingRef, new BoolPredicate(() => _targetReached))
                .At(eatingRef, wanderRef, new BoolPredicate(() => !_isEating))
                .At(chaseRef, wanderRef, new BoolPredicate(() => !_currentTarget))
                .StateInitial(wanderRef)
                .Build();
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            _stateMachine?.FixedUpdate();
        }

        private void HandleTargetUpdated(Transform target)
        {
            _currentTarget = target;
            _targetReached = false;
            _isEating = false;
            DebugUtility.Log<EaterAIController>($"Novo alvo definido: {target?.name ?? "nenhum"}.");
        }

        private void HandleEatPlanet(Planets planet)
        {
            if (_currentTarget == planet.transform)
            {
                _targetReached = true;
                _isEating = true;
                DebugUtility.Log<EaterAIController>($"Iniciando consumo do planeta: {planet.name}.");
                _desire.ConsumePlanet(planet.GetResources()); // Chama ConsumePlanet em EaterDesire
            }
        }

        private void HandlePlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (!_currentTarget || _currentTarget.GetComponent<Planets>() != evt.Planet) return;
            _currentTarget = null;
            _targetReached = false;
            _isEating = false;
            DebugUtility.Log<EaterAIController>($"EaterAI: Planeta {evt.Planet?.name} desmarcado. Alvo limpo, voltando a vagar.");
        }

        private void HandleStarved(EaterStarvedEvent evt)
        {
            enabled = false;
            DebugUtility.LogVerbose<EaterAIController>("EaterAI desativado devido à morte por fome.");
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        private void HandleDeath(DeathEvent evt)
        {
            if (evt.Source == gameObject)
            {
                enabled = false;
                DebugUtility.LogVerbose<EaterAIController>("EaterAI desativado devido à morte por dano.");
                EventBus<GameOverEvent>.Raise(new GameOverEvent());
            }
        }

        private void OnFinishEating()
        {
            _isEating = false;
            _currentTarget = null;
            DebugUtility.LogVerbose<EaterAIController>("EaterAI: Terminou de comer. Voltando a vagar.");
        }

        private float GetChaseSpeed()
        {
            float healthRatio = _health.GetCurrentHealth() / _health.GetMaxValue();
            return healthRatio <= 0.25f ? baseChaseSpeed * 1.5f : baseChaseSpeed;
        }

        public PlanetResourcesSo GetDesiredResource() => _desire.GetDesiredResource(); // Método para acessar o recurso desejado
    }
}