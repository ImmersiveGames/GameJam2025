using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem.ProjectSystems
{
    [CreateAssetMenu(fileName = "New FanShotStrategy", menuName = "Shooting/Strategies/FanShot")]
    public class FanShotStrategySo : SpawnStrategy
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private int projectileCount = 3;
        [SerializeField] private float spreadAngle = 30f;

        public override void Spawn(PoolManager poolManager, SpawnParameters parameters)
        {
            DebugUtility.LogVerbose<FanShotStrategySo>($"Iniciando spawn para '{parameters.poolName}' em {parameters.spawnPosition}.", "blue");
            if (poolManager == null)
            {
                DebugUtility.LogError<FanShotStrategySo>("PoolManager é nulo.", null);
                return;
            }

            var baseDirection = parameters.mainCamera != null ? parameters.mainCamera.transform.forward : Vector3.forward;
            float startAngle = -spreadAngle / 2;

            for (int i = 0; i < projectileCount; i++)
            {
                float currentAngle = startAngle + (spreadAngle / (projectileCount - 1)) * i;
                var direction = Quaternion.Euler(0, currentAngle, 0) * baseDirection;
                var bullet = poolManager.GetObject(parameters.poolName, parameters.spawnPosition);
                if (bullet == null)
                {
                    DebugUtility.LogError<FanShotStrategySo>($"Falha ao obter projétil do pool '{parameters.poolName}'.", null);
                    continue;
                }
                var projectile = bullet as IProjectile;
                if (projectile != null)
                {
                    projectile.Configure(direction, speed);
                }
                DebugUtility.LogVerbose<FanShotStrategySo>($"Projétil disparado em {parameters.spawnPosition}, ângulo {currentAngle}.", "blue");
            }
        }
    }
}