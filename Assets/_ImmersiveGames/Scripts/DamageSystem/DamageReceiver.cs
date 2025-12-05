using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem.Commands;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Recurso alvo (ex: Health)")]
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private float damageCooldown = 0.25f;

        [Header("Explosão ao morrer")]
        [SerializeField] private bool spawnExplosionOnDeath = true;
        [SerializeField] private PoolData explosionPoolData;
        [SerializeField] private Vector3 explosionOffset = Vector3.zero;

        [Header("Ciclo de Vida")]
        [SerializeField]
        [Tooltip("Quando verdadeiro, desativa a skin do ator imediatamente após o DeathEvent.")]
        private bool disableSkinOnDeath = true;
        [SerializeField]
        [Tooltip("Quando verdadeiro, dispara GameOver ao detectar a morte deste ator.")]
        private bool triggerGameOverOnDeath;

        [Header("Pooling / Destruição")]
        [SerializeField] private bool returnToPoolOnDeath = true;
        [SerializeField] private bool destroyGameObjectIfNoPool = false;

        [Header("Estratégias de Dano (executadas em sequência)")]
        [SerializeField] private List<DamageStrategySelection> strategyPipeline = new()
        {
            new DamageStrategySelection()
        };

        private IActor _actor;
        private ActorResourceComponent _component;
        private DamageCooldownModule _cooldowns;
        private DamageLifecycleModule _lifecycle;
        private DamageExplosionModule _explosion;
        private IDamageStrategy _strategy;
        private DamageCommandInvoker _commandInvoker;
        private DamageReceiverLifecycleHandler _lifecycleHandler;
        private bool _waitingForLifecycleBinding;
        private string _receiverId;
        private IPoolable _poolable;

        [Header("Audio")]
        [SerializeField] private EntityAudioEmitter audioEmitter;
        [SerializeField] private SoundData hitSound;
        [SerializeField] private SoundData deathSound;
        [SerializeField] private SoundData reviveSound;

        [Inject] private IGameManager _gameManager;

        private void Awake()
        {
            DependencyManager.Provider.InjectDependencies(this);

            _actor = GetComponent<IActor>();
            _receiverId = _actor != null
                ? _actor.ActorId
                : $"DamageReceiver_{gameObject.GetInstanceID()}";
            _component = GetComponent<ActorResourceComponent>();
            _poolable = GetComponent<IPoolable>();
            _cooldowns = new DamageCooldownModule(damageCooldown);
            _lifecycle = new DamageLifecycleModule(_receiverId)
            {
                DisableSkinOnDeath = disableSkinOnDeath
            };
            _explosion = new DamageExplosionModule(transform, explosionPoolData, explosionOffset);
            SyncLifecycleOptions();
            audioEmitter ??= GetComponent<EntityAudioEmitter>();

            EnsureStrategyConfiguration();
            BuildStrategy();
            BuildCommandPipeline();

            if (Application.isPlaying)
            {
                EnsureLifecycleHandler();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureStrategyConfiguration();
            BuildStrategy();
            BuildCommandPipeline();
            _explosion = new DamageExplosionModule(transform, explosionPoolData, explosionOffset);
            audioEmitter ??= GetComponent<EntityAudioEmitter>();
            SyncLifecycleOptions();
        }
#endif

        private void Start()
        {
            if (Application.isPlaying && spawnExplosionOnDeath)
            {
                _explosion.Initialize();
            }

            if (Application.isPlaying)
            {
                EnsureLifecycleHandler();
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EnsureLifecycleHandler();
        }

        private void OnDisable()
        {
            DisposeLifecycleHandler();
        }

        private void OnDestroy()
        {
            DisposeLifecycleHandler();
        }

        private void Update()
        {
            if (!Application.isPlaying || !_waitingForLifecycleBinding)
            {
                return;
            }

            EnsureLifecycleHandler();
        }

        private void EnsureStrategyConfiguration()
        {
            if (strategyPipeline == null || strategyPipeline.Count == 0)
            {
                strategyPipeline = new List<DamageStrategySelection>
                {
                    new()
                };
            }

            for (int i = 0; i < strategyPipeline.Count; i++)
            {
                var selection = strategyPipeline[i] ?? new DamageStrategySelection();
                selection.EnsureInitialized();
                strategyPipeline[i] = selection;
            }
        }

        private void BuildStrategy()
        {
            _strategy = DamageStrategyFactory.CreatePipeline(strategyPipeline);
        }

        private void BuildCommandPipeline()
        {
            _commandInvoker = new DamageCommandInvoker(new IDamageCommand[]
            {
                new ResolveResourceSystemCommand(),
                new DamageCooldownCommand(),
                new CalculateDamageCommand(),
                new ApplyDamageCommand(),
                new RaiseDamageEventsCommand()
            });
        }

        public void ReceiveDamage(DamageContext ctx)
        {
            if (ctx == null || _commandInvoker == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                EnsureLifecycleHandler();

                var resourceSystem = _component != null ? _component.GetResourceSystem() : null;
                if (resourceSystem == null)
                {
                    HandleDamageWithoutResource(ctx);
                    return;
                }
            }

            var context = new DamageCommandContext(
                ctx,
                targetResource,
                _component,
                _strategy,
                _cooldowns,
                _lifecycle,
                spawnExplosionOnDeath ? _explosion : null);

            try
            {
                _lifecycleHandler?.BeginPipeline(ctx);
                _commandInvoker.Execute(context);
            }
            finally
            {
                _lifecycleHandler?.EndPipeline();
            }
        }

        public string GetReceiverId() => _receiverId;

        public void UndoLastDamage()
        {
            _commandInvoker?.UndoLast();
        }

        /// <summary>
        /// Garante que o observador de ciclo de vida esteja conectado ao ResourceSystem da entidade.
        /// </summary>
        private void EnsureLifecycleHandler()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_component == null)
            {
                return;
            }

            SyncLifecycleOptions();

            _lifecycleHandler ??= new DamageReceiverLifecycleHandler(
                targetResource,
                _lifecycle,
                spawnExplosionOnDeath ? _explosion : null,
                CreateLifecycleDamageContext,
                HandleLifecycleNotification);

            var system = _component.GetResourceSystem();
            if (system == null)
            {
                _waitingForLifecycleBinding = true;
                return;
            }

            if (_lifecycleHandler.TryAttach(system))
            {
                _waitingForLifecycleBinding = false;
            }
            else
            {
                _waitingForLifecycleBinding = true;
            }
        }

        private void SyncLifecycleOptions()
        {
            if (_lifecycle != null)
            {
                _lifecycle.DisableSkinOnDeath = disableSkinOnDeath;
                _lifecycle.TriggerGameOverOnDeath = triggerGameOverOnDeath;
            }
        }

        private void DisposeLifecycleHandler()
        {
            _lifecycleHandler?.Dispose();
            _lifecycleHandler = null;
            _waitingForLifecycleBinding = false;
        }

        private DamageContext CreateLifecycleDamageContext(ResourceChangeContext change)
        {
            if (_actor == null)
            {
                return null;
            }

            if (!change.IsDecrease && change.NewValue > 0f)
            {
                return null;
            }

            float damageValue = Mathf.Abs(change.Delta);
            var attackerId = ResolveLifecycleAttackerId(change.Source);
            return new DamageContext(attackerId, _actor.ActorId, damageValue, targetResource, DamageType.Pure);
        }

        private string ResolveLifecycleAttackerId(ResourceChangeSource source)
        {
            return source switch
            {
                ResourceChangeSource.AutoFlow => $"{_actor?.ActorId ?? string.Empty}_AutoFlow",
                ResourceChangeSource.Link => $"{_actor?.ActorId ?? string.Empty}_Link",
                ResourceChangeSource.External => $"{_actor?.ActorId ?? string.Empty}_External",
                _ => string.Empty
            };
        }

        private void HandleLifecycleNotification(DamageLifecycleNotification notification)
        {
            TryRaiseGameOver(notification);

            if (audioEmitter == null)
            {
                return;
            }

            bool hasHitSound = hitSound != null && hitSound.clip != null;
            bool hasDeathSound = deathSound != null && deathSound.clip != null;
            bool hasReviveSound = reviveSound != null && reviveSound.clip != null;

            if (hasHitSound && notification.IsDamage)
            {
                var request = notification.Request;
                var hitPosition = request != null && request.HasHitPosition
                    ? request.HitPosition
                    : transform.position;
                var ctx = AudioContext.Default(hitPosition, audioEmitter.UsesSpatialBlend);
                audioEmitter.Play(hitSound, ctx);
            }

            if (!notification.DeathStateChanged)
            {
                return;
            }

            var center = transform.position;
            var deathCtx = AudioContext.Default(center, audioEmitter.UsesSpatialBlend);

            if (notification.IsDead)
            {
                if (hasDeathSound)
                {
                    audioEmitter.Play(deathSound, deathCtx);
                }

                ExecuteDeathReturn();
            }
            else if (hasReviveSound)
            {
                audioEmitter.Play(reviveSound, deathCtx);
            }
        }

        private void HandleDamageWithoutResource(DamageContext ctx)
        {
            if (ctx == null)
            {
                return;
            }

            if (_cooldowns != null && !string.IsNullOrEmpty(ctx.AttackerId))
            {
                if (!_cooldowns.CanDealDamage(ctx.AttackerId, _receiverId))
                {
                    return;
                }
            }

            if (audioEmitter != null)
            {
                var hasDeathSound = deathSound != null && deathSound.clip != null;
                var hasHitSound = hitSound != null && hitSound.clip != null;
                var sound = hasDeathSound ? deathSound : hasHitSound ? hitSound : null;

                if (sound != null)
                {
                    var position = ctx.HasHitPosition ? ctx.HitPosition : transform.position;
                    var audioCtx = AudioContext.Default(position, audioEmitter.UsesSpatialBlend);
                    audioEmitter.Play(sound, audioCtx);
                }
            }

            if (spawnExplosionOnDeath)
            {
                _explosion?.Initialize();
                _explosion?.PlayExplosion(ctx);
            }

            ExecuteDeathReturn();
        }

        private void ExecuteDeathReturn()
        {
            if (returnToPoolOnDeath && _poolable != null)
            {
                _poolable.Deactivate();
                return;
            }

            if (destroyGameObjectIfNoPool)
            {
                gameObject.SetActive(false);
            }
        }

        private void TryRaiseGameOver(DamageLifecycleNotification notification)
        {
            if (_actor == null)
            {
                return;
            }

            if (!triggerGameOverOnDeath || !notification.DeathStateChanged || !notification.IsDead)
            {
                return;
            }

            var manager = _gameManager ?? GameManager.Instance;
            if (manager == null)
            {
                return;
            }

            manager.TryTriggerGameOver("Death event");
        }

    }
}
