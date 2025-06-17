using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public class SpawnFactory
    {
        private static SpawnFactory _instance;
        public static SpawnFactory Instance => _instance ??= new SpawnFactory();

        private SpawnFactory() { }

        public ISpawnTrigger CreateTrigger(TriggerData data, InputActionAsset inputAsset = null)
        {
            if (data == null)
            {
                DebugUtility.LogError<SpawnFactory>("TriggerData é nulo.");
                return new SimpleSpawnTrigger();
            }

            var properties = data.properties ?? new TriggerProperties();
            if (data.triggerType is TriggerType.InputSystemTrigger or TriggerType.InputSystemHoldTrigger && inputAsset == null)
            {
                DebugUtility.LogError<SpawnFactory>($"InputActionAsset não fornecido para {data.triggerType}.");
                return null;
            }

            ValidateTriggerProperties(data.triggerType, properties, inputAsset);

            return data.triggerType switch
            {
                TriggerType.SimpleTrigger => new SimpleSpawnTrigger(),
                TriggerType.IntervalTrigger => new IntervalTrigger(properties.interval, properties.startImmediately),
                TriggerType.InputSystemTrigger => new InputSystemTrigger(properties.actionName, inputAsset),
                TriggerType.InputSystemHoldTrigger => new InputSystemHoldTrigger(properties.actionName, inputAsset),
                TriggerType.InitializationTrigger => new InitializationTrigger(),
                TriggerType.DeathEventTrigger => new DeathEventTrigger(),
                TriggerType.PredicateTrigger => new PredicateTrigger(properties.predicate),
                TriggerType.CompositeTrigger => CreateCompositeTrigger(properties, inputAsset),
                _ => new SimpleSpawnTrigger()
            };
        }

        private ISpawnTrigger CreateCompositeTrigger(TriggerProperties properties, InputActionAsset inputAsset)
        {
            if (properties.compositeTriggers == null || properties.compositeTriggers.Count == 0)
            {
                DebugUtility.LogError<SpawnFactory>("Lista de CompositeTriggers está vazia.");
                return new SimpleSpawnTrigger();
            }

            var triggers = new List<ISpawnTrigger>();
            foreach (var triggerData in properties.compositeTriggers)
            {
                var trigger = CreateTrigger(triggerData, inputAsset);
                if (trigger != null)
                    triggers.Add(trigger);
                else
                    DebugUtility.LogWarning<SpawnFactory>($"Falha ao criar trigger {triggerData.triggerType} para CompositeTrigger.");
            }

            if (triggers.Count == 0)
            {
                DebugUtility.LogError<SpawnFactory>("Nenhum trigger válido criado para CompositeTrigger.");
                return new SimpleSpawnTrigger();
            }

            return new CompositeTrigger(triggers, properties.combinationMode);
        }

        public ISpawnStrategy CreateStrategy(StrategyData data)
        {
            if (data == null)
            {
                DebugUtility.LogError<SpawnFactory>("StrategyData é nulo.");
                return new SingleSpawnStrategy();
            }

            var properties = data.properties ?? new StrategyProperties();
            ValidateStrategyProperties(data.strategyType, properties);

            return data.strategyType switch
            {
                StrategyType.SingleSpawnStrategy => new SingleSpawnStrategy(),
                StrategyType.SingleShootStrategy => new SingleShootStrategy(),
                StrategyType.BurstStrategy => new BurstStrategy(properties.radius, properties.space),
                StrategyType.RandomSpawnStrategy => new RandomSpawnStrategy(properties.spawnArea),
                StrategyType.WaveSpawnStrategy => new WaveSpawnStrategy(properties.waveInterval, properties.waveCount),
                StrategyType.OrbitPlanetStrategy => new OrbitPlanetStrategy(properties.useRandomAngles, properties.addAngleVariation),
                StrategyType.ContinuousTargetedShootStrategy => new ContinuousTargetedShootStrategy(), // Nova estratégia
                _ => new SingleSpawnStrategy()
            };
        }

        private void ValidateTriggerProperties(TriggerType type, TriggerProperties properties, InputActionAsset inputAsset)
        {
            if (!SpawnPropertyMap.TriggerPropertiesMap.TryGetValue(type, out var expectedProperties)) return;

            foreach (var (nome, tipo, _) in expectedProperties)
            {
                if (tipo == typeof(float) && nome == "interval" && properties.interval <= 0)
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' inválida para {type}.");
                else if (tipo == typeof(string) && nome == "actionName")
                {
                    if (string.IsNullOrEmpty(properties.actionName))
                        DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' é obrigatória para {type}.");
                    else if (inputAsset != null && inputAsset.FindAction(properties.actionName) == null)
                        DebugUtility.LogError<SpawnFactory>($"Ação '{properties.actionName}' não encontrada no InputActionAsset para {type}.");
                }
                else if (tipo == typeof(PredicateData) && nome == "predicate" && properties.predicate == null)
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' é obrigatória para {type}.");
                else if (tipo == typeof(List<TriggerData>) && nome == "compositeTriggers" && (properties.compositeTriggers == null || properties.compositeTriggers.Count == 0))
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' é obrigatória e não pode estar vazia para {type}.");
            }
        }

        private void ValidateStrategyProperties(StrategyType type, StrategyProperties properties)
        {
            if (!SpawnPropertyMap.StrategyPropertiesMap.TryGetValue(type, out var expectedProperties)) return;

            foreach (var (nome, tipo, _) in expectedProperties)
            {
                if (tipo == typeof(float) && nome == "radius" && properties.radius <= 0)
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' inválida para {type}.");
                else if (tipo == typeof(float) && nome == "space" && properties.space <= 0)
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' inválida para {type}.");
                else if (tipo == typeof(Vector2) && nome == "spawnArea" && (properties.spawnArea.x <= 0 || properties.spawnArea.y <= 0))
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' inválida para {type}.");
                else if (tipo == typeof(float) && nome == "waveInterval" && properties.waveInterval <= 0)
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' inválida para {type}.");
                else if (tipo == typeof(int) && nome == "waveCount" && properties.waveCount <= 0)
                    DebugUtility.LogError<SpawnFactory>($"Propriedade '{nome}' inválida para {type}.");
            }
        }
    }
}