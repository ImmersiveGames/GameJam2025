using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
namespace _ImmersiveGames.Scripts.HealthSystems
{
    [System.Serializable]
    public class HealthThresholdEvent : UnityEvent<float> { }

    public class HealthSystem : MonoBehaviour, IDestructible
    {
        [SerializeField] private float maxHealth = 100f; // HP máximo configurável no Inspector
        [SerializeField] private float currentHealth; // HP atual
        [SerializeField] private List<float> healthThresholds = new List<float> { 0.75f, 0.5f, 0.25f }; // Porcentagens para eventos
        [SerializeField]
        public UnityEvent onDeath; // Evento disparado ao morrer
        [SerializeField]
        public UnityEvent<float> onHealthChanged; // Evento disparado quando HP muda
        [SerializeField]
        public HealthThresholdEvent onThresholdReached; // Evento para thresholds específicos
        
        private readonly List<float> _triggeredThresholds = new (); // Rastreia thresholds já disparados

        protected virtual void Awake()
        {
            currentHealth = maxHealth; // Inicializa com HP máximo
            _triggeredThresholds.Clear(); // Limpa thresholds disparados
        }

        // Aplica dano ao objeto
        public void TakeDamage(float damage)
        {
            if (damage < 0) return; // Evita dano negativo

            currentHealth = Mathf.Max(0, currentHealth - damage); // Reduz HP, não abaixo de 0
            onHealthChanged.Invoke(currentHealth / maxHealth); // Notifica mudança de HP (em %)
            CheckThresholds(); // Verifica thresholds de porcentagem

            if (currentHealth <= 0)
            {
                onDeath.Invoke(); // Dispara evento de morte
                // Opcional: Desativar ou destruir o objeto
                // gameObject.SetActive(false);
            }
        }

        // Cura o objeto
        public void Heal(float amount)
        {
            if (amount < 0) return; // Evita cura negativa

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount); // Aumenta HP, não acima do máximo
            onHealthChanged.Invoke(currentHealth / maxHealth); // Notifica mudança de HP (em %)
            CheckThresholds(); // Verifica thresholds de porcentagem
        }

        // Verifica se algum threshold de porcentagem foi atingido
        private void CheckThresholds()
        {
            float healthPercentage = currentHealth / maxHealth;

            foreach (float threshold in healthThresholds.Where(threshold => healthPercentage <= threshold && !_triggeredThresholds.Contains(threshold)))
            {
                _triggeredThresholds.Add(threshold); // Marca o threshold como disparado
                onThresholdReached.Invoke(threshold); // Dispara evento para o threshold
            }

            // Reativa thresholds se a saúde aumentar acima deles
            for (int i = _triggeredThresholds.Count - 1; i >= 0; i--)
            {
                if (healthPercentage > _triggeredThresholds[i])
                {
                    _triggeredThresholds.RemoveAt(i); // Remove threshold para permitir disparo futuro
                }
            }
        }

        // Getters para acesso externo
        public float GetCurrentHealth() => currentHealth;
        public float GetMaxHealth() => maxHealth;
        public float GetHealthPercentage() => currentHealth / maxHealth;
    }
    public interface IDestructible
    {
        void Heal(float amount);
        void TakeDamage(float damage);
    }
}