using UnityEngine;
using System;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private bool _destroyOnDeath = true;
        [SerializeField] private float _destroyDelay = 2f;
        
        private float _currentHealth;
        
        // Evento para notificar sobre a morte do inimigo
        public event Action<Enemy> OnEnemyDeath;
        
        private void OnEnable()
        {
            // Reiniciar a saúde quando o inimigo for ativado
            _currentHealth = _maxHealth;
        }
        
        public void TakeDamage(float damageAmount)
        {
            // Verificar se o inimigo já está morto
            if (_currentHealth <= 0)
                return;
                
            // Aplicar dano
            _currentHealth -= damageAmount;
            
            // Verificar se o inimigo morreu
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            // Disparar evento de morte
            OnEnemyDeath?.Invoke(this);
            
            // Aqui você pode adicionar efeitos de morte, animações, etc.
            
            // Desativar componentes como Colliders, scripts de IA, etc.
            Collider[] colliders = GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Opcionalmente destruir o objeto
            if (_destroyOnDeath)
            {
                if (_destroyDelay > 0)
                {
                    Destroy(gameObject, _destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
        
        // Propriedade para acessar a saúde atual (somente leitura)
        public float CurrentHealth => _currentHealth;
        
        // Propriedade para acessar a saúde máxima (somente leitura)
        public float MaxHealth => _maxHealth;
    }
}
