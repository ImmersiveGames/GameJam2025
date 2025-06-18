using UnityEngine.InputSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem
{
    public class EnhancedSpawnFactory
    {
        private static EnhancedSpawnFactory _instance;
        public static EnhancedSpawnFactory Instance => _instance ??= new EnhancedSpawnFactory();

        private EnhancedSpawnFactory() { }

        public ISpawnTrigger CreateTrigger(EnhancedTriggerData data, InputActionAsset inputAsset = null)
        {
            if (data == null)
            {
                DebugUtility.LogError<EnhancedSpawnFactory>("EnhancedTriggerData é nulo.", null);
                return null;
            }

            if (data.triggerType == TriggerType.InputSystemTrigger && inputAsset == null)
            {
                DebugUtility.LogError<EnhancedSpawnFactory>($"InputActionAsset é necessário para {data.triggerType}.", null);
                return null;
            }

            return data.triggerType switch
            {
                TriggerType.InitializationTrigger => new InitializationTrigger(data),
                TriggerType.IntervalTrigger => new IntervalTrigger(data),
                TriggerType.InputSystemTrigger => new InputSystemTrigger(data, inputAsset),
                TriggerType.GlobalEventTrigger => new GlobalEventTrigger(data),
                TriggerType.PredicateTrigger => new PredicateTrigger(data),
                _ => null
            };
        }

        public ISpawnStrategy CreateStrategy(EnhancedStrategyData data)
        {
            if (data == null)
            {
                DebugUtility.LogError<EnhancedSpawnFactory>("EnhancedStrategyData é nulo.", null);
                return null;
            }

            return data.strategyType switch
            {
                StrategyType.SimpleSpawnStrategy => new SimpleSpawnStrategy(data),
                StrategyType.DirectionalSpawnStrategy => new DirectionalSpawnStrategy(data),
                StrategyType.FullPoolSpawnStrategy => new FullPoolSpawnStrategy(data),
                _ => null
            };
        }
    }
}