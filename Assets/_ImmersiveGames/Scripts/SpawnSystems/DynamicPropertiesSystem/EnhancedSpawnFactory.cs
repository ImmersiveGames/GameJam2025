
using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.SpawnSystems.Strategies;
using _ImmersiveGames.Scripts.SpawnSystems.Triggers;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem
{
    public class EnhancedSpawnFactory
    {
        private static EnhancedSpawnFactory _instance;
        public static EnhancedSpawnFactory Instance => _instance ??= new EnhancedSpawnFactory();

        private EnhancedSpawnFactory() { }

        public ISpawnTriggerOld CreateTrigger(EnhancedTriggerData data, InputActionAsset inputAsset = null)
        {
            if (data == null)
            {
                DebugUtility.LogError<EnhancedSpawnFactory>("EnhancedTriggerData é nulo.");
                return null;
            }

            if (data.triggerType == TriggerType.InputSystemTrigger && inputAsset == null)
            {
                DebugUtility.LogError<EnhancedSpawnFactory>($"InputActionAsset é necessário para InputSystemTriggerOld.");
                return null;
            }

            return data.triggerType switch
            {
                TriggerType.InitializationTrigger => new InitializationTriggerOld(data),
                TriggerType.IntervalTrigger => new IntervalTriggerOld(data),
                TriggerType.InputSystemTrigger => new InputSystemTriggerOld(data, inputAsset),
                TriggerType.GlobalEventTrigger => new GlobalEventTriggerOld(data),
                TriggerType.GenericGlobalEventTrigger => new GenericGlobalEventTriggerOld(data),
                TriggerType.SensorTrigger => new SensorTriggerOld(data),
                TriggerType.PredicateTrigger => new PredicateTriggerOld(data),
                _ => null
            };
        }

        public ISpawnStrategy CreateStrategy(EnhancedStrategyData data)
        {
            if (data != null)
                return data.strategyType switch
                {
                    StrategyType.SimpleSpawnStrategy => new SimpleSpawnStrategy(data),
                    StrategyType.DirectionalSpawnStrategy => new DirectionalSpawnStrategy(data),
                    StrategyType.FullPoolSpawnStrategy => new FullPoolSpawnStrategy(data),
                    //StrategyType.OrbitPlanetStrategy => new OrbitPlanetStrategy(data),
                    StrategyType.CircularZoomOutStrategy => new CircularZoomOutStrategy(data),
                    _ => null
                };
            DebugUtility.LogError<EnhancedSpawnFactory>("EnhancedStrategyData é nulo.");
            return null;

        }
    }
}