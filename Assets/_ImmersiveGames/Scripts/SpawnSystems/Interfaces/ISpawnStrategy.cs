using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
using UnityEngine.Events;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    public interface ISpawnStrategy
    {
        List<IPoolable> Spawn(ObjectPool pool, Vector3 position, Vector3 direction, IActor spawner, SpawnSystem.RotationMode rotationMode);
    }public interface ISpawnTrigger
    {
        bool ShouldSpawn();
    }
}