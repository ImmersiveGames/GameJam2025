using System;
using _ImmersiveGames.Scripts.DetectionsSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.PlanetSystems.EventsBus;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.StateMachine;
using _ImmersiveGames.Scripts.Utils.Predicates;


namespace _ImmersiveGames.Scripts.EaterSystem
{
    using FSM = StateMachine.StateMachine;
    [RequireComponent(typeof(EaterMaster))]
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterMovement : MonoBehaviour, IResettable
    {
        private FSM _stateMachine;
        private EaterMaster _eater;
        private EaterConfigSo _config;
        private IDetectable _target;
        
        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;
        public bool IsOrbiting {get; set;}
        public Transform TargetTransform => _target?.Transform;
        
        private IState _wanderingState;
        private IState _chasingState;
        private IState _orbitingState;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            _config = _eater.GetConfig;
        }
        private void OnEnable()
        {
            _eater.EventPlanetDetected += OnPlanetDetected;
            _planetUnmarkedEventBinding = new EventBinding<PlanetUnmarkedEvent>(OnUnmarkedPlanet);
            EventBus<PlanetUnmarkedEvent>.Register(_planetUnmarkedEventBinding);

            _planetMarkedEventBinding = new EventBinding<PlanetMarkedEvent>(OnMarkedPlanet);
            EventBus<PlanetMarkedEvent>.Register(_planetMarkedEventBinding);
        }

        private void Start()
        {
            StateMachineInitialize();
        }

        private void OnDisable()
        {
            _eater.EventPlanetDetected -= OnPlanetDetected;
            EventBus<PlanetUnmarkedEvent>.Unregister(_planetUnmarkedEventBinding);
            EventBus<PlanetMarkedEvent>.Unregister(_planetMarkedEventBinding);
        }

        private void Update()
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _stateMachine.Update();
        }

        private void StateMachineInitialize()
        {
            Reset();
            var builder = new StateMachineBuilder();
            builder.AddState(new WanderingState(this, _config), out _wanderingState)
                .AddState(new ChasingState(this, _config), out _chasingState)
                .AddState(new OrbitingState(this), out _orbitingState)
                .Any(_chasingState, new FuncPredicate(() => _target != null && !IsOrbiting))
                .Any(_orbitingState, new FuncPredicate(() => _target != null && IsOrbiting))
                .Any(_wanderingState, new PredicateTargetIsNull(() => _target))
                .StateInitial(_wanderingState);
            _stateMachine = builder.Build();

        }
        
        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            _target = obj.Detected;
            IsOrbiting = false;
        }

        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            _target = null;
            IsOrbiting = false;
        }
        
        private void OnPlanetDetected(IDetectable obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<EaterMovement>($"Planeta detectado: {obj.Name}, Sensor: {sensor}, IsOrbiting: {IsOrbiting}");
            if (_stateMachine.CurrentState is not ChasingState || sensor != SensorTypes.EaterEatSensor || !PlanetsManager.Instance.IsMarkedPlanet(obj)) return;
            _target = obj.GetPlanetsMaster();
            IsOrbiting = true;
            DebugUtility.LogVerbose<EaterMovement>($"[{_stateMachine.CurrentState}] Alvo {_target}, IsOrbiting: {IsOrbiting}");
        }

        public void Reset()
        {
            IsOrbiting = false;
            _target = null;
        }
    }
    
    
    public class PredicateTargetIsNull : IPredicate
    {
        private readonly Func<IDetectable> _getTarget;

        public PredicateTargetIsNull(Func<IDetectable> getTarget)
        {
            _getTarget = getTarget;
        }

        public bool Evaluate()
        {
            return _getTarget() == null;
        }
    }

}