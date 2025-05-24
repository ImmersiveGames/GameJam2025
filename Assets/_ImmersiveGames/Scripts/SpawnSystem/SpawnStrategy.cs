using _ImmersiveGames.Scripts.SpawnSystem._ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystem
{
    public abstract class SpawnStrategy : ScriptableObject
    {
        [SerializeField] protected ProjectileData projectileData;
        [SerializeField] protected float fireRate = 0.1f;

        public ProjectileData ProjectileData => projectileData;
        public float FireRate => fireRate;

        public abstract void Spawn(PoolManager poolManager, SpawnParameters parameters);
    }
}