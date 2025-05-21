using UnityEngine;
using System;
using _ImmersiveGames.Scripts.ScriptableObjects;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public abstract class DestructibleObject : MonoBehaviour
    {
        [SerializeField] protected DestructibleObjectSo destructibleObject;
        protected float currentHealth;
        private float _defense;
        
        public event Action<DestructibleObject> OnDeath;
        
        public virtual void Initialize()
        {
            currentHealth = destructibleObject.maxHealth;
            _defense = destructibleObject.defense;
        }

        public virtual void TakeDamage(float damageAmount)
        {
            if (currentHealth <= 0) return;

            float finalDamage = Mathf.Max(0, damageAmount - _defense);
            currentHealth -= finalDamage;

            OnDamageTaken();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void OnDamageTaken()
        {
            // Para ser sobrescrito nas classes filhas
        }

        protected virtual void Die()
        {
            OnDeath?.Invoke(this);

            if (destructibleObject.destroyOnDeath)
            {
                Destroy(gameObject, destructibleObject.destroyDelay);
            }
        }

        public float CurrentHealth => currentHealth;
        public float MaxHealth => destructibleObject.maxHealth;
    }
}