using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Services;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class DamageReceiver : DamageSystemBase, IDamageable, IRespawnable
    {
        [Header("Damage Configuration")]
        [SerializeField] private bool canReceiveDamage = true;
        [SerializeField] private ResourceType primaryDamageResource = ResourceType.Health;

        [Header("Death Configuration")]
        [SerializeField]
        internal bool destroyOnDeath;
        [SerializeField] private GameObject deathEffect;
        [SerializeField] private bool invokeEvents = true;
        [SerializeField] private bool checkHealthOnStart = true;

        [Header("Respawn Settings")]
        [SerializeField] private bool canRespawn = true;
        [SerializeField]
        internal float respawnTime = 3f;
        [SerializeField] private Vector3 respawnPosition = Vector3.zero;
        [SerializeField] private bool useInitialPositionAsRespawn = true;
        [SerializeField]
        internal bool deactivateOnDeath = true;

        private EntityResourceBridge _resourceBridge;
        private bool _isDead;
        private EventBinding<ResourceUpdateEvent> _resourceUpdateBinding;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private readonly Dictionary<ResourceType, float> _initialResourceValues = new();
        private readonly IRespawnStrategy _respawnStrategy = new DelayedRespawnStrategy(); // Novo: Strategy injetada
        

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
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Awake concluído. CanReceiveDamage: {canReceiveDamage}, PrimaryResource: {primaryDamageResource}");
        }

        private void Start()
        {
            StoreInitialResourceValues();
            if (checkHealthOnStart) CheckCurrentHealth();
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Start concluído. Initial Health: {CurrentHealth}");
        }

        private void StoreInitialResourceValues()
        {
            if (_resourceBridge == null) return;

            var resourceSystem = _resourceBridge.GetService();
            foreach (var resourceEntry in resourceSystem.GetAll())
            {
                _initialResourceValues[resourceEntry.Key] = resourceEntry.Value.GetCurrentValue();
            }
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Valores iniciais armazenados: {string.Join(", ", _initialResourceValues.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
        }

        public void ReceiveDamage(float damage, IActor damageSource = null, ResourceType targetResource = ResourceType.None)
        {
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] ReceiveDamage chamado: {damage} de {damageSource?.ActorName ?? "unknown"}, Resource: {targetResource}");
            if (!canReceiveDamage || _isDead)
            {
                DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Dano ignorado: CanReceive={canReceiveDamage}, IsDead={_isDead}");
                return;
            }

            if (damageSource is MonoBehaviour sourceBehaviour &&
                !IsInDamageableLayer(sourceBehaviour.gameObject))
            {
                DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Fonte não em layer válida: {sourceBehaviour.gameObject.name}");
                return;
            }

            var resourceToDamage = targetResource == ResourceType.None ? primaryDamageResource : targetResource;

            if (_resourceBridge != null)
            {
                float before = _resourceBridge.GetService().Get(resourceToDamage)?.GetCurrentValue() ?? 0f;
                _resourceBridge.GetService().Modify(resourceToDamage, -damage);
                float after = _resourceBridge.GetService().Get(resourceToDamage)?.GetCurrentValue() ?? 0f;
                DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Recurso {resourceToDamage} modificado: {before} -> {after}");
                CheckCurrentHealth();
            }
            else
            {
                DebugUtility.LogWarning<DamageReceiver>($"ReceiveDamage: no ResourceBridge on {name}, damage not applied.");
            }

            OnDamageReceived?.Invoke(damage, damageSource);
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Evento OnDamageReceived invocado");
        }

        private void OnResourceUpdated(ResourceUpdateEvent evt)
        {
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] OnResourceUpdated: ActorId={evt.ActorId}, Type={evt.ResourceType}, NewValue={evt.NewValue.GetCurrentValue()}");
            if (evt.ActorId == actor?.ActorId &&
                (evt.ResourceType == primaryDamageResource || IsLinkedToPrimaryResource(evt.ResourceType)))
            {
                CheckCurrentHealth();
            }
        }

        private bool IsLinkedToPrimaryResource(ResourceType resourceType)
        {
            if (!DependencyManager.Instance.TryGetGlobal(out IResourceLinkService linkService))
                return false;

            var linkConfig = linkService.GetLink(actor.ActorId, resourceType);
            bool linked = linkConfig != null && linkConfig.targetResource == primaryDamageResource;
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Link check para {resourceType}: {linked}");
            return linked;
        }

        private void CheckCurrentHealth()
        {
            if (_isDead || _resourceBridge == null) return;

            var healthResource = _resourceBridge.GetService().Get(primaryDamageResource);
            if (healthResource == null) return;

            float current = healthResource.GetCurrentValue();
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] CheckHealth: Current={current}, Max={healthResource.GetMaxValue()}");
            if (current <= 0f)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            if (_isDead) return;

            _isDead = true;
            canReceiveDamage = false;
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Morte detectada, estado atualizado");

            OnDeath?.Invoke(actor);
            EventBus<ActorDeathEvent>.Raise(new ActorDeathEvent(actor, transform.position));
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Eventos de morte invocados");

            if (deathEffect != null)
            {
                destructionHandler.HandleEffectSpawn(deathEffect, transform.position, transform.rotation);
                DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Efeito de morte spawnado");
            }

            HandleRespawnLogic();
        }

        private void HandleRespawnLogic()
        {
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Lógica de respawn: CanRespawn={canRespawn}, Time={respawnTime}");
            if (!canRespawn)
            {
                FinalizeDeath();
                return;
            }
            _respawnStrategy.Execute(this); // Usar strategy
        }


        internal void ExecuteDelayedRespawn() => Revive();

        internal void FinalizeDeath()
        {
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Finalizando morte: Destroy={destroyOnDeath}, Deactivate={deactivateOnDeath}");
            if (destroyOnDeath)
            {
                destructionHandler.HandleDestruction(gameObject, false);
            }
            else if (deactivateOnDeath)
            {
                gameObject.SetActive(false);
            }
        }

        #region IRespawnable

        public void Revive(float healthAmount = -1)
        {
            if (!_isDead) return;

            CancelInvoke(nameof(ExecuteDelayedRespawn));
            _isDead = false;
            canReceiveDamage = true;
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Revive iniciado, estado atualizado");

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            transform.position = useInitialPositionAsRespawn ? _initialPosition : respawnPosition;
            transform.rotation = _initialRotation;
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Posição restaurada: {transform.position}");

            float reviveHealth = healthAmount >= 0 ? healthAmount : GetInitialResourceValue(primaryDamageResource);
            _resourceBridge?.GetService().Set(primaryDamageResource, reviveHealth);
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Saúde restaurada para {reviveHealth}");

            OnRevive?.Invoke(actor);
            EventBus<ActorReviveEvent>.Raise(new ActorReviveEvent(actor, transform.position));
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Eventos de revive invocados");
        }

        public void ResetToInitialState()
        {
            CancelInvoke(nameof(ExecuteDelayedRespawn));
            _isDead = false;
            canReceiveDamage = true;

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Reset posição: {transform.position}");

            if (_resourceBridge != null)
            {
                var resourceSystem = _resourceBridge.GetService();
                foreach (var resourceEntry in _initialResourceValues)
                {
                    resourceSystem.Set(resourceEntry.Key, resourceEntry.Value);
                    DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Reset {resourceEntry.Key}: {resourceEntry.Value}");
                }
            }

            OnRevive?.Invoke(actor);
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] Reset concluído");
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
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] OnDestroy chamado");
        }

        // Properties
        public bool IsDead => _isDead;
        public bool CanReceiveDamage => canReceiveDamage && !_isDead;
        public float CurrentHealth => _resourceBridge?.GetService().Get(primaryDamageResource)?.GetCurrentValue() ?? 0f;
        public bool CanRespawn => canRespawn;

        #region Debug Helpers

        public void SetRespawnTime(float testRespawnTime) => respawnTime = testRespawnTime;
        public void SetCanRespawn(bool testCanRespawn) => canRespawn = testCanRespawn;
        public void SetDeactivateOnDeath(bool testDeactivateOnDeath) => deactivateOnDeath = testDeactivateOnDeath;

        public void KillImmediately()
        {
            if (_resourceBridge == null) return;
            DebugUtility.LogVerbose<DamageReceiver>($"[Receiver {gameObject.name}] KillImmediately chamado");
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
            DebugUtility.LogVerbose<DamageReceiver>($"Health Status: {health}, IsDead: {_isDead}, CanRespawn: {canRespawn}, RespawnTime: {respawnTime}");
        }

        [ContextMenu("Debug Initial Values")]
        internal void DebugInitialValues()
        {
            DebugUtility.LogVerbose<DamageReceiver>($"Initial Position: {_initialPosition}");
            DebugUtility.LogVerbose<DamageReceiver>($"Initial Rotation: {_initialRotation}");
            DebugUtility.LogVerbose<DamageReceiver>($"Respawn Settings - CanRespawn: {canRespawn}, RespawnTime: {respawnTime}, DeactivateOnDeath: {deactivateOnDeath}");
            DebugUtility.LogVerbose<DamageReceiver>("Initial Resource Values:");
            foreach (var kvp in _initialResourceValues)
            {
                DebugUtility.LogVerbose<DamageReceiver>($"  {kvp.Key}: {kvp.Value}");
            }
        }

        #endregion
    }
}