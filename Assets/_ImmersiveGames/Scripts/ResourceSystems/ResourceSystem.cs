using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.ResourceSystems.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using UnityEngine.Events;
namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public class ResourceThresholdEvent : UnityEvent<float> { }

    // Classe base para gerenciamento de recursos
    public abstract class ResourceSystem : MonoBehaviour, IResource
    {
        [SerializeField] protected ResourceConfigSo config; // Configuração do recurso
        protected float maxValue; // Valor máximo do recurso
        protected float currentValue; // Valor atual do recurso
        protected List<float> thresholds; // Limiares de porcentagem
        [SerializeField] public UnityEvent onDepleted; // Evento disparado quando esgotado
        [SerializeField] public UnityEvent<float> onValueChanged; // Evento disparado quando valor muda
        [SerializeField] public ResourceThresholdEvent onThresholdReached; // Evento para limiares
        protected readonly List<float> triggeredThresholds = new List<float>(); // Limiares já disparados
        protected readonly List<ResourceModifier> _modifiers = new List<ResourceModifier>(); // Lista de modificadores
        private bool _autoFillEnabled; // Auto-preenchimento habilitado
        private bool _autoDrainEnabled; // Autodrenagem habilitada
        private float _autoFillRate; // Taxa de preenchimento automático
        private float _autoDrainRate; // Taxa de drenagem automática
        private float _autoChangeDelay; // Atraso para mudanças automáticas
        private float _autoChangeTimer = 0f; // Temporizador para mudanças automáticas

        // Inicializa o recurso com base na configuração
        protected virtual void Awake()
        {
            if (config == null)
            {
                Debug.LogWarning("ResourceConfigSO não atribuído!", this);
                return;
            }
            maxValue = config.MaxValue;
            currentValue = config.InitialValue;
            thresholds = new List<float>(config.Thresholds);
            _autoFillEnabled = config.AutoFillEnabled;
            _autoDrainEnabled = config.AutoDrainEnabled;
            _autoFillRate = config.AutoFillRate;
            _autoDrainRate = config.AutoDrainRate;
            _autoChangeDelay = config.AutoChangeDelay;
            triggeredThresholds.Clear();
        }

        // Atualiza autopreenchimento/drenagem e modificadores
        protected virtual void Update()
        {
            if (_autoFillEnabled || _autoDrainEnabled)
            {
                _autoChangeTimer += Time.deltaTime;
                if (_autoChangeTimer >= _autoChangeDelay)
                {
                    if (_autoFillEnabled && currentValue < maxValue)
                    {
                        Increase(_autoFillRate * Time.deltaTime);
                    }
                    if (_autoDrainEnabled && currentValue > 0)
                    {
                        Decrease(_autoDrainRate * Time.deltaTime);
                    }
                }
            }

            // Aplica modificadores (buffs/debuffs)
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var modifier = _modifiers[i];
                if (modifier.AmountPerSecond > 0)
                    Increase(modifier.AmountPerSecond * Time.deltaTime);
                else
                    Decrease(-modifier.AmountPerSecond * Time.deltaTime);

                if (modifier.Update(Time.deltaTime))
                    _modifiers.RemoveAt(i); // Remove modificador expirado
            }
        }

        // Aumenta o valor do recurso
        public void Increase(float amount)
        {
            if (amount < 0) return;
            currentValue = Mathf.Min(maxValue, currentValue + amount);
            float percentage = GetPercentage();
            onValueChanged.Invoke(percentage);
            CheckThresholds();
            _autoChangeTimer = 0f; // Reseta temporizador ao mudar valor
            if (currentValue >= maxValue)
                EventBus<ResourceEvent>.Raise(new ResourceEvent(gameObject, config.ResourceType, percentage));
        }

        // Diminui o valor do recurso
        public void Decrease(float amount)
        {
            if (amount < 0) return;
            currentValue = Mathf.Max(0, currentValue - amount);
            float percentage = GetPercentage();
            onValueChanged.Invoke(percentage);
            CheckThresholds();
            _autoChangeTimer = 0f; // Reseta temporizador ao mudar valor
            if (currentValue <= 0)
            {
                OnDepleted();
                onDepleted.Invoke();
                EventBus<ResourceEvent>.Raise(new ResourceEvent(gameObject, config.ResourceType, percentage));
            }
        }

        // Verifica se limiares foram atingidos
        protected void CheckThresholds()
        {
            float percentage = GetPercentage();
            foreach (float threshold in thresholds.Where(t => percentage <= t && !triggeredThresholds.Contains(t)))
            {
                triggeredThresholds.Add(threshold);
                onThresholdReached.Invoke(threshold);
            }
            for (int i = triggeredThresholds.Count - 1; i >= 0; i--)
            {
                if (percentage > triggeredThresholds[i])
                {
                    triggeredThresholds.RemoveAt(i); // Permite disparo futuro
                }
            }
        }

        // Método virtual para comportamento ao esgotar
        protected virtual void OnDepleted()
        {
            // Sobrescrito por classes derivadas
        }

        // Adiciona um modificador (buff/debuff)
        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false)
        {
            _modifiers.Add(new ResourceModifier(amountPerSecond, duration, isPermanent));
        }

        // Remove todos os modificadores
        public void RemoveAllModifiers()
        {
            _modifiers.Clear();
        }

        // Getters para acesso externo
        public float GetCurrentValue() => currentValue;
        public float GetMaxValue() => maxValue;
        public float GetPercentage() => currentValue / maxValue;

        // Dados para serialização
        [System.Serializable]
        public class ResourceSaveData
        {
            public float CurrentValue; // Valor atual salvo
            public List<float> TriggeredThresholds; // Limiares disparados salvos
        }

        // Salva o estado do recurso
        public ResourceSaveData Save()
        {
            return new ResourceSaveData
            {
                CurrentValue = currentValue,
                TriggeredThresholds = new List<float>(triggeredThresholds)
            };
        }

        // Carrega o estado do recurso
        public void Load(ResourceSaveData data)
        {
            currentValue = data.CurrentValue;
            triggeredThresholds.Clear();
            triggeredThresholds.AddRange(data.TriggeredThresholds);
            onValueChanged.Invoke(GetPercentage());
            CheckThresholds();
        }

        // Propriedade para acessar a configuração
        public ResourceConfigSo Config => config;
    }
}