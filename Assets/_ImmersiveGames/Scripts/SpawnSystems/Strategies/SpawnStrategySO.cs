using UnityEngine;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public abstract class SpawnStrategySo : ScriptableObject
    {
        public abstract void Spawn(IPoolable[] objects, Vector3 origin, Vector3 forward);
    }
}