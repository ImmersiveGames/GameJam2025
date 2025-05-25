using _ImmersiveGames.Scripts.SpawnSystemOLD;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public interface ISpawnStrategy
    {
        void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data);
    }
}