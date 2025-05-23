using _ImmersiveGames.Scripts.PoolSystem;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public interface IShootingStrategy
    {
        void Initialize(Transform firePointRef, Camera mainCameraRef, ProjectileObjectPool poolRef);
        void Fire(bool isFiring, float deltaTime);
    }
}