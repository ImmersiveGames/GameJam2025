/*using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using UnityEngine;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.EaterSystem.States;
using _ImmersiveGames.Scripts.PlanetSystems.Events;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StatesMachines;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    [RequireComponent(typeof(EaterMaster))]
    
    public class EaterMovement : MonoBehaviour, IResettable
    {
        private StateMachine _stateMachine;
        private EaterMaster _eater;
        private SensorController _sensorController;
        private EaterConfigSo _config;

        private EventBinding<PlanetUnmarkedEvent> _planetUnmarkedEventBinding;
        private EventBinding<PlanetMarkedEvent> _planetMarkedEventBinding;
        public bool IsOrbiting {get; set;}
        public Transform TargetTransform => _target?.Detectable.Transform;
        private IDetectable _target;
        public IDetectable Target {
            get => _target;
            set {
                DebugUtility.LogVerbose<>($"[Eater] Target {(value == null ? "cleared" : "set to " + value.Detectable.ActorName)}");
                _target = value;
            }
        }

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
                .Any(_chasingState, new FuncPredicate(() => _target != null && !IsOrbiting))
                .Any(_orbitingState, new FuncPredicate(() => _target != null && IsOrbiting))
                .Any(_wanderingState, new PredicateTargetIsNull(() => _target))
                .StateInitial(_wanderingState);
            _stateMachine = builder.Build();

        }
        
        private void OnMarkedPlanet(PlanetMarkedEvent obj)
        {
            _target = obj.Detected;
            IsOrbiting = _sensorController.IsObjectInSensorRange(_target, SensorTypes.EaterEatSensor);
            if (IsOrbiting)
            {
                _eater.OnEventStartEatPlanet(obj.Detected);
                DebugUtility.LogVerbose<EaterMovement>($"Planeta marcado: {_target.Detectable.Transform} está na área do EaterEatSensor, ativando órbita.", "green");
            }
            else
            {
                DebugUtility.LogVerbose<EaterMovement>($"Planeta marcado: {_target.Detectable.Transform}, fora da área do EaterEatSensor.", "yellow");
            }
            // Forçar verificação imediata da máquina de estados
            _stateMachine.Update();
        }

        private void OnUnmarkedPlanet(PlanetUnmarkedEvent obj)
        {
            if (IsOrbiting)
            {
                _eater.OnEventEndEatPlanet(obj.Detected);
            }
            _target = null;
            IsOrbiting = false;
        }
        
        private void OnPlanetDetected(IDetectable obj, SensorTypes sensor)
        {
            DebugUtility.LogVerbose<EaterMovement>($"Planeta detectado: {obj.Detectable.ActorName}, Sensor: {sensor}, IsOrbiting: {IsOrbiting}");
            if (sensor != SensorTypes.EaterEatSensor || !PlanetsManager.Instance.IsMarkedPlanet(obj)) return;
            _target = obj;
            IsOrbiting = true;
            _eater.OnEventStartEatPlanet(obj);
            DebugUtility.Log<EaterMovement>($"[{_stateMachine.CurrentState}] Alvo {_target.Detectable.ActorName}, IsOrbiting: {IsOrbiting}");
        }

        public void Reset(bool resetSkin = true)
        {
            IsOrbiting = false;
            _target = null;
        }
    }
    
}*/