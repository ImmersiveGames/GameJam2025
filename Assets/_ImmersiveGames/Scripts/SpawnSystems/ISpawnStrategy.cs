using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public interface ISpawnStrategy
    {
        void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data);
    }
}