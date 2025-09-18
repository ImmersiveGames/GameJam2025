using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public class ResourceThresholdUnityEvent : UnityEvent<float> { }

    public abstract class ResourceSystem : MonoBehaviour, IResourceValue, IResourceThreshold, IResettable
    {
        [SerializeField] protected ResourceConfigSo config;
        public event Action EventDepleted;
        public event Action<float> EventValueChanged;
        public event Action<float> OnThresholdReached;
        [SerializeField] public ResourceThresholdUnityEvent onThresholdReached;
        private float _maxValue;
        protected float currentValue;
        protected readonly List<float> triggeredThresholds = new();
        private readonly List<ResourceModifier> _modifiers = new();
        private float _autoFillTimer;
        private float _autoDrainTimer;
        private bool _autoFillEnabled;
        private bool _autoDrainEnabled;
        private float _autoFillRate;
        private float _autoDrainRate;
        private bool _isGameActive;
        private EventBinding<StateChangedEvent> _stateChangedBinding;

        protected virtual void Awake()
        {
            if (!config)
            {
                DebugUtility.LogError<ResourceSystem>("ResourceConfigSO não atribuído!", this);
                return;
            }
            _maxValue = config.MaxValue;
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            _isGameActive = false;
            _autoFillTimer = config.AutoChangeDelay;
            _autoDrainTimer = config.AutoChangeDelay;
            DebugUtility.LogVerbose<ResourceSystem>($"Awake: Inicializado ResourceSystem com UniqueId={config.UniqueId}, InitialValue={currentValue}, MaxValue={_maxValue}, AutoFillEnabled={config.AutoFillEnabled}, AutoDrainEnabled={config.AutoDrainEnabled}, AutoFillRate={config.AutoFillRate}, AutoDrainRate={config.AutoDrainRate}, AutoChangeDelay={config.AutoChangeDelay}");
        }

        protected virtual void OnEnable()
        {
            _autoFillEnabled = config.AutoFillEnabled;
            _autoDrainEnabled = config.AutoDrainEnabled;
            _autoFillRate = Mathf.Max(0, config.AutoFillRate);
            _autoDrainRate = Mathf.Max(0, config.AutoDrainRate);
            _stateChangedBinding = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedBinding);
            DebugUtility.LogVerbose<ResourceSystem>($"OnEnable: AutoFillEnabled={_autoFillEnabled} (Rate={_autoFillRate}), AutoDrainEnabled={_autoDrainEnabled} (Rate={_autoDrainRate}), AutoChangeDelay={config.AutoChangeDelay}");
        }

        private void Start()
        {
            EventBus<ResourceBindEvent>.Raise(new ResourceBindEvent(gameObject, config.ResourceType, config.UniqueId, this));
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(config.UniqueId, gameObject, config.ResourceType, GetPercentage(), true));
            DebugUtility.LogVerbose<ResourceSystem>($"Start: Disparado ResourceBindEvent para UniqueId={config.UniqueId}");
        }

        protected virtual void Update()
        {
            if (!_isGameActive)
            {
                DebugUtility.LogWarning<ResourceSystem>($"Update ignorado: Jogo não está ativo (_isGameActive={_isGameActive})");
                return;
            }
            AutoFill();
            AutoDrain();
            ApplyModifiers();
        }

        protected virtual void OnDisable()
        {
            if (_stateChangedBinding != null)
            {
                EventBus<StateChangedEvent>.Unregister(_stateChangedBinding);
            }
            DebugUtility.LogVerbose<ResourceSystem>($"OnDisable: Desregistrado binding de StateChangedEvent");
        }

        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.IsGameActive;
            DebugUtility.LogVerbose<ResourceSystem>($"OnStateChanged: Estado do jogo atualizado: IsGameActive={_isGameActive} (Estado: {evt.StateName})");
        }

        private void ApplyModifiers()
        {
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (modifier.amountPerSecond > 0)
                    Increase(modifier.amountPerSecond * Time.deltaTime);
                else
                    Decrease(-modifier.amountPerSecond * Time.deltaTime);

                if (modifier.Update(Time.deltaTime))
                    _modifiers.RemoveAt(i);
            }
        }

        private void AutoFill()
        {
            if (!_autoFillEnabled || currentValue >= _maxValue)
                return;
            _autoFillTimer += Time.deltaTime;
            if (_autoFillTimer >= config.AutoChangeDelay)
            {
                Increase(_autoFillRate);
                _autoFillTimer = 0f;
                DebugUtility.LogVerbose<ResourceSystem>($"AutoFill aplicado: Amount={_autoFillRate:F5}, CurrentValue={currentValue:F5}, AutoChangeDelay={config.AutoChangeDelay}");
            }
        }

        private void AutoDrain()
        {
            if (!_autoDrainEnabled || currentValue <= 0)
                return;
            _autoDrainTimer += Time.deltaTime;
            if (_autoDrainTimer >= config.AutoChangeDelay)
            {
                Decrease(_autoDrainRate);
                _autoDrainTimer = 0f;
                DebugUtility.LogVerbose<ResourceSystem>($"AutoDrain aplicado: Amount={_autoDrainRate:F5}, CurrentValue={currentValue:F5}, AutoChangeDelay={config.AutoChangeDelay}");
            }
        }

        public void SetExternalAutoChange(bool isFill, bool auto, float rate)
        {
            rate = Mathf.Max(0, rate);
            if (isFill)
            {
                _autoFillEnabled = auto;
                _autoFillRate = rate;
                _autoFillTimer = config.AutoChangeDelay;
            }
            else
            {
                _autoDrainEnabled = auto;
                _autoDrainRate = rate;
                _autoDrainTimer = config.AutoChangeDelay;
            }
            DebugUtility.LogVerbose<ResourceSystem>($"SetExternalAutoChange: {(isFill ? "AutoFill" : "AutoDrain")} {(auto ? "ATIVADO" : "DESATIVADO")} a {rate:F2} por segundo, AutoChangeDelay={config.AutoChangeDelay}, UniqueId={config.UniqueId}");
        }

        private void UpdateResourceValue(float amount, bool isIncrease)
        {
            if (amount < 0)
            {
                DebugUtility.LogWarning<ResourceSystem>($"UpdateResourceValue: Tentativa de atualizar recurso com valor inválido (amount={amount})");
                return;
            }
            float oldValue = currentValue;
            currentValue = isIncrease
                ? Mathf.Min(_maxValue, currentValue + amount)
                : Mathf.Max(0, currentValue - amount);
            float percentage = GetPercentage();
            EventValueChanged?.Invoke(percentage);
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(config.UniqueId, gameObject, config.ResourceType, percentage, isIncrease));
            CheckThresholds();
            DebugUtility.LogVerbose<ResourceSystem>($"UpdateResourceValue: IsIncrease={isIncrease}, Amount={amount:F5}, OldValue={oldValue:F5}, NewValue={currentValue:F5}, Percentage={percentage:F3}");
            if (!isIncrease && currentValue <= 0)
            {
                OnResourceDepleted();
            }
            else if (isIncrease && currentValue >= _maxValue)
            {
                EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(config.UniqueId, gameObject, config.ResourceType, percentage, true));
            }
        }

        public void Increase(float amount) => UpdateResourceValue(amount, true);
        public void Decrease(float amount) => UpdateResourceValue(amount, false);
        protected virtual void OnResourceDepleted() => EventDepleted?.Invoke();

        public void CheckThresholds()
        {
            float percentage = GetPercentage();
            foreach (float threshold in config.Thresholds)
            {
                if (percentage <= threshold && !triggeredThresholds.Contains(threshold))
                {
                    triggeredThresholds.Add(threshold);
                    OnThresholdReached?.Invoke(threshold);
                    EventBus<ResourceThresholdCrossedEvent>.Raise(new ResourceThresholdCrossedEvent(config.UniqueId, gameObject, new ThresholdCrossInfo(percentage, threshold, false)));
                    DebugUtility.LogVerbose<ResourceSystem>($"CheckThresholds: Threshold cruzado (descendente): Threshold={threshold:F3}, Percentage={percentage:F3}, UniqueId={config.UniqueId}");
                }
                else if (percentage > threshold && triggeredThresholds.Contains(threshold))
                {
                    triggeredThresholds.Remove(threshold);
                    OnThresholdReached?.Invoke(threshold);
                    EventBus<ResourceThresholdCrossedEvent>.Raise(new ResourceThresholdCrossedEvent(config.UniqueId, gameObject, new ThresholdCrossInfo(percentage, threshold, true)));
                    DebugUtility.LogVerbose<ResourceSystem>($"CheckThresholds: Threshold cruzado (ascendente): Threshold={threshold:F3}, Percentage={percentage:F3}, UniqueId={config.UniqueId}");
                }
            }
        }

        public virtual void Reset(bool resetSkin = false)
        {
            currentValue = resetSkin ? config.InitialValue : currentValue;
            triggeredThresholds.Clear();
            _modifiers.Clear();
            _autoFillTimer = config.AutoChangeDelay;
            _autoDrainTimer = config.AutoChangeDelay;
            EventValueChanged?.Invoke(GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(config.UniqueId, gameObject, config.ResourceType, GetPercentage(), true));
            DebugUtility.LogVerbose<ResourceSystem>($"Reset: CurrentValue={currentValue:F5}, UniqueId={config.UniqueId}");
        }

        public void OnEventValueChanged(float checkValue)
        {
            EventValueChanged?.Invoke(checkValue);
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            _modifiers.Add(new ResourceModifier(amountPerSecond, duration, isPermanent));
            DebugUtility.LogVerbose<ResourceSystem>($"AddModifier: AmountPerSecond={amountPerSecond:F2}, Duration={duration:F2}, IsPermanent={isPermanent}");
        }

        public void RemoveAllModifiers()
        {
            _modifiers.Clear();
            DebugUtility.LogVerbose<ResourceSystem>($"RemoveAllModifiers: Todos os modificadores removidos, UniqueId={config.UniqueId}");
        }

        public float GetCurrentValue() => currentValue;
        public float GetMaxValue() => _maxValue;
        public float GetPercentage() => _maxValue > 0 ? currentValue / _maxValue : 0;

        [Serializable]
        public class ResourceSaveData
        {
            public float currentValue;
            public List<float> triggeredThresholds;
        }

        public ResourceSaveData Save()
        {
            return new ResourceSaveData
            {
                currentValue = currentValue,
                triggeredThresholds = new List<float>(triggeredThresholds)
            };
        }

        public void Load(ResourceSaveData data)
        {
            currentValue = data.currentValue;
            triggeredThresholds.Clear();
            triggeredThresholds.AddRange(data.triggeredThresholds);
            EventValueChanged?.Invoke(GetPercentage());
            CheckThresholds();
            DebugUtility.LogVerbose<ResourceSystem>($"Load: CurrentValue={currentValue:F5}, UniqueId={config.UniqueId}");
        }

        public ResourceConfigSo Config => config;
    }
}