using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using System.Collections;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;

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

        // CORREÇÃO: Remover referência ao bridge antigo
        private ResourceSystem _resourceSystem;
        private bool _isDead;
        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private readonly Dictionary<ResourceType, float> _initialResourceValues = new();
        
        // CORREÇÃO: Inicialização correta dos módulos
        private IRespawnHandler _respawnHandler;
        private IDeathHandler _deathHandler;
        private IDamageValidator _damageValidator;

        // CORREÇÃO: Flag de inicialização
        private bool _resourceSystemInitialized;
        private Coroutine _initializationCoroutine;

        // Eventos (para debug)
        public event System.Action<float, IActor> EventDamageReceived;
        public event System.Action<IActor> EventDeath;
        public event System.Action<IActor> EventRevive;

        protected override void Awake()
        {
            base.Awake();
            
            // CORREÇÃO: Inicialização assíncrona do ResourceSystem
            InitializeModules();
            StoreInitialState();
            
            // CORREÇÃO: Iniciar inicialização do ResourceSystem
            _initializationCoroutine = StartCoroutine(InitializeResourceSystemAsync());
        }

        // CORREÇÃO: Inicialização assíncrona do ResourceSystem
        private IEnumerator InitializeResourceSystemAsync()
        {
            if (actor == null)
            {
                DebugUtility.LogError<DamageReceiver>("Actor é null, não é possível inicializar DamageReceiver");
                yield break;
            }

            string actorId = actor.ActorId;
            int maxAttempts = 10;
            int attempt = 0;

            DebugUtility.LogVerbose<DamageReceiver>($"🚀 Iniciando inicialização do DamageReceiver para {actorId}");

            while (!_resourceSystemInitialized && attempt < maxAttempts)
            {
                attempt++;
                
                if (attempt > 1)
                    yield return new WaitForSeconds(0.2f);
                else
                    yield return null;

                DebugUtility.LogVerbose<DamageReceiver>($"Tentativa {attempt} de obter ResourceSystem para {actorId}");

                if (TryGetResourceSystem(actorId))
                {
                    _resourceSystemInitialized = true;
                    DebugUtility.LogVerbose<DamageReceiver>($"✅ ResourceSystem obtido com sucesso na tentativa {attempt}");
                    
                    // Inicializar event listeners após ter o ResourceSystem
                    InitializeEventListeners();
                    
                    // Verificar saúde inicial se configurado
                    if (checkHealthOnStart) 
                        CheckCurrentHealth();
                    
                    break;
                }
            }

            if (!_resourceSystemInitialized)
            {
                DebugUtility.LogError<DamageReceiver>($"❌ Falha ao obter ResourceSystem para {actorId} após {maxAttempts} tentativas");
            }
        }

        // CORREÇÃO: Método para obter ResourceSystem seguindo novo padrão
        private bool TryGetResourceSystem(string actorId)
        {
            // Tentativa 1: DependencyManager
            if (DependencyManager.Instance.TryGetForObject(actorId, out _resourceSystem))
            {
                DebugUtility.LogVerbose<DamageReceiver>("ResourceSystem obtido via DependencyManager");
                return true;
            }

            // Tentativa 2: Orchestrator
            if (DependencyManager.Instance.TryGetGlobal(out IActorResourceOrchestrator orchestrator))
            {
                _resourceSystem = orchestrator.GetActorResourceSystem(actorId);
                if (_resourceSystem != null)
                {
                    DebugUtility.LogVerbose<DamageReceiver>("ResourceSystem obtido via Orchestrator");
                    return true;
                }
            }

            // Tentativa 3: InjectableEntityResourceBridge (novo)
            var entityBridge = GetComponent<InjectableEntityResourceBridge>();
            if (entityBridge != null)
            {
                // CORREÇÃO: Usar reflection para acessar o serviço se necessário
                var serviceField = typeof(InjectableEntityResourceBridge).GetField("_service", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (serviceField != null)
                {
                    _resourceSystem = serviceField.GetValue(entityBridge) as ResourceSystem;
                    if (_resourceSystem != null)
                    {
                        DebugUtility.LogVerbose<DamageReceiver>("ResourceSystem obtido via InjectableEntityResourceBridge (reflection)");
                        return true;
                    }
                }
            }

            DebugUtility.LogVerbose<DamageReceiver>("ResourceSystem não encontrado em nenhuma fonte");
            return false;
        }

        private void InitializeModules()
        {
            // CORREÇÃO: Inicializar todos os módulos
            _respawnHandler = new RespawnHandler(this);
            _deathHandler = new DeathHandler(this);
            _damageValidator = new DamageValidator(this);
        }

        private void InitializeEventListeners()
        {
            if (invokeEvents && _resourceSystem != null)
            {
                _resourceUpdateBinding = new EventBinding<ResourceUpdateEvent>(OnResourceUpdated);
                EventBus<ResourceUpdateEvent>.Register(_resourceUpdateBinding);
                DebugUtility.LogVerbose<DamageReceiver>("Event listeners inicializados");
            }
        }

        private void StoreInitialState()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            
            // CORREÇÃO: Armazenar valores iniciais será feito quando o ResourceSystem estiver disponível
            if (_resourceSystem != null)
            {
                StoreInitialResourceValues();
            }
        }

        private void StoreInitialResourceValues()
        {
            if (_resourceSystem == null) return;

            _initialResourceValues.Clear();
            foreach (KeyValuePair<ResourceType, IResourceValue> resourceEntry in _resourceSystem.GetAll())
            {
                _initialResourceValues[resourceEntry.Key] = resourceEntry.Value.GetCurrentValue();
            }
            
            DebugUtility.LogVerbose<DamageReceiver>($"Valores iniciais armazenados para {_initialResourceValues.Count} recursos");
        }

        // CORREÇÃO: Start simplificado - a inicialização é feita via corrotina
        private void Start()
        {
            // A verificação de saúde é feita na corrotina de inicialização
        }

        public void ReceiveDamage(float damage, IActor damageSource = null, ResourceType targetResource = ResourceType.None)
        {
            // CORREÇÃO: Verificar se o sistema está inicializado
            if (!_resourceSystemInitialized)
            {
                DebugUtility.LogWarning<DamageReceiver>("Tentativa de receber dano antes da inicialização completa");
                return;
            }

            if (!_damageValidator.CanReceiveDamage(damage, damageSource, targetResource)) return;

            var resourceToDamage = targetResource == ResourceType.None ? primaryDamageResource : targetResource;
            ApplyDamageToResource(resourceToDamage, damage);
            
            EventDamageReceived?.Invoke(damage, damageSource);
            CheckCurrentHealth();
        }

        private void ApplyDamageToResource(ResourceType resourceType, float damage)
        {
            _resourceSystem?.Modify(resourceType, -damage);
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
            if (_isDead || _resourceSystem == null) return;

            var healthResource = _resourceSystem.Get(primaryDamageResource);
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
        public bool CanReceiveDamage => canReceiveDamage && !_isDead && _resourceSystemInitialized;
        public bool CanRespawn => canRespawn;
        public float CurrentHealth => _resourceSystem?.Get(primaryDamageResource)?.GetCurrentValue() ?? 0f;
        #endregion

        private void OnDestroy()
        {
            if (_initializationCoroutine != null)
            {
                StopCoroutine(_initializationCoroutine);
            }
            
            _respawnHandler?.CancelRespawn();
            if (_resourceUpdateBinding != null)
            {
                EventBus<ResourceUpdateEvent>.Unregister(_resourceUpdateBinding);
            }
        }

        #region Internal Accessors for Modules
        internal ResourceSystem ResourceSystem => _resourceSystem;
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
        internal bool ResourceSystemInitialized => _resourceSystemInitialized;
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
        [ContextMenu("🔧 Debug DamageReceiver Status")]
        public void DebugDamageReceiverStatus()
        {
            DebugUtility.LogWarning<DamageReceiver>(
                $"🔧 DAMAGE RECEIVER STATUS:\n" +
                $" - Actor: {actor?.ActorId}\n" +
                $" - ResourceSystem Initialized: {_resourceSystemInitialized}\n" +
                $" - ResourceSystem: {_resourceSystem != null}\n" +
                $" - Is Dead: {_isDead}\n" +
                $" - Can Receive Damage: {CanReceiveDamage}\n" +
                $" - Current Health: {CurrentHealth}\n" +
                $" - Primary Resource: {primaryDamageResource}"
            );

            if (_resourceSystem != null)
            {
                var health = _resourceSystem.Get(primaryDamageResource);
                if (health != null)
                {
                    DebugUtility.LogWarning<DamageReceiver>($" - Health: {health.GetCurrentValue():F1}/{health.GetMaxValue():F1}");
                }
            }
        }

        [ContextMenu("Debug/Check Health Status")]
        internal void DebugCheckHealthStatus()
        {
            CheckCurrentHealth();
            DebugDamageReceiverStatus();
        }

        [ContextMenu("Debug/Show Initial Values")]
        internal void DebugShowInitialValues()
        {
            DebugUtility.LogWarning<DamageReceiver>($"Initial Resource Values: {_initialResourceValues.Count} values");
            foreach (var kv in _initialResourceValues)
            {
                DebugUtility.LogWarning<DamageReceiver>($" - {kv.Key}: {kv.Value}");
            }
        }

        public void SetRespawnTime(float time) => respawnTime = time;
        public void SetCanRespawn(bool canRespawnFlag) => canRespawn = canRespawnFlag;
        public void SetDeactivateOnDeath(bool deactivateFlag) => deactivateOnDeath = deactivateFlag;

        [ContextMenu("💀 Kill Immediately")]
        public void KillImmediately()
        {
            if (!_resourceSystemInitialized)
            {
                DebugUtility.LogWarning<DamageReceiver>("Não é possível matar - ResourceSystem não inicializado");
                return;
            }

            _resourceSystem?.Set(primaryDamageResource, 0f);
            CheckCurrentHealth();
            DebugUtility.LogWarning<DamageReceiver>("✅ Matado imediatamente");
        }

        [ContextMenu("🔄 Force Reinitialize")]
        public void ForceReinitialize()
        {
            if (_initializationCoroutine != null)
            {
                StopCoroutine(_initializationCoroutine);
            }
            
            _resourceSystemInitialized = false;
            _resourceSystem = null;
            _initializationCoroutine = StartCoroutine(InitializeResourceSystemAsync());
            DebugUtility.LogWarning<DamageReceiver>("🔄 Reinicialização forçada iniciada");
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