using _ImmersiveGames.Scripts.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EnemySystem
{
    public class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        private float _currentHealth;

        public bool IsAlive => _currentHealth > 0;

        private void Awake()
        {
            _currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            _currentHealth -= damage;
            Debug.Log($"Inimigo {gameObject.name} recebeu {damage} de dano. Vida atual: {_currentHealth}");

            if (_currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"Inimigo {gameObject.name} destruído.");
            Destroy(gameObject);
        }
    }
}