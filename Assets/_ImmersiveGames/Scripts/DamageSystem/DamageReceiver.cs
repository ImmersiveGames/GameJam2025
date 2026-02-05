using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem.Commands;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bind;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.Scripts.AudioSystem.Components;
using _ImmersiveGames.Scripts.AudioSystem.System;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Recurso alvo (ex: Health)")]
        [SerializeField] private RuntimeAttributeType targetRuntimeAttribute = RuntimeAttributeType.Health;
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
        [SerializeField] private bool destroyGameObjectIfNoPool;

        [Header("Estratégias de Dano (executadas em sequência)")]
        [SerializeField] private List<DamageStrategySelection> strategyPipeline = new()
        {
            new DamageStrategySelection()
        };

        private IActor _actor;
        private RuntimeAttributeController _component;
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
            _component = GetComponent<RuntimeAttributeController>();
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
                targetRuntimeAttribute,
                _component,
                _strategy,
                _cooldowns);

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
        /// Garante que o observador de ciclo de vida esteja conectado ao RuntimeAttributeContext da entidade.
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
                targetRuntimeAttribute,
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

            _waitingForLifecycleBinding = !_lifecycleHandler.TryAttach(system);
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

        private DamageContext CreateLifecycleDamageContext(RuntimeAttributeChangeContext change)
        {
            if (_actor == null)
            {
                return null;
            }

            if (change is { IsDecrease: false, NewValue: > 0f })
            {
                return null;
            }

            float damageValue = Mathf.Abs(change.Delta);
            string attackerId = ResolveLifecycleAttackerId(change.Source);
            return new DamageContext(attackerId, _actor.ActorId, damageValue, targetRuntimeAttribute, DamageType.Pure);
        }

        private string ResolveLifecycleAttackerId(RuntimeAttributeChangeSource source)
        {
            return source switch
            {
                RuntimeAttributeChangeSource.AutoFlow => $"{_actor?.ActorId ?? string.Empty}_AutoFlow",
                RuntimeAttributeChangeSource.Link => $"{_actor?.ActorId ?? string.Empty}_Link",
                RuntimeAttributeChangeSource.External => $"{_actor?.ActorId ?? string.Empty}_External",
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

            var soundFlags = GetSoundFlags();

            PlayHitSoundIfApplicable(notification, soundFlags);

            if (!notification.DeathStateChanged)
            {
                return;
            }

            HandleDeathOrRevive(notification, soundFlags);
        }

        private (bool hasHit, bool hasDeath, bool hasRevive) GetSoundFlags()
        {
            bool hasHitSound = hitSound != null && hitSound.clip != null;
            bool hasDeathSound = deathSound != null && deathSound.clip != null;
            bool hasReviveSound = reviveSound != null && reviveSound.clip != null;
            return (hasHitSound, hasDeathSound, hasReviveSound);
        }

        private void PlayHitSoundIfApplicable(DamageLifecycleNotification notification, (bool hasHit, bool hasDeath, bool hasRevive) flags)
        {
            if (!flags.hasHit || !notification.IsDamage)
            {
                return;
            }

            var request = notification.Request;
            var hitPosition = request is { hasHitPosition: true }
                ? request.hitPosition
                : transform.position;

            var ctx = AudioContext.Default(hitPosition, audioEmitter.UsesSpatialBlend);
            audioEmitter.Play(hitSound, ctx);
        }

        private void HandleDeathOrRevive(DamageLifecycleNotification notification, (bool hasHit, bool hasDeath, bool hasRevive) flags)
        {
            var center = transform.position;
            var deathCtx = AudioContext.Default(center, audioEmitter.UsesSpatialBlend);

            if (notification.IsDead)
            {
                if (flags.hasDeath)
                {
                    audioEmitter.Play(deathSound, deathCtx);
                }

                ExecuteDeathReturn();
            }
            else if (flags.hasRevive)
            {
                audioEmitter.Play(reviveSound, deathCtx);
            }
        }

        private void HandleDamageWithoutResource(DamageContext ctx)
        {
            if (!IsValidNonResourceDamage(ctx))
            {
                return;
            }

            PlayNonResourceDamageSound(ctx);
            TriggerExplosionIfConfigured(ctx);
            ExecuteDeathReturn();
        }

        private bool IsValidNonResourceDamage(DamageContext ctx)
        {
            if (ctx == null)
            {
                return false;
            }

            if (_cooldowns != null && !string.IsNullOrEmpty(ctx.attackerId))
            {
                if (!_cooldowns.CanDealDamage(ctx.attackerId, _receiverId))
                {
                    return false;
                }
            }

            return true;
        }

        private void PlayNonResourceDamageSound(DamageContext ctx)
        {
            if (audioEmitter == null)
            {
                return;
            }

            var sound = SelectDamageSound();
            if (sound == null)
            {
                return;
            }

            var position = ctx.hasHitPosition ? ctx.hitPosition : transform.position;
            var audioCtx = AudioContext.Default(position, audioEmitter.UsesSpatialBlend);
            audioEmitter.Play(sound, audioCtx);
        }

        private SoundData SelectDamageSound()
        {
            bool hasDeathSound = deathSound != null && deathSound.clip != null;
            bool hasHitSound = hitSound != null && hitSound.clip != null;
            return hasDeathSound ? deathSound : (hasHitSound ? hitSound : null);
        }

        private void TriggerExplosionIfConfigured(DamageContext ctx)
        {
            if (!spawnExplosionOnDeath)
            {
                return;
            }

            _explosion?.Initialize();
            _explosion?.PlayExplosion(ctx);
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

