using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions.Strategy
{
    /// <summary>
    /// Base para estratégias de perseguição de minions.
    /// </summary>
    public abstract class MinionChaseStrategySo : ScriptableObject
    {
        public abstract Tween CreateChaseTween(
            Transform minion,
            Transform target,
            float baseSpeed,
            string targetLabel);
    }
}