using System;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.ScriptableObjects;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EnemySystem
{
    public abstract class DestructibleObject : MonoBehaviour, IDamageable
    {
        [SerializeField] protected DestructibleObjectSo destructibleObject;
        private float _defense;
        private bool _destroyOnDeath = true;
        private float _destroyDelay = 2f;

        public event Action<DestructibleObject> OnDeath;

        public virtual void Initialize()
        {
            if (destructibleObject == null)
            {
                Debug.LogError($"DestructibleObjectSo não está definido em {gameObject.name}.");
                return;
            }

            CurrentHealth = destructibleObject.maxHealth;
            _defense = destructibleObject.defense;
            _destroyOnDeath = destructibleObject.canDestroy;
            _destroyDelay = destructibleObject.deathDelay;
        }

        public virtual void TakeDamage(float damageAmount)
        {
            if (CurrentHealth <= 0) return;

            float finalDamage = Mathf.Max(0, damageAmount - _defense);
            CurrentHealth -= finalDamage;

            OnDamageTaken();

            if (CurrentHealth <= 0)
            {
                Die();
                DebugUtility.LogVerbose<Planets>($"O planeta foi destruído", "green");
            }
            DebugUtility.LogVerbose<Planets>($"recebeu {finalDamage} de dano. Vida atual: {CurrentHealth}", "green");
        }

        public bool IsAlive => CurrentHealth > 0;

        protected virtual void OnDamageTaken() { }

        protected virtual void Die()
        {
            OnDeath?.Invoke(this);
            if (_destroyOnDeath)
            {
                Destroy(gameObject, _destroyDelay);
            }
        }

        public float CurrentHealth { get; protected set; } = 100f;
        public float MaxHealth => destructibleObject.maxHealth;
    }
}