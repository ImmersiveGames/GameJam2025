using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [DebugLevel(DebugLevel.Logs)]
    public sealed class DamageReceiver : DamageSystemBase, IDamageable, IRespawnable
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
        
        // Correção: Inicialização correta dos módulos
        private IRespawnHandler _respawnHandler;
        private IDeathHandler _deathHandler;
        private IDamageValidator _damageValidator;

        // Eventos (para debug)
        public event System.Action<float, IActor> EventDamageReceived;
        public event System.Action<IActor> EventDeath;
        public event System.Action<IActor> EventRevive;

        protected override void Awake()
        {
            base.Awake();
            _resourceBridge = GetComponent<EntityResourceBridge>();
            
            InitializeModules();
            InitializeEventListeners();
            StoreInitialState();
        }

        private void InitializeModules()
        {
            // Correção: Inicializar todos os módulos
            _respawnHandler = new RespawnHandler(this);
            _deathHandler = new DeathHandler(this);
            _damageValidator = new DamageValidator(this);
        }

        private void InitializeEventListeners()
        {
            if (invokeEvents && _resourceBridge != null)
            {
                _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
                EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);
            }
        }

        private void StoreInitialState()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            StoreInitialResourceValues();
        }

        private void StoreInitialResourceValues()
        {
            if (_resourceBridge == null) return;

            var resourceSystem = _resourceBridge.GetService();
            foreach (KeyValuePair<ResourceType, IResourceValue> resourceEntry in resourceSystem.GetAll())
            {
                _initialResourceValues[resourceEntry.Key] = resourceEntry.Value.GetCurrentValue();
            }
        }

        private void Start()
        {
            if (checkHealthOnStart) CheckCurrentHealth();
        }

        public void ReceiveDamage(float damage, IActor damageSource = null, ResourceType targetResource = ResourceType.None)
        {
            if (!_damageValidator.CanReceiveDamage(damage, damageSource, targetResource)) return;

            var resourceToDamage = targetResource == ResourceType.None ? primaryDamageResource : targetResource;
            ApplyDamageToResource(resourceToDamage, damage);
            
            EventDamageReceived?.Invoke(damage, damageSource);
            CheckCurrentHealth();
        }

        private void ApplyDamageToResource(ResourceType resourceType, float damage)
        {
            _resourceBridge?.GetService().Modify(resourceType, -damage);
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            if (evt.ActorId == actor?.ActorId && IsRelevantResource(evt.ResourceType))
            {
                CheckCurrentHealth();
            }
        }

        private bool IsRelevantResource(ResourceType resourceType)
        {
            return resourceType == primaryDamageResource || IsLinkedToPrimaryResource(resourceType);
        }

        private bool IsLinkedToPrimaryResource(ResourceType resourceType)
        {
            if (!DependencyManager.Instance.TryGetGlobal(out IResourceLinkService linkService))
                return false;

            var linkConfig = linkService.GetLink(actor.ActorId, resourceType);
            return linkConfig != null && linkConfig.targetResource == primaryDamageResource;
        }

        private void CheckCurrentHealth()
        {
            if (_isDead || _resourceBridge == null) return;

            var healthResource = _resourceBridge.GetService().Get(primaryDamageResource);
            if (healthResource?.GetCurrentValue() <= 0f)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            if (_isDead) return;

            _isDead = true;
            canReceiveDamage = false;

            _deathHandler.ExecuteDeath();
            _respawnHandler.HandleRespawn();
        }

        #region IRespawnable Implementation
        public void Revive(float healthAmount = -1)
        {
            _respawnHandler.Revive(healthAmount);
        }

        public void ResetToInitialState()
        {
            _respawnHandler.ResetToInitialState();
        }

        public bool IsDead => _isDead;
        public bool CanReceiveDamage => canReceiveDamage && !_isDead;
        public bool CanRespawn => canRespawn;
        public float CurrentHealth => _resourceBridge?.GetService().Get(primaryDamageResource)?.GetCurrentValue() ?? 0f;
        #endregion

        private void OnDestroy()
        {
            _respawnHandler?.CancelRespawn();
            if (_resourceUpdateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
            }
        }

        #region Internal Accessors for Modules
        internal EntityResourceBridge ResourceBridge => _resourceBridge;
        internal new IActor Actor => actor;
        internal Dictionary<ResourceType, float> InitialResourceValues => _initialResourceValues;
        internal Vector3 InitialPosition => _initialPosition;
        internal Quaternion InitialRotation => _initialRotation;
        
        internal bool DestroyOnDeath => destroyOnDeath;
        internal GameObject DeathEffect => deathEffect;
        internal bool InvokeEvents => invokeEvents;
        internal bool CanRespawnConfig => canRespawn;
        internal float RespawnTime => respawnTime;
        internal Vector3 RespawnPosition => respawnPosition;
        internal bool UseInitialPositionAsRespawn => useInitialPositionAsRespawn;
        internal bool DeactivateOnDeath => deactivateOnDeath;
        internal ResourceType PrimaryDamageResource => primaryDamageResource;
        
        internal void SetDead(bool dead) => _isDead = dead;
        internal void SetCanReceiveDamage(bool canReceive) => canReceiveDamage = canReceive;
        #endregion

        #region Respawn Integration Methods
        internal void ExecuteDelayedRespawn()
        {
            Revive();
        }

        internal void FinalizeDeath()
        {
            if (destroyOnDeath)
            {
                destructionHandler.HandleDestruction(gameObject, false);
            }
            else if (deactivateOnDeath)
            {
                gameObject.SetActive(false);
            }
        }
        #endregion

        #region Debug Methods
        [ContextMenu("Debug/Check Health Status")]
        internal void DebugCheckHealthStatus()
        {
            // Método para o debugger usar
        }

        [ContextMenu("Debug/Show Initial Values")]
        internal void DebugShowInitialValues()
        {
            // Método para o debugger usar
        }

        public void SetRespawnTime(float time) => respawnTime = time;
        public void SetCanRespawn(bool canRespawnFlag) => canRespawn = canRespawnFlag;
        public void SetDeactivateOnDeath(bool deactivateFlag) => deactivateOnDeath = deactivateFlag;

        [ContextMenu("Debug/Kill Immediately")]
        public void KillImmediately()
        {
            _resourceBridge?.GetService().Set(primaryDamageResource, 0f);
            CheckCurrentHealth();
        }
        #endregion
        public void OnEventDeath(IActor obj)
        {
            EventDeath?.Invoke(obj);
        }
        public void OnEventRevive(IActor obj)
        {
            EventRevive?.Invoke(obj);
        }
    }
}