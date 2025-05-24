using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem.ProjectSystems
{
    [CreateAssetMenu(fileName = "New SingleShotStrategy", menuName = "Shooting/Strategies/SingleShot")]
    public class SingleShotStrategySo : SpawnStrategy
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private Vector3 direction = Vector3.forward;

        public override void Spawn(PoolManager poolManager, SpawnParameters parameters)
        {
            DebugUtility.LogVerbose<SingleShotStrategySo>($"Iniciando spawn para '{parameters.poolName}' em {parameters.spawnPosition}.", "blue");
            if (poolManager == null)
            {
                DebugUtility.LogError<SingleShotStrategySo>("PoolManager é nulo.", null);
                return;
            }
            var bullet = poolManager.GetObject(parameters.poolName, parameters.spawnPosition);
            if (bullet == null)
            {
                DebugUtility.LogError<SingleShotStrategySo>($"Falha ao obter projétil do pool '{parameters.poolName}'.", null);
                return;
            }

            var projectile = bullet as IProjectile;
            if (projectile != null)
            {
                var dir = parameters.mainCamera != null ? parameters.mainCamera.transform.forward : direction;
                projectile.Configure(dir, speed);
            }
            DebugUtility.LogVerbose<SingleShotStrategySo>($"Projétil disparado em {parameters.spawnPosition}.", "blue");
        }
    }
}