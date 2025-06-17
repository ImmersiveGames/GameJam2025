using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public static class SpawnPropertyMap
    {
        public static readonly Dictionary<TriggerType, List<(string nome, Type tipo, object valorPadrão)>> TriggerPropertiesMap = new()
        {
            { TriggerType.SimpleTrigger, new List<(string, Type, object)>() },
            { TriggerType.IntervalTrigger, new List<(string, Type, object)>
                {
                    ("interval", typeof(float), 2f),
                    ("startImmediately", typeof(bool), true)
                }
            },
            { TriggerType.InputSystemTrigger, new List<(string, Type, object)>
                {
                    ("actionName", typeof(string), "Fire")
                }
            },
            { TriggerType.InputSystemHoldTrigger, new List<(string, Type, object)>
                {
                    ("actionName", typeof(string), "Fire")
                }
            },
            { TriggerType.InitializationTrigger, new List<(string, Type, object)>() },
            { TriggerType.DeathEventTrigger, new List<(string, Type, object)>() },
            { TriggerType.PredicateTrigger, new List<(string, Type, object)>
                {
                    ("predicate", typeof(PredicateData), null)
                }
            },
            { TriggerType.CompositeTrigger, new List<(string, Type, object)>
                {
                    ("compositeTriggers", typeof(List<TriggerData>), new List<TriggerData>()),
                    ("combinationMode", typeof(CombinationMode), CombinationMode.AND)
                }
            }
        };

        public static readonly Dictionary<StrategyType, List<(string nome, Type tipo, object valorPadrão)>> StrategyPropertiesMap = new()
        {
            { StrategyType.SingleSpawnStrategy, new List<(string, Type, object)>() },
            { StrategyType.SingleShootStrategy, new List<(string, Type, object)>() },
            { StrategyType.BurstStrategy, new List<(string, Type, object)>
                {
                    ("radius", typeof(float), 2f),
                    ("space", typeof(float), 5f)
                }
            },
            { StrategyType.RandomSpawnStrategy, new List<(string, Type, object)>
                {
                    ("spawnArea", typeof(Vector2), new Vector2(5f, 5f))
                }
            },
            { StrategyType.WaveSpawnStrategy, new List<(string, Type, object)>
                {
                    ("waveInterval", typeof(float), 1f),
                    ("waveCount", typeof(int), 3)
                }
            },
            { StrategyType.OrbitPlanetStrategy, new List<(string, Type, object)>
                {
                    ("useRandomAngles", typeof(bool), false),
                    ("addAngleVariation", typeof(bool), true)
                }
            }
        };
    }
}