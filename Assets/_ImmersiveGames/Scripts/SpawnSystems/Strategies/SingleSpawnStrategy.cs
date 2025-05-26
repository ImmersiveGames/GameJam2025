using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    public class SingleSpawnStrategy : ISpawnStrategy
    {
        public void Spawn(IPoolable[] objects, Vector3 origin, SpawnData data, Vector3 spawnDirection)
        {
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                var go = obj.GetGameObject();
                go.transform.position = origin;
                obj.Activate(origin);

                if (data is ShootingSpawnData shootingData)
                {
                    var movement = go.GetComponent<ProjectileMovement>();
                    if (movement)
                    {
                        movement.InitializeMovement(spawnDirection.normalized, shootingData.ProjectileSpeed);
                    }
                    else
                    {
                        DebugUtility.LogError<SingleSpawnStrategy>($"ProjectileMovement não encontrado em {go.name}.", go);
                    }
                }
                else
                {
                    DebugUtility.LogError<SingleSpawnStrategy>($"SpawnData inválido para {go.name}. Esperado ShootingSpawnData.", go);
                }
            }
        }
    }
}