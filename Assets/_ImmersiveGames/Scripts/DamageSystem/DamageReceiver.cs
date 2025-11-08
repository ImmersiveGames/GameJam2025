using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.DamageSystem.Commands;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [RequireComponent(typeof(IActor))]
    public class DamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Recurso alvo (ex: Health)")]
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private float damageCooldown = 0.25f;

        [Header("Explosão ao morrer")]
        [SerializeField] private bool spawnExplosionOnDeath = true;
        [SerializeField] private PoolData explosionPoolData;
        [SerializeField] private Vector3 explosionOffset = Vector3.zero;

        [Header("Estratégias de Dano (executadas em sequência)")]
        [SerializeField] private List<DamageStrategySelection> strategyPipeline = new()
        {
            new DamageStrategySelection()
        };

        private IActor _actor;
        private InjectableEntityResourceBridge _bridge;
        private DamageCooldownModule _cooldowns;
        private DamageLifecycleModule _lifecycle;
        private DamageExplosionModule _explosion;
        private IDamageStrategy _strategy;
        private DamageCommandInvoker _commandInvoker;

        [Header("Audio")]
        [SerializeField] private EntityAudioEmitter audioEmitter;
        [SerializeField] private SoundData hitSound;
        [SerializeField] private SoundData deathSound;
        [SerializeField] private SoundData reviveSound;

        private void Awake()
        {
            _actor = GetComponent<IActor>();
            _bridge = GetComponent<InjectableEntityResourceBridge>();
            _cooldowns = new DamageCooldownModule(damageCooldown);
            _lifecycle = new DamageLifecycleModule(_actor.ActorId);
            _explosion = new DamageExplosionModule(transform, explosionPoolData, explosionOffset);
            audioEmitter ??= GetComponent<EntityAudioEmitter>();

            EnsureStrategyConfiguration();
            BuildStrategy();
            BuildCommandPipeline();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureStrategyConfiguration();
            BuildStrategy();
            BuildCommandPipeline();
            _explosion = new DamageExplosionModule(transform, explosionPoolData, explosionOffset);
            audioEmitter ??= GetComponent<EntityAudioEmitter>();
        }
#endif

        private void Start()
        {
            if (Application.isPlaying && spawnExplosionOnDeath)
            {
                _explosion.Initialize();
            }
        }

        private void OnDestroy()
        {
            _lifecycle?.Dispose();
            _lifecycle = null;
        }

        private void EnsureStrategyConfiguration()
        {
            if (strategyPipeline == null || strategyPipeline.Count == 0)
            {
                strategyPipeline = new List<DamageStrategySelection>
                {
                    new DamageStrategySelection()
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
                new RaiseDamageEventsCommand(),
                new CheckDeathCommand(),
                new SpawnExplosionCommand()
            });
        }

        public void ReceiveDamage(DamageContext ctx)
        {
            if (ctx == null || _commandInvoker == null)
            {
                return;
            }

            var context = new DamageCommandContext(
                ctx,
                targetResource,
                _bridge,
                _strategy,
                _cooldowns,
                _lifecycle,
                spawnExplosionOnDeath ? _explosion : null);

            _commandInvoker.Execute(context);
            HandleAudioFeedback(context);
        }

        public string GetReceiverId() => _actor.ActorId;

        public void UndoLastDamage()
        {
            _commandInvoker?.UndoLast();
        }

        private void HandleAudioFeedback(DamageCommandContext context)
        {
            if (audioEmitter == null || context == null)
            {
                return;
            }

            var request = context.Request;
            if (request == null)
            {
                return;
            }

            bool hasHitSound = hitSound != null && hitSound.clip != null;
            bool hasDeathSound = deathSound != null && deathSound.clip != null;
            bool hasReviveSound = reviveSound != null && reviveSound.clip != null;

            if (hasHitSound && context.DamageApplied)
            {
                var hitPosition = request.HasHitPosition ? request.HitPosition : transform.position;
                var ctx = AudioContext.Default(hitPosition, audioEmitter.UsesSpatialBlend);
                audioEmitter.Play(hitSound, ctx);
            }

            if (!context.DeathStateChanged || context.LifecycleModule == null)
            {
                return;
            }

            var center = transform.position;
            var deathCtx = AudioContext.Default(center, audioEmitter.UsesSpatialBlend);

            if (context.LifecycleModule.IsDead)
            {
                if (hasDeathSound)
                {
                    audioEmitter.Play(deathSound, deathCtx);
                }
            }
            else if (hasReviveSound)
            {
                audioEmitter.Play(reviveSound, deathCtx);
            }
        }

    }
}
