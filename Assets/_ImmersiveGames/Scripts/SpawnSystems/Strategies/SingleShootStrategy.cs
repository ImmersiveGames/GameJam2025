using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class SingleShootStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, SpawnData data, Vector3 origin, Vector3 forward)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var go = obj.GetGameObject();
                go.transform.position = origin;
                obj.Activate(origin);

                var movement = go.GetComponent<ProjectileMovement>();
                if (movement)
                {
                    var poolableData = data.PoolableData as ProjectilesData;
                    if (poolableData)
                    {
                        movement.InitializeMovement(forward.normalized, poolableData.speed);
                    }
                    else
                    {
                        DebugUtility.LogError<SingleShootStrategy>("PoolableData não é do tipo ProjectilesData.", go);
                    }
                }
                else
                {
                    DebugUtility.LogError<SingleShootStrategy>($"ProjectileMovement não encontrado em {go.name}.", go);
                }
            }
        }
        public void Spawn(ObjectPool pool, SpawnData data, Vector3 origin, Vector3 forward)
        {
            throw new System.NotImplementedException();
        }
    }
}