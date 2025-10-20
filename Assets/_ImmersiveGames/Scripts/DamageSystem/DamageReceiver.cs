using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using _ImmersiveGames.Scripts.DamageSystem.Commands;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;
using _ImmersiveGames.Scripts.Utils.PoolSystems;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [RequireComponent(typeof(ActorMaster))]
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

        private ActorMaster _actor;
        private InjectableEntityResourceBridge _bridge;
        private DamageCooldownModule _cooldowns;
        private DamageLifecycleModule _lifecycle;
        private DamageExplosionModule _explosion;
        private IDamageStrategy _strategy;
        private DamageCommandInvoker _commandInvoker;

        private void Awake()
        {
            _actor = GetComponent<ActorMaster>();
            _bridge = GetComponent<InjectableEntityResourceBridge>();
            _cooldowns = new DamageCooldownModule(damageCooldown);
            _lifecycle = new DamageLifecycleModule(_actor.ActorId);
            _explosion = new DamageExplosionModule(transform, explosionPoolData, explosionOffset);
            if (Application.isPlaying && spawnExplosionOnDeath)
            {
                _explosion.Initialize();
            }

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
            if (Application.isPlaying && spawnExplosionOnDeath)
            {
                _explosion.Initialize();
            }
        }
#endif

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
        }

        public string GetReceiverId() => _actor.ActorId;

        public void UndoLastDamage()
        {
            _commandInvoker?.UndoLast();
        }

    }
}
