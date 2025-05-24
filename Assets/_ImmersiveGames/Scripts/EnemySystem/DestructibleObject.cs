using System;
using _ImmersiveGames.Scripts.PoolSystemOld;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.EnemySystem
{
    public abstract class DestructibleObject : MonoBehaviour, IDamageable
    {
        [SerializeField] protected DestructibleObjectSo destructibleObject;
        private float _defense;
        private bool _destroyOnDeath = false; // Desativado para pooling
        private float _destroyDelay = 2f;

        public event Action<DestructibleObject> OnDeath;

        public virtual void Initialize()
        {
            if (destructibleObject == null)
            {
                DebugUtility.LogError(GetType(), $"DestructibleObjectSo não está definido em {gameObject.name}.", this);
                return;
            }

            CurrentHealth = destructibleObject.maxHealth;
            _defense = destructibleObject.defense;
            _destroyOnDeath = false; // Forçar desativação em vez de destruição
            _destroyDelay = destructibleObject.deathDelay;
        }

        public virtual void TakeDamage(float damageAmount)
        {
            if (!IsAlive) return;

            float finalDamage = Mathf.Max(0, damageAmount - _defense);
            CurrentHealth -= finalDamage;

            OnDamageTaken();

            if (!IsAlive)
            {
                Die();
            }
        }

        public bool IsAlive => CurrentHealth > 0;

        protected virtual void OnDamageTaken() { }

        protected virtual void Die()
        {
            OnDeath?.Invoke(this);
            ReturnToPool(); // Tentar retornar ao pool
        }

        protected virtual void ReturnToPool()
        {
            var pooledObj = GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.ReturnSelfToPool();
            }
            else
            {
                gameObject.SetActive(false);
                DebugUtility.LogWarning(GetType(), $"Objeto {gameObject.name} não está em um pool, desativado.", this);
            }
        }

        public virtual void ResetState()
        {
            CurrentHealth = destructibleObject?.maxHealth ?? 100f;
        }

        public float CurrentHealth { get; protected set; } = 100f;
        public float MaxHealth => destructibleObject?.maxHealth ?? 100f;
    }
}