using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [CreateAssetMenu(fileName = "SingleSpawnStrategy",menuName = "ImmersiveGames/Strategies/SingleSpawn")]
    public class SingleSpawnStrategy : SpawnStrategySo
    {
        [SerializeField] private float projectileSpeed;
        
        public override void Spawn(IPoolable[] objects, Vector3 origin, Vector3 forward)
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
                    movement.InitializeMovement(forward.normalized, projectileSpeed);
                }
                else
                {
                    DebugUtility.LogError<SingleSpawnStrategy>($"ProjectileMovement não encontrado em {go.name}.", go);
                }
            }
        }
    }
}