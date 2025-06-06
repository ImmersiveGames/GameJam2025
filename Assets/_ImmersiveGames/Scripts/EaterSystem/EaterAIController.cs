using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.EaterSystem.Predicates;
using _ImmersiveGames.Scripts.GameManagerSystems;
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

        private StateMachine.StateMachine _stateMachine;
        private EaterHealth _health;
        private EaterDetectable _detector;
        private EaterHunger _hunger;
        private EaterDesire _desire;
        private Transform _currentTarget;
        private bool _isEating;
        private EventBinding<PlanetMarkedEvent> _planetMarkedBinding;
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedBinding;
        private EventBinding<EaterStarvedEvent> _starvedBinding;
        private EventBinding<DeathEvent> _deathBinding;

        private void Awake()
        {
            _health = GetComponent<EaterHealth>();
            _detector = GetComponent<EaterDetectable>();
            _hunger = GetComponent<EaterHunger>();
            _desire = GetComponent<EaterDesire>();
            _detector.OnTargetUpdated += HandleTargetUpdated;
            _detector.OnEatPlanet += HandleEatPlanet;
            _planetMarkedBinding = new EventBinding<PlanetMarkedEvent>(HandlePlanetMarked);
            _planetUnmarkedBinding = new EventBinding<PlanetUnmarkedEvent>(HandlePlanetUnmarked);
            _starvedBinding = new EventBinding<EaterStarvedEvent>(HandleStarved);
            _deathBinding = new EventBinding<DeathEvent>(HandleDeath);
        }

        private void OnEnable()
        {
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedBinding);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedBinding);
            EventBus<EaterStarvedEvent>.Register(_starvedBinding);
            EventBus<DeathEvent>.Register(_deathBinding);
        }

        private void OnDisable()
        {
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedBinding);
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
            var wanderState = new WanderState(transform,  minWanderSpeed, maxWanderSpeed, directionChangeInterval);
            var chaseState = new ChaseState(
                transform,
                () => _currentTarget,
                GetChaseSpeed
            );
            var eatingState = new EatingState(
                _hunger,
                _detector,
                OnFinishEating
            );

            _stateMachine = new StateMachineBuilder()
                .AddState(wanderState, out var wanderRef)
                .AddState(chaseState, out var chaseRef)
                .AddState(eatingState, out var eatingRef)

                // Wander → Chase: Só entra em perseguição se estiver com fome, tiver desejo, e tiver planeta marcado
                .At(wanderRef, chaseRef, new BoolPredicate(() =>
                    _hunger.IsHungry &&
                    _desire.GetDesiredResource() != null &&
                    _currentTarget != null
                ))

                // Chase → Eating: começou a comer
                .At(chaseRef, eatingRef, new BoolPredicate(() => _isEating))

                // Eating → Wander: terminou de comer
                .At(eatingRef, wanderRef, new BoolPredicate(() => !_isEating))

                // Chase → Wander: perdeu o alvo (planeta desmarcado ou sumiu)
                .At(chaseRef, wanderRef, new BoolPredicate(() => _currentTarget == null))

                .StateInitial(wanderRef)
                .Build();
        }

        private void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _stateMachine?.Update();
        }

        private void FixedUpdate()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _stateMachine?.FixedUpdate();
        }

        private void HandleTargetUpdated(Transform target)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _currentTarget = target;
            _isEating = false;
            DebugUtility.Log<EaterAIController>($"Novo alvo definido: {target?.name ?? "nenhum"}.");
        }

        private void HandlePlanetMarked(PlanetMarkedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _currentTarget = evt.PlanetMaster.transform;
            DebugUtility.Log<EaterAIController>($"Planeta marcado: {evt.PlanetMaster.name}. Iniciando perseguição.");
        }

        private void HandleEatPlanet(PlanetsMaster planetMaster)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            if (_currentTarget == planetMaster.transform)
            {
                _isEating = true;
                EventBus<EaterStartedEatingEvent>.Raise(new EaterStartedEatingEvent());
                _desire.ConsumePlanet(planetMaster.GetResources());
                DebugUtility.Log<EaterAIController>($"Iniciando consumo do planeta: {planetMaster.name}.");
            }
        }

        private void HandlePlanetUnmarked(PlanetUnmarkedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            if (_currentTarget != null && _currentTarget.GetComponent<PlanetsMaster>() == evt.PlanetMaster)
            {
                _currentTarget = null;
                _isEating = false;
                DebugUtility.Log<EaterAIController>($"EaterAI: Planeta {evt.PlanetMaster?.name} desmarcado. Alvo limpo, voltando a vagar.");
            }
        }

        private void HandleStarved(EaterStarvedEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            enabled = false;
            DebugUtility.LogVerbose<EaterAIController>("EaterAI desativado devido à morte por fome.");
            EventBus<GameOverEvent>.Raise(new GameOverEvent());
        }

        private void HandleDeath(DeathEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            if (evt.Source == gameObject)
            {
                enabled = false;
                DebugUtility.LogVerbose<EaterAIController>("EaterAI desativado devido à morte por dano.");
                EventBus<GameOverEvent>.Raise(new GameOverEvent());
            }
        }

        private void OnFinishEating()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _isEating = false;
            _currentTarget = null;
            DebugUtility.LogVerbose<EaterAIController>("EaterAI: Terminou de comer. Voltando a vagar.");
        }

        private float GetChaseSpeed()
        {
            float healthRatio = _health.GetCurrentHealth() / _health.GetMaxValue();
            return healthRatio <= 0.25f ? baseChaseSpeed * 1.5f : baseChaseSpeed;
        }

        public PlanetResourcesSo GetDesiredResource() => _desire.GetDesiredResource();
    }
}