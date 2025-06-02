using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public interface IDestructible
    {
        void Deafeat(Vector3 position);
        void Heal(float amount);
        void TakeDamage(float damage);
        float GetCurrentValue();
    }
}