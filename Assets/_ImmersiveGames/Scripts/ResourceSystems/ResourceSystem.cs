using System;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public class ResourceThresholdUnityEvent : UnityEvent<float> { }

    [RequireComponent(typeof(IActor))]
    public abstract class ResourceSystem : MonoBehaviour, IResourceValue, IResourceThreshold, IResettable
    {
        [SerializeField] protected ResourceConfigSo config;
        private ValueCore _valueCore;
        private ThresholdMonitor _thresholdMonitor;
        private ModifierManager _modifierManager;
        private string _uniqueId;
        private float _autoFillTimer;
        private float _autoDrainTimer;
        private bool _autoFillEnabled;
        private bool _autoDrainEnabled;
        private float _autoFillRate;
        private float _autoDrainRate;
        private bool _isGameActive;
        private EventBinding<StateChangedEvent> _stateChangedBinding;
        public event Action EventDepleted;
        public event Action<float> EventValueChanged;
        public event Action<float> OnThresholdReached;
        [SerializeField] public ResourceThresholdUnityEvent onThresholdReached;

        protected virtual void Awake()
        {
            if (!config)
            {
                DebugUtility.LogError<ResourceSystem>("ResourceConfigSO não atribuído!", this);
                return;
            }
            _valueCore = new ValueCore(config);
            _thresholdMonitor = new ThresholdMonitor(config, _valueCore.GetState());
            _uniqueId = UniqueIdFactory.Instance.GenerateId(gameObject, config.UniqueId);
            _modifierManager = new ModifierManager(_uniqueId, gameObject, GetActorId());
            _autoFillTimer = config.AutoChangeDelay;
            _autoDrainTimer = config.AutoChangeDelay;
            DebugUtility.LogVerbose<ResourceSystem>($"Awake: Inicializado ResourceSystem com UniqueId={_uniqueId}, InitialValue={_valueCore.GetCurrentValue()}, MaxValue={_valueCore.GetMaxValue()}, Source={gameObject.name}");
        }

        protected virtual void OnEnable()
        {
            _autoFillEnabled = config.AutoFillEnabled;
            _autoDrainEnabled = config.AutoDrainEnabled;
            _autoFillRate = Mathf.Max(0, config.AutoFillRate);
            _autoDrainRate = Mathf.Max(0, config.AutoDrainRate);
            _stateChangedBinding = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedBinding);
            DebugUtility.LogVerbose<ResourceSystem>($"OnEnable: AutoFillEnabled={_autoFillEnabled} (Rate={_autoFillRate}), AutoDrainEnabled={_autoDrainEnabled} (Rate={_autoDrainRate}), Source={gameObject.name}");
        }

        private void Start()
        {
            var actorId = GetActorId();
            EventBus<ResourceBindEvent>.Raise(new ResourceBindEvent(gameObject, config.ResourceType, _uniqueId, this, actorId));
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), true, actorId));
            DebugUtility.LogVerbose<ResourceSystem>($"Start: Disparado ResourceBindEvent para UniqueId={_uniqueId}, ActorId={actorId}, Source={gameObject.name}");
        }

        protected virtual void Update()
        {
            if (!_isGameActive)
            {
                DebugUtility.LogVerbose<ResourceSystem>($"Update: Ignorado - Jogo não está ativo (_isGameActive={_isGameActive}), Source={gameObject.name}");
                return;
            }
            AutoFill();
            AutoDrain();
            ApplyModifiers();
        }

        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.IsGameActive;
            DebugUtility.LogVerbose<ResourceSystem>($"OnStateChanged: Estado do jogo atualizado: IsGameActive={_isGameActive}, Source={gameObject.name}");
        }

        private void ApplyModifiers()
        {
            float delta = _modifierManager.UpdateAndGetDelta(Time.deltaTime);
            if (delta > 0)
                _valueCore.Increase(delta);
            else if (delta < 0)
                _valueCore.Decrease(-delta);
            _thresholdMonitor.CheckThresholds();
            if (delta != 0)
            {
                EventValueChanged?.Invoke(_valueCore.GetPercentage());
                EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), delta > 0, GetActorId()));
                DebugUtility.LogVerbose<ResourceSystem>($"ApplyModifiers: Delta={delta:F2}, Percentage={_valueCore.GetPercentage():F3}, UniqueId={_uniqueId}, Source={gameObject.name}");
            }
        }

        private void AutoFill()
        {
            if (!_autoFillEnabled || _valueCore.GetCurrentValue() >= _valueCore.GetMaxValue())
            {
                DebugUtility.LogVerbose<ResourceSystem>($"AutoFill: Ignorado - AutoFillEnabled={_autoFillEnabled}, CurrentValue={_valueCore.GetCurrentValue():F2}, MaxValue={_valueCore.GetMaxValue():F2}, Source={gameObject.name}");
                return;
            }
            _autoFillTimer += Time.deltaTime;
            DebugUtility.LogVerbose<ResourceSystem>($"AutoFill: Timer={_autoFillTimer:F2}, AutoChangeDelay={config.AutoChangeDelay:F2}, Source={gameObject.name}");
            if (_autoFillTimer >= config.AutoChangeDelay)
            {
                float modifiedRate = _modifierManager.UpdateAndGetDelta(config.AutoChangeDelay, _autoFillRate);
                _valueCore.Increase(modifiedRate);
                _thresholdMonitor.CheckThresholds();
                EventValueChanged?.Invoke(_valueCore.GetPercentage());
                EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), true, GetActorId()));
                DebugUtility.LogVerbose<ResourceSystem>($"AutoFill: Aplicado - ModifiedRate={modifiedRate:F2}, CurrentValue={_valueCore.GetCurrentValue():F2}, Percentage={_valueCore.GetPercentage():F3}, UniqueId={_uniqueId}, Source={gameObject.name}");
                _autoFillTimer = 0f;
            }
        }

        private void AutoDrain()
        {
            if (!_autoDrainEnabled || _valueCore.GetCurrentValue() <= 0)
            {
                DebugUtility.LogVerbose<ResourceSystem>($"AutoDrain: Ignorado - AutoDrainEnabled={_autoDrainEnabled}, CurrentValue={_valueCore.GetCurrentValue():F2}, Source={gameObject.name}");
                return;
            }
            _autoDrainTimer += Time.deltaTime;
            DebugUtility.LogVerbose<ResourceSystem>($"AutoDrain: Timer={_autoDrainTimer:F2}, AutoChangeDelay={config.AutoChangeDelay:F2}, Source={gameObject.name}");
            if (_autoDrainTimer >= config.AutoChangeDelay)
            {
                float modifiedRate = _modifierManager.UpdateAndGetDelta(config.AutoChangeDelay, _autoDrainRate);
                _valueCore.Decrease(modifiedRate);
                _thresholdMonitor.CheckThresholds();
                EventValueChanged?.Invoke(_valueCore.GetPercentage());
                EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), false, GetActorId()));
                DebugUtility.LogVerbose<ResourceSystem>($"AutoDrain: Aplicado - ModifiedRate={modifiedRate:F2}, CurrentValue={_valueCore.GetCurrentValue():F2}, Percentage={_valueCore.GetPercentage():F3}, UniqueId={_uniqueId}, Source={gameObject.name}");
                _autoDrainTimer = 0f;
            }
        }

        public void Increase(float amount)
        {
            _valueCore.Increase(amount);
            EventValueChanged?.Invoke(_valueCore.GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), true, GetActorId()));
            _thresholdMonitor.CheckThresholds();
            if (_valueCore.GetCurrentValue() <= 0)
                OnResourceDepleted();
            DebugUtility.LogVerbose<ResourceSystem>($"Increase: Amount={amount:F2}, CurrentValue={_valueCore.GetCurrentValue():F2}, Percentage={_valueCore.GetPercentage():F3}, UniqueId={_uniqueId}, Source={gameObject.name}");
        }

        public void Decrease(float amount)
        {
            _valueCore.Decrease(amount);
            EventValueChanged?.Invoke(_valueCore.GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), false, GetActorId()));
            _thresholdMonitor.CheckThresholds();
            if (_valueCore.GetCurrentValue() <= 0)
                OnResourceDepleted();
            DebugUtility.LogVerbose<ResourceSystem>($"Decrease: Amount={amount:F2}, CurrentValue={_valueCore.GetCurrentValue():F2}, Percentage={_valueCore.GetPercentage():F3}, UniqueId={_uniqueId}, Source={gameObject.name}");
        }

        protected virtual void OnResourceDepleted() => EventDepleted?.Invoke();

        public void CheckThresholds() => _thresholdMonitor.CheckThresholds();

        public virtual void Reset(bool resetSkin = true)
        {
            _valueCore.SetCurrentValue(resetSkin ? config.InitialValue : _valueCore.GetCurrentValue());
            _thresholdMonitor = new ThresholdMonitor(config, _valueCore.GetState());
            _modifierManager.RemoveAllModifiers();
            _autoFillTimer = config.AutoChangeDelay;
            _autoDrainTimer = config.AutoChangeDelay;
            EventValueChanged?.Invoke(_valueCore.GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), true, GetActorId()));
            DebugUtility.LogVerbose<ResourceSystem>($"Reset: CurrentValue={_valueCore.GetCurrentValue():F2}, Percentage={_valueCore.GetPercentage():F3}, UniqueId={_uniqueId}, Source={gameObject.name}");
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            _modifierManager.AddModifier(amountPerSecond, duration, isPermanent);
            DebugUtility.LogVerbose<ResourceSystem>($"AddModifier: AmountPerSecond={amountPerSecond:F2}, Duration={duration:F2}, IsPermanent={isPermanent}, UniqueId={_uniqueId}, Source={gameObject.name}");
        }

        public void RemoveAllModifiers() => _modifierManager.RemoveAllModifiers();

        public float GetCurrentValue() => _valueCore.GetCurrentValue();
        public float GetMaxValue() => _valueCore.GetMaxValue();
        public float GetPercentage() => _valueCore.GetPercentage();

        private string GetActorId()
        {
            var actor = GetComponentInParent<IActor>();
            return actor?.Name ?? "";
        }

        public ResourceConfigSo Config => config;
        public string UniqueId => _uniqueId;

        public void SetModThresholdStrategy(float factor)
        {
            _thresholdMonitor.SetStrategy(new ModThresholdStrategy(factor));
            DebugUtility.LogVerbose<ResourceSystem>($"SetModThresholdStrategy: Factor={factor:F2}, UniqueId={_uniqueId}, Source={gameObject.name}");
        }
    }
}