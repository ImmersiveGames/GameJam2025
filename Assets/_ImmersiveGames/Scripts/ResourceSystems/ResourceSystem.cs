using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.NewResourceSystem;
using _ImmersiveGames.Scripts.NewResourceSystem.Events;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [RequireComponent(typeof(IActor))]
    public abstract class ResourceSystem : MonoBehaviour, IResource
    {
        [SerializeField] protected ResourceConfigSo config;
        private string _uniqueId;
        private string _actorId;
        private bool _isGameActive;
        [Inject] private IResourceValue _valueService;
        [Inject] private IResourceThreshold _thresholdService;
        [Inject] private IResourceModifier _modifierService;
        [Inject] private IAutoChange _autoChangeService;
        private float _previousValue;
        private EventBinding<StateChangedEvent> _stateChangedBinding;
        public event Action EventDepleted;
        public event Action<float> EventValueChanged;

        protected virtual void Awake()
        {
            if (!config)
            {
                DebugUtility.LogError<ResourceSystem>("ResourceConfigSO não atribuído!", this);
                return;
            }

            _actorId = GetActorId();
            _uniqueId = UniqueIdFactory.Instance.GenerateId(gameObject, config.UniqueId);

            var state = new ResourceState(config.InitialValue, config.MaxValue);
            IAutoChangeStrategy strategy = config.AutoFillEnabled ? new AutoFillStrategy() : (IAutoChangeStrategy)new AutoDrainStrategy();
            DependencyManager.Instance.RegisterForObject(_uniqueId, new ResourceValueService(config));
            DependencyManager.Instance.RegisterForObject(_uniqueId, new ThresholdMonitorService(config, state, _actorId, gameObject));
            DependencyManager.Instance.RegisterForObject(_uniqueId, new ModifierService(config.UniqueId, gameObject, _actorId));
            DependencyManager.Instance.RegisterForObject(_uniqueId, new AutoChangeService(strategy, config, 
                DependencyManager.Instance.GetForObject<IResourceValue>(_uniqueId),
                DependencyManager.Instance.GetForObject<IResourceModifier>(_uniqueId)));

            DependencyManager.Instance.InjectDependencies(this, _uniqueId);

            if (_valueService == null || _thresholdService == null || _modifierService == null || _autoChangeService == null)
            {
                DebugUtility.LogError<ResourceSystem>($"Falha ao injetar serviços para UniqueId={_uniqueId}, Source={gameObject.name}");
                return;
            }

            _previousValue = _valueService.GetCurrentValue();
            DebugUtility.LogVerbose<ResourceSystem>(
                $"Awake: Inicializado ResourceSystem UniqueId={_uniqueId}, ActorId={_actorId}, " +
                $"InitialValue={_valueService.GetCurrentValue():F2}, MaxValue={_valueService.GetMaxValue():F2}, Source={gameObject.name}");
        }

        protected virtual void OnEnable()
        {
            _stateChangedBinding = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedBinding);

            DebugUtility.LogVerbose<ResourceSystem>(
                $"OnEnable: ActorId={_actorId}, AutoFill={config.AutoFillEnabled} Rate={config.AutoFillRate:F2}, " +
                $"AutoDrain={config.AutoDrainEnabled} Rate={config.AutoDrainRate:F2}, Source={gameObject.name}");
        }

        protected virtual void OnDisable()
        {
            if (_stateChangedBinding != null)
                EventBus<StateChangedEvent>.Unregister(_stateChangedBinding);
        }

        private void Start()
        {
            EventBus<ResourceBindEvent>.Raise(new ResourceBindEvent(
                gameObject, config.ResourceType, _uniqueId, this, _actorId));

            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(
                _uniqueId, gameObject, config.ResourceType, _valueService.GetPercentage(), true, _actorId));
        }

        protected virtual void Update()
        {
            if (!_isGameActive) return;

            _autoChangeService.Tick(Time.deltaTime);
            ApplyModifiers();
            NotifyChange(_valueService.GetCurrentValue() >= _previousValue);
            _previousValue = _valueService.GetCurrentValue();
        }

        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.IsGameActive;
        }

        private void ApplyModifiers()
        {
            float delta = _modifierService.UpdateAndGetDelta(Time.deltaTime);
            if (delta == 0) return;

            if (delta > 0) _valueService.Increase(delta);
            else _valueService.Decrease(-delta);

            _thresholdService.CheckThresholds();
        }

        public void Increase(float amount)
        {
            _valueService.Increase(amount);
            NotifyChange(true);
            _previousValue = _valueService.GetCurrentValue();

            if (_valueService.GetCurrentValue() <= 0)
                OnResourceDepleted();
        }

        public void Decrease(float amount)
        {
            _valueService.Decrease(amount);
            NotifyChange(false);
            _previousValue = _valueService.GetCurrentValue();

            if (_valueService.GetCurrentValue() <= 0)
                OnResourceDepleted();
        }

        public void SetCurrentValue(float value)
        {
            _valueService.SetCurrentValue(value);
            NotifyChange(value >= _previousValue);
            _previousValue = _valueService.GetCurrentValue();
        }

        public ResourceState GetState()
        {
            return new ResourceState(_valueService.GetCurrentValue(), _valueService.GetMaxValue());
        }

        private void NotifyChange(bool isAscending)
        {
            EventValueChanged?.Invoke(_valueService.GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(
                _uniqueId, gameObject, config.ResourceType, _valueService.GetPercentage(), isAscending, _actorId));
            _thresholdService.CheckThresholds();
        }

        protected virtual void OnResourceDepleted()
        {
            DebugUtility.LogVerbose<ResourceSystem>($"ResourceDepleted: {config.ResourceType}, UniqueId={_uniqueId}, ActorId={_actorId}, Source={gameObject.name}");
            EventDepleted?.Invoke();
        }

        public void CheckThresholds() => _thresholdService.CheckThresholds();

        public virtual void Reset(bool resetSkin = true)
        {
            _valueService.SetCurrentValue(resetSkin ? config.InitialValue : _valueService.GetCurrentValue());
            IAutoChangeStrategy strategy = config.AutoFillEnabled ? new AutoFillStrategy() : (IAutoChangeStrategy)new AutoDrainStrategy();
            DependencyManager.Instance.RegisterForObject(_uniqueId, new ThresholdMonitorService(config, GetState(), _actorId, gameObject));
            DependencyManager.Instance.RegisterForObject(_uniqueId, new AutoChangeService(strategy, config, 
                DependencyManager.Instance.GetForObject<IResourceValue>(_uniqueId),
                DependencyManager.Instance.GetForObject<IResourceModifier>(_uniqueId)));
            _thresholdService = DependencyManager.Instance.GetForObject<IResourceThreshold>(_uniqueId);
            _autoChangeService = DependencyManager.Instance.GetForObject<IAutoChange>(_uniqueId);
            _modifierService.RemoveAllModifiers();

            NotifyChange(true);
            _previousValue = _valueService.GetCurrentValue();
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false) =>
            _modifierService.AddModifier(amountPerSecond, duration, isPermanent);

        public void RemoveAllModifiers() =>
            _modifierService.RemoveAllModifiers();

        public float GetCurrentValue() => _valueService.GetCurrentValue();
        public float GetMaxValue() => _valueService.GetMaxValue();
        public float GetPercentage() => _valueService.GetPercentage();

        public ResourceConfigSo Config => config;
        public ResourceType Type => config.ResourceType;
        public string UniqueId => _uniqueId;
        public string ActorId => _actorId;
        public GameObject Source => gameObject;

        public string GetActorId()
        {
            var actor = GetComponentInParent<IActor>();
            if (actor == null)
            {
                DebugUtility.LogError<ResourceSystem>("IActor não encontrado!", this);
                return string.Empty;
            }

            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                string getActorId = $"Player_{playerInput.playerIndex}";
                DebugUtility.LogVerbose<ResourceSystem>($"GetActorId: ActorName={actor.Name}, PlayerIndex={playerInput.playerIndex}, ActorId={getActorId}");
                return getActorId;
            }

            int instanceId = UniqueIdFactory.Instance.GetInstanceCount(actor.Name);
            string actorId = $"NPC_{actor.Name}_{instanceId}";
            DebugUtility.LogVerbose<ResourceSystem>($"GetActorId: ActorName={actor.Name}, InstanceId={instanceId}, ActorId={actorId}");
            return actorId;
        }

        public void SetStrategy(IThresholdStrategy strategy)
        {
            DependencyManager.Instance.RegisterForObject(_uniqueId, new ThresholdMonitorService(config, GetState(), _actorId, gameObject, strategy));
            _thresholdService = DependencyManager.Instance.GetForObject<IResourceThreshold>(_uniqueId);
        }
        public void SetConfig(ResourceConfigSo resourceConfigSo)
        {
            config = resourceConfigSo;
        }
    }
}