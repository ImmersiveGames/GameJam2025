using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Bind;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;
using UnityEngine.Serialization;
using _ImmersiveGames.Scripts.DamageSystem.Commands;
using _ImmersiveGames.Scripts.DamageSystem.Strategies;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    [RequireComponent(typeof(ActorMaster))]
    public class DamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Recurso alvo (ex: Health)")]
        [SerializeField] private ResourceType targetResource = ResourceType.Health;
        [SerializeField] private float damageCooldown = 0.25f;

        [Header("Estratégias de Dano (executadas em sequência)")]
        [SerializeField] private List<DamageStrategySelection> strategyPipeline = new()
        {
            new DamageStrategySelection()
        };

        [SerializeField, HideInInspector, FormerlySerializedAs("strategyType")]
        private DamageStrategyType legacyStrategyType = DamageStrategyType.Basic;

        [SerializeField, HideInInspector, FormerlySerializedAs("criticalSettings")]
        private CriticalDamageSettings legacyCriticalSettings = new();

        [SerializeField, HideInInspector, FormerlySerializedAs("resistanceModifiers")]
        private DamageModifiers legacyResistanceModifiers = new();

        [SerializeField, HideInInspector]
        private bool legacyStrategyMigrated;

        private ActorMaster _actor;
        private InjectableEntityResourceBridge _bridge;
        private DamageCooldownModule _cooldowns;
        private DamageLifecycleModule _lifecycle;
        private IDamageStrategy _strategy;
        private DamageCommandInvoker _commandInvoker;

        private void Awake()
        {
            _actor = GetComponent<ActorMaster>();
            _bridge = GetComponent<InjectableEntityResourceBridge>();
            _cooldowns = new DamageCooldownModule(damageCooldown);
            _lifecycle = new DamageLifecycleModule(_actor.ActorId);

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

            TryMigrateLegacyStrategy();

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
                new CheckDeathCommand()
            });
        }

        private void TryMigrateLegacyStrategy()
        {
            if (legacyStrategyMigrated)
                return;

            if (strategyPipeline == null || strategyPipeline.Count == 0)
                return;

            if (strategyPipeline.Count > 1)
            {
                legacyStrategyMigrated = true;
                return;
            }

            var current = strategyPipeline[0];
            if (current != null && current.type != DamageStrategyType.Basic)
            {
                legacyStrategyMigrated = true;
                return;
            }

            bool hasLegacyCritical = legacyCriticalSettings != null && HasNonDefaultCritical(legacyCriticalSettings);
            bool hasLegacyResistance = legacyResistanceModifiers != null && legacyResistanceModifiers.Entries.Count > 0;
            bool shouldApplyLegacy = legacyStrategyType != DamageStrategyType.Basic || hasLegacyCritical || hasLegacyResistance;

            if (!shouldApplyLegacy)
            {
                legacyStrategyMigrated = true;
                return;
            }

            current ??= new DamageStrategySelection();
            current.type = legacyStrategyType;
            current.criticalSettings = legacyCriticalSettings ?? new CriticalDamageSettings();
            current.resistanceModifiers = legacyResistanceModifiers ?? new DamageModifiers();

            strategyPipeline[0] = current;
            legacyStrategyMigrated = true;
        }

        private static bool HasNonDefaultCritical(CriticalDamageSettings settings)
        {
            return !Mathf.Approximately(settings.criticalChance, 0.2f) ||
                   !Mathf.Approximately(settings.criticalMultiplier, 2f);
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
                _lifecycle);

            _commandInvoker.Execute(context);
        }

        public string GetReceiverId() => _actor.ActorId;

        public void UndoLastDamage()
        {
            _commandInvoker?.UndoLast();
        }
    }
}
