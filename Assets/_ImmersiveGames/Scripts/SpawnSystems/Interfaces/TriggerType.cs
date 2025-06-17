using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public enum TriggerType
    {
        SimpleTrigger,
        IntervalTrigger,
        InputSystemTrigger,
        InputSystemHoldTrigger,
        InitializationTrigger,
        DeathEventTrigger,
        PredicateTrigger,
        CompositeTrigger
    }
    public enum StrategyType
    {
        SingleSpawnStrategy,
        SingleShootStrategy,
        BurstStrategy,
        RandomSpawnStrategy,
        WaveSpawnStrategy,
        OrbitPlanetStrategy
    }
    
    [System.Serializable]
    public enum CombinationMode
    {
        AND,
        OR
    }

    [System.Serializable]
    public class TriggerProperties
    {
        public float interval = 2f;
        public bool startImmediately = true;
        public string actionName = "Fire";
        public PredicateData predicate;
        public List<TriggerData> compositeTriggers = new List<TriggerData>();
        public CombinationMode combinationMode = CombinationMode.AND;
    }
    
    [System.Serializable]
    public class StrategyProperties
    {
        public float radius = 2f;
        public float space = 5f;
        public Vector2 spawnArea = new Vector2(5f, 5f);
        public float waveInterval = 1f;
        public int waveCount = 3;
        public bool useRandomAngles;
        public bool addAngleVariation = true;
    }
}