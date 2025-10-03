using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DamageReceiver : DamageSystemBase, IDamageable, IRespawnable
    {
        [Header("Damage Configuration")]
        [SerializeField] private bool canReceiveDamage = true;
        [SerializeField] private ResourceType primaryDamageResource = ResourceType.Health;
        
        [Header("Death Configuration")]
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private GameObject deathEffect;
        [SerializeField] private bool invokeEvents = true;
        [SerializeField] private bool checkHealthOnStart = true;

        [Header("Respawn Settings")]
        [SerializeField] private bool canRespawn = true;
        [SerializeField] private float respawnTime = 3f;
        [SerializeField] private Vector3 respawnPosition = Vector3.zero;
        [SerializeField] private bool useInitialPositionAsRespawn = true;
        [SerializeField] private bool deactivateOnDeath = true;

        private EntityResourceBridge _resourceBridge;
        private bool _isDead;
        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private readonly Dictionary<ResourceType, float> _initialResourceValues = new();

        // Eventos
        public event System.Action<float, IActor> OnDamageReceived;
        public event System.Action<IActor> OnDeath;
        public event System.Action<IActor> OnRevive;

        protected override void Awake()
        {
            base.Awake();
            _resourceBridge = GetComponent<EntityResourceBridge>();
            
            if (_resourceBridge == null)
            {
                DebugUtility.LogWarning<DamageReceiver>($"No EntityResourceBridge found on {name}.");
            }

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            if (invokeEvents)
            {
                _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
                EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);
            }
        }

        private void Start()
        {
            StoreInitialResourceValues();
            if (checkHealthOnStart) CheckCurrentHealth();
        }

        private void StoreInitialResourceValues()
        {
            if (_resourceBridge == null) return;

            var resourceSystem = _resourceBridge.GetService();
            foreach (var resourceEntry in resourceSystem.GetAll())
            {
                _initialResourceValues[resourceEntry.Key] = resourceEntry.Value.GetCurrentValue();
            }
        }

        public void ReceiveDamage(float damage, IActor damageSource = null, ResourceType targetResource = ResourceType.None)
        {
            if (!canReceiveDamage || _isDead) return;

            // Verificar layer da fonte do dano
            if (damageSource is MonoBehaviour sourceBehaviour && 
                !IsInDamageableLayer(sourceBehaviour.gameObject))
            {
                return;
            }

            ResourceType resourceToDamage = targetResource == ResourceType.None ? primaryDamageResource : targetResource;

            if (_resourceBridge != null)
            {
                _resourceBridge.GetService().Modify(resourceToDamage, -damage);
                CheckCurrentHealth();
            }

            OnDamageReceived?.Invoke(damage, damageSource);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (evt.ActorId == _actor?.ActorId && 
                (evt.ResourceType == primaryDamageResource || IsLinkedToPrimaryResource(evt.ResourceType)))
            {
                CheckCurrentHealth();
            }
        }

        private bool IsLinkedToPrimaryResource(ResourceType resourceType)
        {
            if (!DependencyManager.Instance.TryGetGlobal(out IResourceLinkService linkService))
                return false;

            var linkConfig = linkService.GetLink(_actor.ActorId, resourceType);
            return linkConfig != null && linkConfig.targetResource == primaryDamageResource;
        }

        private void CheckCurrentHealth()
        {
            if (_isDead || _resourceBridge == null) return;

            var healthResource = _resourceBridge.GetService().Get(primaryDamageResource);
            if (healthResource == null) return;

            if (healthResource.GetCurrentValue() <= 0f)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            if (_isDead) return;

            _isDead = true;
            canReceiveDamage = false;

            OnDeath?.Invoke(_actor);
            EventBus<ActorDeathEvent>.Raise(new ActorDeathEvent(_actor, transform.position));

            // Usa o destruction handler para spawnar efeitos
            if (deathEffect != null)
            {
                _destructionHandler.HandleEffectSpawn(deathEffect, transform.position, transform.rotation);
            }

            if (canRespawn)
            {
                if (respawnTime == 0f)
                {
                    Revive();
                }
                else if (respawnTime > 0f)
                {
                    if (deactivateOnDeath && !destroyOnDeath) gameObject.SetActive(false);
                    Invoke(nameof(ExecuteDelayedRespawn), respawnTime);
                }
                else
                {
                    FinalizeDeath();
                }
            }
            else
            {
                FinalizeDeath();
            }
        }

        private void ExecuteDelayedRespawn() => Revive();

        private void FinalizeDeath()
        {
            if (destroyOnDeath)
            {
                _destructionHandler.HandleDestruction(gameObject, false);
            }
            else if (deactivateOnDeath)
            {
                gameObject.SetActive(false);
            }
        }

        #region IRespawnable Implementation

        public void Revive(float healthAmount = -1)
        {
            if (!_isDead) return;

            CancelInvoke(nameof(ExecuteDelayedRespawn));
            _isDead = false;
            canReceiveDamage = true;

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (useInitialPositionAsRespawn)
            {
                transform.position = _initialPosition;
                transform.rotation = _initialRotation;
            }
            else
            {
                transform.position = respawnPosition;
            }

            float reviveHealth = healthAmount >= 0 ? healthAmount : GetInitialResourceValue(primaryDamageResource);
            _resourceBridge?.GetService().Set(primaryDamageResource, reviveHealth);

            OnRevive?.Invoke(_actor);
            EventBus<ActorReviveEvent>.Raise(new ActorReviveEvent(_actor, transform.position));
        }

        public void ResetToInitialState()
        {
            CancelInvoke(nameof(ExecuteDelayedRespawn));
            _isDead = false;
            canReceiveDamage = true;

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            if (_resourceBridge != null)
            {
                var resourceSystem = _resourceBridge.GetService();
                foreach (var resourceEntry in _initialResourceValues)
                {
                    resourceSystem.Set(resourceEntry.Key, resourceEntry.Value);
                }
            }

            OnRevive?.Invoke(_actor);
        }

        #endregion

        private float GetInitialResourceValue(ResourceType resourceType)
        {
            return _initialResourceValues.TryGetValue(resourceType, out float value) 
                ? value 
                : _resourceBridge?.GetService().Get(resourceType)?.GetMaxValue() ?? 100f;
        }

        protected void OnDestroy()
        {
            CancelInvoke(nameof(ExecuteDelayedRespawn));
            if (_resourceUpdateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
            }
        }

        // Properties
        public bool IsDead => _isDead;
        public bool CanReceiveDamage => canReceiveDamage && !_isDead;
        public float CurrentHealth => _resourceBridge?.GetService().Get(primaryDamageResource)?.GetCurrentValue() ?? 0f;
        public bool CanRespawn => canRespawn;

        #region Debbug Helpers

        public void SetRespawnTime(float testRespawnTime)
        {
            respawnTime = testRespawnTime;
        }

        public void SetCanRespawn(bool testCanRespawn)
        {
            canRespawn = testCanRespawn;
        }
        public void SetDeactivateOnDeath(bool testDeactivateOnDeath)
        {
            deactivateOnDeath = testDeactivateOnDeath;
        }
        public void KillImmediately()
        {
            if (_resourceBridge == null) return;
            _resourceBridge.GetService().Set(primaryDamageResource, 0f);
            CheckCurrentHealth();
        }
        
        [ContextMenu("Check Health Status")]
        internal void CheckHealthStatus()
        {
            if (_resourceBridge == null) return;
            var resourceSystem = _resourceBridge.GetService();
            var healthResource = resourceSystem.Get(primaryDamageResource);
            float health = healthResource?.GetCurrentValue() ?? 0f;
            Debug.Log($"Health Status: {health}, IsDead: {_isDead}, CanRespawn: {canRespawn}, RespawnTime: {respawnTime}");
        }
        [ContextMenu("Debug Initial Values")]
        internal void DebugInitialValues()
        {
            Debug.Log($"Initial Position: {_initialPosition}");
            Debug.Log($"Initial Rotation: {_initialRotation}");
            Debug.Log($"Respawn Settings - CanRespawn: {canRespawn}, RespawnTime: {respawnTime}, DeactivateOnDeath: {deactivateOnDeath}");
            Debug.Log("Initial Resource Values:");
            foreach (KeyValuePair<ResourceType, float> kvp in _initialResourceValues)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value}");
            }
        }
        #endregion
    }
}