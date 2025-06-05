using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public class ResourceThresholdEvent : UnityEvent<float> { }

    public abstract class ResourceSystem : MonoBehaviour, IResource
    {
        [SerializeField] protected ResourceConfigSo config; // Configuração do recurso
        [SerializeField] public UnityEvent onDepleted; // Evento disparado quando esgotado
        [SerializeField] public UnityEvent<float> onValueChanged; // Evento disparado quando valor muda
        [SerializeField] public ResourceThresholdEvent onThresholdReached; // Evento para limiares
        protected float maxValue; // Valor máximo do recurso
        protected float currentValue; // Valor atual do recurso
        protected readonly List<float> triggeredThresholds = new(); // Limiares já disparados
        protected readonly List<ResourceModifier> modifiers = new(); // Lista de modificadores

        private float _autoChangeTimer; // Temporizador para mudanças automáticas
        private bool _autoFillEnabled;
        private bool _autoDrainEnabled;
        private float _autoFillRate;
        private float _autoDrainRate;
        private GameManager _gameManager;

        protected virtual void Awake()
        {
            if (!config)
            {
                DebugUtility.LogWarning<ResourceSystem>("ResourceConfigSO não atribuído!", this);
                return;
            }
            _gameManager = GameManager.Instance;
            maxValue = config.MaxValue;
            currentValue = config.InitialValue;
            triggeredThresholds.Clear();
        }

        protected virtual void OnEnable()
        {
            _autoFillEnabled = config.AutoFillEnabled;
            _autoDrainEnabled = config.AutoDrainEnabled;
            _autoFillRate = config.AutoFillRate;
            _autoDrainRate = config.AutoDrainRate;
        }

        private void Start()
        {
            EventBus<ResourceBindEvent>.Raise(new ResourceBindEvent(gameObject, config.ResourceType, config.UniqueId, this));
            EventBus<ResourceEvent>.Raise(new ResourceEvent(config.UniqueId, gameObject, config.ResourceType, GetPercentage()));
        }

        protected virtual void Update()
        {
            AutoFill();
            ApplyModifiers();
        }

        private void ApplyModifiers()
        {
            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = modifiers[i];
                if (modifier.amountPerSecond > 0)
                    Increase(modifier.amountPerSecond * Time.deltaTime);
                else
                    Decrease(-modifier.amountPerSecond * Time.deltaTime);

                if (modifier.Update(Time.deltaTime))
                    modifiers.RemoveAt(i); // Remove modificador expirado
            }
        }

        private void AutoFill()
        {
            if (!_autoFillEnabled && !_autoDrainEnabled || !_gameManager.ShouldPlayingGame()) return;
            _autoChangeTimer += Time.deltaTime;
            if (!(_autoChangeTimer >= config.AutoChangeDelay)) return;
            if (_autoFillEnabled && currentValue < maxValue)
            {
                Increase(_autoFillRate * Time.deltaTime);
            }
            if (_autoDrainEnabled && currentValue > 0)
            {
                Decrease(_autoDrainRate * Time.deltaTime);
            }
        }

        public void SetExternalAutoDrain(bool enabledAuto, float rate)
        {
            _autoDrainEnabled = enabledAuto;
            _autoDrainRate = rate;
            _autoChangeTimer = config.AutoChangeDelay;
            DebugUtility.Log<ResourceSystem>($"🦸 AutoDrain {(enabledAuto ? "ATIVADO" : "DESATIVADO")} a {rate} por segundo no GameObject {gameObject.name} com UniqueId {config.UniqueId}");
        }

        public void SetExternalAutoFill(bool enabledAuto, float rate)
        {
            _autoFillEnabled = enabledAuto;
            _autoFillRate = rate;
            _autoChangeTimer = config.AutoChangeDelay;
            DebugUtility.Log<ResourceSystem>($"💧 AutoFill {(enabledAuto ? "ATIVADO" : "DESATIVADO")} a {rate} por segundo no GameObject {gameObject.name} com UniqueId {config.UniqueId}");
        }

        public void Increase(float amount)
        {
            if (amount < 0) return;
            currentValue = Mathf.Min(maxValue, currentValue + amount);
            float percentage = GetPercentage();
            onValueChanged?.Invoke(percentage);
            CheckThresholds();
            _autoChangeTimer = 0f;
            if (currentValue >= maxValue)
            {
                EventBus<ResourceEvent>.Raise(new ResourceEvent(config.UniqueId, gameObject, config.ResourceType, percentage));
            }
        }

        public void Decrease(float amount)
        {
            if (amount < 0) return;
            currentValue = Mathf.Max(0, currentValue - amount);
            float percentage = GetPercentage();
            onValueChanged?.Invoke((percentage));
            CheckThresholds();
            _autoChangeTimer = 0f;
            if (currentValue <= 0)
            {
                OnDepleted();
                onDepleted?.Invoke();
                EventBus<ResourceEvent>.Raise(new ResourceEvent(config.UniqueId, gameObject, config.ResourceType, percentage));
            }
        }

        protected void CheckThresholds()
        {
            float percentage = GetPercentage();
            foreach (float threshold in config.Thresholds.Where(t => percentage <= t && !triggeredThresholds.Contains(t)))
            {
                triggeredThresholds.Add(threshold);
                onThresholdReached?.Invoke(threshold);
            }
            for (int i = triggeredThresholds.Count - 1; i >= 0; i--)
            {
                if (percentage > triggeredThresholds[i])
                {
                    triggeredThresholds.RemoveAt(i);
                }
            }
        }

        protected virtual void OnDepleted()
        {
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            modifiers.Add(new ResourceModifier(amountPerSecond, duration, isPermanent));
        }

        public void RemoveAllModifiers()
        {
            modifiers.Clear();
        }

        public float GetCurrentValue() => currentValue;
        public float GetMaxValue() => maxValue;
        public float GetPercentage() => currentValue / maxValue;

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
            onValueChanged?.Invoke(GetPercentage());
            CheckThresholds();
        }

        public ResourceConfigSo Config => config;
    }

    public struct ThresholdCrossInfo
    {
        public GameObject Source { get; }
        public ResourceType Type { get; }
        public float CurrentValue { get; }
        public float Threshold { get; }
        public bool IsAscending { get; }

        public ThresholdCrossInfo(GameObject source, ResourceType type, float currentValue, float threshold, bool isAscending)
        {
            Source = source;
            Type = type;
            CurrentValue = currentValue;
            Threshold = threshold;
            IsAscending = isAscending;
        }
    }
}