using System.Collections.Generic;
using _ImmersiveGames.Scripts.DetectionsSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public enum TriggerType
    {
        InitializationTrigger,
        IntervalTrigger,
        InputSystemTrigger,
        GlobalEventTrigger,
        PredicateTrigger
    }
    public enum StrategyType
    {
        SimpleSpawnStrategy,
        DirectionalSpawnStrategy,
        FullPoolSpawnStrategy,
        OrbitPlanetStrategy
    }
    
    [System.Serializable]
    public enum CombinationMode
    {
        AND,
        OR
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
        public float interval = 1f; // Intervalo para ContinuousTargetedShootStrategy
    }
}