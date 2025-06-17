using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Spawn/StrategyData")]
    public class StrategyData : ScriptableObject
    {
        public StrategyType strategyType;
        public StrategyProperties properties = new StrategyProperties();
    }
}