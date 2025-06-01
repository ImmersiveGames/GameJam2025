using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public class ResourceThresholdEvent : UnityEvent<float> { }

    // Interface genérica para recursos

    // Classe base abstrata para gerenciamento de recursos
    public abstract class ResourceSystem : MonoBehaviour, IResource
    {
        [SerializeField] protected float maxValue = 100f; // Valor máximo configurável no Inspector
        [SerializeField] protected float currentValue; // Valor atual
        [SerializeField] protected List<float> thresholds = new List<float> { 0.75f, 0.5f, 0.25f }; // Porcentagens para eventos
        [SerializeField] public UnityEvent onDepleted; // Evento disparado quando o recurso chega a 0
        [SerializeField] public UnityEvent<float> onValueChanged; // Evento disparado quando o valor muda
        [SerializeField] public ResourceThresholdEvent onThresholdReached; // Evento para thresholds

        private readonly List<float> _triggeredThresholds = new List<float>(); // Rastreia thresholds disparados

        protected virtual void Awake()
        {
            currentValue = maxValue; // Inicializa com valor máximo
            _triggeredThresholds.Clear(); // Limpa thresholds disparados
        }

        // Aumenta o valor do recurso
        public void Increase(float amount)
        {
            if (amount < 0) return; // Evita aumento negativo

            currentValue = Mathf.Min(maxValue, currentValue + amount); // Não ultrapassa o máximo
            onValueChanged.Invoke(GetPercentage()); // Notifica mudança (em %)
            CheckThresholds(); // Verifica thresholds
        }

        // Diminui o valor do recurso
        public void Decrease(float amount)
        {
            if (amount < 0) return; // Evita redução negativa

            currentValue = Mathf.Max(0, currentValue - amount); // Não fica abaixo de 0
            onValueChanged.Invoke(GetPercentage()); // Notifica mudança (em %)
            CheckThresholds(); // Verifica thresholds

            if (!(currentValue <= 0)) return;
            OnDepleted(); // Chama método virtual para comportamento específico
            onDepleted.Invoke(); // Dispara evento de esgotamento
        }

        // Verifica se algum threshold de porcentagem foi atingido
        protected void CheckThresholds()
        {
            float percentage = GetPercentage();

            foreach (float threshold in thresholds.Where(t => percentage <= t && !_triggeredThresholds.Contains(t)))
            {
                _triggeredThresholds.Add(threshold); // Marca threshold como disparado
                onThresholdReached.Invoke(threshold); // Dispara evento
            }

            // Reativa thresholds se o valor aumentar acima deles
            for (int i = _triggeredThresholds.Count - 1; i >= 0; i--)
            {
                if (percentage > _triggeredThresholds[i])
                {
                    _triggeredThresholds.RemoveAt(i); // Permite disparo futuro
                }
            }
        }

        // Método virtual para comportamento específico ao esgotar o recurso
        protected virtual void OnDepleted()
        {
            // Pode ser sobrescrito pelas classes derivadas
        }

        // Getters para acesso externo
        public float GetCurrentValue() => currentValue;
        public float GetMaxValue() => maxValue;
        public float GetPercentage() => currentValue / maxValue;
    }
}