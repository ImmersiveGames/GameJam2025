using _ImmersiveGames.Scripts.SpawnSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    [CreateAssetMenu(fileName = "New CircleShotStrategy", menuName = "Shooting/Strategies/CircleShot")]
    public class CircleShotStrategySo : SpawnStrategy
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private int projectileCount = 8;

        public override void Spawn(PoolManager poolManager, SpawnParameters parameters)
        {
            DebugUtility.LogVerbose<CircleShotStrategySo>($"Iniciando spawn para '{parameters.poolName}' em {parameters.spawnPosition}.", "blue");
            if (poolManager == null)
            {
                DebugUtility.LogError<CircleShotStrategySo>("PoolManager é nulo.", null);
                return;
            }

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = 360f / projectileCount * i;
                var direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                var bullet = poolManager.GetObject(parameters.poolName, parameters.spawnPosition);
                if (bullet == null)
                {
                    DebugUtility.LogError<CircleShotStrategySo>($"Falha ao obter projétil do pool '{parameters.poolName}'.", null);
                    continue;
                }
                var projectile = bullet as IProjectile;
                if (projectile != null)
                {
                    projectile.Configure(direction, speed);
                }
                DebugUtility.LogVerbose<CircleShotStrategySo>($"Projétil disparado em {parameters.spawnPosition}, ângulo {angle}.", "blue");
            }
        }
    }
}