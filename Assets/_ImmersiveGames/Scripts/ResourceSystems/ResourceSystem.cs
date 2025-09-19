using System;
using System.Collections.Generic;
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

    public abstract class ResourceSystem : MonoBehaviour, IResourceValue, IResourceThreshold, IResettable
    {
        [SerializeField] protected ResourceConfigSo config;
        [SerializeField] private ModifierManager _modifierManager;
        public event Action EventDepleted;
        public event Action<float> EventValueChanged;
        public event Action<float> OnThresholdReached;
        [SerializeField] public ResourceThresholdUnityEvent onThresholdReached;
        protected float maxValue;
        protected float currentValue;
        protected readonly List<float> triggeredThresholds = new();
        private float _autoFillTimer;
        private float _autoDrainTimer;
        private bool _autoFillEnabled;
        private bool _autoDrainEnabled;
        private float _autoFillRate;
        private float _autoDrainRate;
        private bool _isGameActive;
        private EventBinding<StateChangedEvent> _stateChangedBinding;
        private string _uniqueId;

        protected virtual void Awake()
        {
            if (!config)
            {
                DebugUtility.LogError<ResourceSystem>("ResourceConfigSO não atribuído!", this);
                return;
            }
            maxValue = config.MaxValue;
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
            _isGameActive = false;
            _autoFillTimer = config.AutoChangeDelay;
            _autoDrainTimer = config.AutoChangeDelay;
            _modifierManager = GetComponent<ModifierManager>() ?? gameObject.AddComponent<ModifierManager>();
            _uniqueId = UniqueIdFactory.Instance.GenerateId(gameObject, config.UniqueId);
            DebugUtility.LogVerbose<ResourceSystem>($"Awake: Inicializado ResourceSystem com UniqueId={_uniqueId}, InitialValue={currentValue}, MaxValue={maxValue}, AutoFillEnabled={config.AutoFillEnabled}, AutoDrainEnabled={config.AutoDrainEnabled}, Source={gameObject.name}");
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
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, GetPercentage(), true, actorId));
            DebugUtility.LogVerbose<ResourceSystem>($"Start: Disparado ResourceBindEvent para UniqueId={_uniqueId}, ActorId={actorId}, Source={gameObject.name}");
        }

        protected virtual void Update()
        {
            if (!_isGameActive)
            {
                DebugUtility.LogWarning<ResourceSystem>($"Update ignorado: Jogo não está ativo (_isGameActive={_isGameActive}), Source={gameObject.name}");
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
            DebugUtility.LogVerbose<ResourceSystem>($"OnDisable: Desregistrado binding de StateChangedEvent, Source={gameObject.name}");
        }

        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.IsGameActive;
            DebugUtility.LogVerbose<ResourceSystem>($"OnStateChanged: Estado do jogo atualizado: IsGameActive={_isGameActive} (Estado: {evt.StateName}), Source={gameObject.name}");
        }

        private void ApplyModifiers()
        {
            if (_modifierManager != null)
            {
                float delta = _modifierManager.UpdateAndGetDelta(Time.deltaTime);
                if (delta > 0)
                    Increase(delta);
                else if (delta < 0)
                    Decrease(-delta);
            }
        }

        private void AutoFill()
        {
            if (!_autoFillEnabled || currentValue >= maxValue) return;
            _autoFillTimer += Time.deltaTime;
            if (_autoFillTimer >= config.AutoChangeDelay)
            {
                var baseRate = _autoFillRate;
                var modifiedRate = _modifierManager != null ? _modifierManager.UpdateAndGetDelta(config.AutoChangeDelay, baseRate) : baseRate;
                Increase(modifiedRate);
                _autoFillTimer = 0f;
                DebugUtility.LogVerbose<ResourceSystem>($"AutoFill aplicado: Base={baseRate:F5}, Modified={modifiedRate:F5}, CurrentValue={currentValue:F5}, Source={gameObject.name}");
            }
        }

        private void AutoDrain()
        {
            if (!_autoDrainEnabled || currentValue <= 0) return;
            _autoDrainTimer += Time.deltaTime;
            if (_autoDrainTimer >= config.AutoChangeDelay)
            {
                var baseRate = _autoDrainRate;
                var modifiedRate = _modifierManager != null ? _modifierManager.UpdateAndGetDelta(config.AutoChangeDelay, baseRate) : baseRate;
                Decrease(modifiedRate);
                _autoDrainTimer = 0f;
                DebugUtility.LogVerbose<ResourceSystem>($"AutoDrain aplicado: Base={baseRate:F5}, Modified={modifiedRate:F5}, CurrentValue={currentValue:F5}, Source={gameObject.name}");
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
            DebugUtility.LogVerbose<ResourceSystem>($"SetExternalAutoChange: {(isFill ? "AutoFill" : "AutoDrain")} {(auto ? "ATIVADO" : "DESATIVADO")} a {rate:F2} por segundo, Source={gameObject.name}");
        }

        private void UpdateResourceValue(float amount, bool isIncrease)
        {
            if (amount < 0)
            {
                DebugUtility.LogWarning<ResourceSystem>($"UpdateResourceValue: Tentativa de atualizar recurso com valor inválido (amount={amount}), Source={gameObject.name}");
                return;
            }
            float oldValue = currentValue;
            float modifiedAmount = _modifierManager != null ? _modifierManager.UpdateAndGetDelta(Time.deltaTime, amount) : amount;
            float newValue = isIncrease
                ? Mathf.Min(maxValue, currentValue + modifiedAmount)
                : Mathf.Max(0, currentValue - modifiedAmount);
            if (Mathf.Approximately(newValue, currentValue)) return;
            currentValue = newValue;
            var actorId = GetActorId();
            float percentage = GetPercentage();
            EventValueChanged?.Invoke(percentage);
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, percentage, isIncrease, actorId));
            CheckThresholds();
            DebugUtility.LogVerbose<ResourceSystem>($"UpdateResourceValue: IsIncrease={isIncrease}, Amount={amount:F5}, Modified={modifiedAmount:F5}, OldValue={oldValue:F5}, NewValue={currentValue:F5}, Source={gameObject.name}");
            if (!isIncrease && currentValue <= 0)
            {
                OnResourceDepleted();
            }
        }

        public void Increase(float amount) => UpdateResourceValue(amount, true);
        public void Decrease(float amount) => UpdateResourceValue(amount, false);
        protected virtual void OnResourceDepleted() => EventDepleted?.Invoke();

        public void CheckThresholds()
        {
            float percentage = GetPercentage();
            var actorId = GetActorId();
            foreach (float threshold in config.Thresholds)
            {
                if (percentage <= threshold && !triggeredThresholds.Contains(threshold))
                {
                    triggeredThresholds.Add(threshold);
                    OnThresholdReached?.Invoke(threshold);
                    EventBus<ResourceThresholdCrossedEvent>.Raise(new ResourceThresholdCrossedEvent(_uniqueId, gameObject, new ThresholdCrossInfo(percentage, threshold, false), actorId));
                }
                else if (percentage > threshold && triggeredThresholds.Contains(threshold))
                {
                    triggeredThresholds.Remove(threshold);
                    OnThresholdReached?.Invoke(threshold);
                    EventBus<ResourceThresholdCrossedEvent>.Raise(new ResourceThresholdCrossedEvent(_uniqueId, gameObject, new ThresholdCrossInfo(percentage, threshold, true), actorId));
                }
            }
        }

        public virtual void Reset(bool resetSkin = true)
        {
            currentValue = resetSkin ? config.InitialValue : currentValue;
            triggeredThresholds.Clear();
            _modifierManager?.RemoveAllModifiers();
            _autoFillTimer = config.AutoChangeDelay;
            _autoDrainTimer = config.AutoChangeDelay;
            var actorId = GetActorId();
            EventValueChanged?.Invoke(GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(_uniqueId, gameObject, config.ResourceType, GetPercentage(), true, actorId));
        }

        public void OnEventValueChanged(float checkValue)
        {
            EventValueChanged?.Invoke(checkValue);
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            _modifierManager?.AddModifier(amountPerSecond, duration, isPermanent);
        }

        public void RemoveAllModifiers()
        {
            _modifierManager?.RemoveAllModifiers();
        }

        public float GetCurrentValue() => currentValue;
        public float GetMaxValue() => maxValue;
        public float GetPercentage() => maxValue > 0 ? currentValue / maxValue : 0;

        private string GetActorId()
        {
            var actor = GetComponentInParent<IActor>();
            if (actor != null)
            {
                DebugUtility.LogVerbose<ResourceSystem>($"GetActorId: Encontrado IActor com Name={actor.Name}, Source={gameObject.name}");
                return actor.Name;
            }
            DebugUtility.LogWarning<ResourceSystem>($"GetActorId: Nenhum IActor encontrado em {gameObject.name}. Usando ID vazio.", this);
            return "";
        }

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
        }

        public ResourceConfigSo Config => config;
        public string UniqueId => _uniqueId;
    }
}