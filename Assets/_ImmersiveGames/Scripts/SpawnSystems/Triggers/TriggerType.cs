using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public enum TriggerType
    {
        InitializationTrigger,
        IntervalTrigger,
        InputSystemTrigger,
        GlobalEventTrigger,
        GenericGlobalEventTrigger,
        PredicateTrigger
    }
    public enum StrategyType
    {
        SimpleSpawnStrategy,
        DirectionalSpawnStrategy,
        FullPoolSpawnStrategy,
        OrbitPlanetStrategy
    }
}