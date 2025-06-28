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
namespace _ImmersiveGames.Scripts.EaterSystem
{
    using FSM = StateMachine.StateMachine;
    [RequireComponent(typeof(EaterMaster))]
    [DebugLevel(DebugLevel.Logs)]
    public class EaterMovement : MonoBehaviour, IResettable
    {
        private FSM _stateMachine;
        private EaterMaster _eater;
        private SensorController _sensorController;
        private EaterConfigSo _config;

        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;
        public bool IsOrbiting {get; set;}
        public Transform TargetTransform => Target?.Detectable.Transform;
        public IDetectable Target { get; private set; }

        private IState _wanderingState;
        private IState _chasingState;
        private IState _orbitingState;

        private void Awake()
        {
            _eater = GetComponent<EaterMaster>();
            _sensorController = GetComponent<SensorController>();
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
                .AddState(new ChasingState(this, _config,_sensorController), out _chasingState)
                .AddState(new OrbitingState(this), out _orbitingState)
                .Any(_chasingState, new FuncPredicate(() => Target != null && !IsOrbiting))
                .Any(_orbitingState, new FuncPredicate(() => Target != null && IsOrbiting))
                .Any(_wanderingState, new PredicateTargetIsNull(() => Target))
                .StateInitial(_wanderingState);
            _stateMachine = builder.Build();

        }
        
        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            Target = obj.Detected;
            IsOrbiting = _sensorController.IsObjectInSensorRange(Target, SensorTypes.EaterEatSensor);
            if (IsOrbiting)
            {
                _eater.OnEventStartEatPlanet(obj.Detected);
                DebugUtility.LogVerbose<EaterMovement>($"Planeta marcado: {Target.Detectable.Transform} está na área do EaterEatSensor, ativando órbita.", "green");
            }
            else
            {
                DebugUtility.LogVerbose<EaterMovement>($"Planeta marcado: {Target.Detectable.Transform}, fora da área do EaterEatSensor.", "yellow");
            }
        }

        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            if (IsOrbiting)
            {
                _eater.OnEventEndEatPlanet(obj.Detected);
            }
            Target = null;
            IsOrbiting = false;
        }
        
        private void OnPlanetDetected(IDetectable obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<EaterMovement>($"Planeta detectado: {obj.Detectable.Name}, Sensor: {sensor}, IsOrbiting: {IsOrbiting}");
            if (sensor != SensorTypes.EaterEatSensor || !PlanetsManager.Instance.IsMarkedPlanet(obj)) return;
            Target = obj;
            IsOrbiting = true;
            _eater.OnEventStartEatPlanet(obj);
            DebugUtility.LogVerbose<EaterMovement>($"[{_stateMachine.CurrentState}] Alvo {Target.Detectable.Name}, IsOrbiting: {IsOrbiting}");
        }

        public void Reset()
        {
            IsOrbiting = false;
            Target = null;
        }
    }
    
}