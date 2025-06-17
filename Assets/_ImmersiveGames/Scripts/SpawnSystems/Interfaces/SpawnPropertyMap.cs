using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public static class SpawnPropertyMap
    {
        public static readonly Dictionary<TriggerType, List<(string nome, Type tipo, bool obrigatorio)>> TriggerPropertiesMap = new Dictionary<TriggerType, List<(string, Type, bool)>>
        {
            [TriggerType.IntervalTrigger] = new List<(string, Type, bool)>
            {
                ("interval", typeof(float), true),
                ("startImmediately", typeof(bool), false)
            },
            [TriggerType.InputSystemTrigger] = new List<(string, Type, bool)>
            {
                ("actionName", typeof(string), true)
            },
            [TriggerType.InputSystemHoldTrigger] = new List<(string, Type, bool)>
            {
                ("actionName", typeof(string), true)
            },
            [TriggerType.PredicateTrigger] = new List<(string, Type, bool)>
            {
                ("predicate", typeof(PredicateData), true)
            },
            [TriggerType.CompositeTrigger] = new List<(string, Type, bool)>
            {
                ("compositeTriggers", typeof(List<TriggerData>), true),
                ("combinationMode", typeof(CombinationMode), false)
            }
        };

        public static readonly Dictionary<StrategyType, List<(string nome, Type tipo, bool obrigatorio)>> StrategyPropertiesMap = new Dictionary<StrategyType, List<(string, Type, bool)>>
        {
            [StrategyType.BurstStrategy] = new List<(string, Type, bool)>
            {
                ("radius", typeof(float), true),
                ("space", typeof(float), true)
            },
            [StrategyType.RandomSpawnStrategy] = new List<(string, Type, bool)>
            {
                ("spawnArea", typeof(Vector2), true)
            },
            [StrategyType.WaveSpawnStrategy] = new List<(string, Type, bool)>
            {
                ("waveInterval", typeof(float), true),
                ("waveCount", typeof(int), true)
            },
            [StrategyType.OrbitPlanetStrategy] = new List<(string, Type, bool)>
            {
                ("useRandomAngles", typeof(bool), false),
                ("addAngleVariation", typeof(bool), false)
            },
            [StrategyType.ContinuousTargetedShootStrategy] = new List<(string, Type, bool)>
            {
                ("interval", typeof(float), true) // Intervalo para disparos contínuos
            }
        };
    }
}