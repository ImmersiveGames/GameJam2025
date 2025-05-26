using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public interface ISpawnStrategy
    {
        void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data, Vector3 transformForward);
    }
}