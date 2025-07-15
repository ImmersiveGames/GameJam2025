namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    public enum TriggerType
    {
        InitializationTrigger,
        IntervalTrigger,
        InputSystemTrigger,
        GlobalEventTrigger,
        GenericGlobalEventTrigger,
        SensorTrigger,
        PredicateTrigger
    }
    public enum StrategyType
    {
        SimpleSpawnStrategy,
        DirectionalSpawnStrategy,
        FullPoolSpawnStrategy,
        OrbitPlanetStrategy,
        CircularZoomOutStrategy
    }
}