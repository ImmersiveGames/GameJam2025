using _ImmersiveGames.Scripts.ActorSystems;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public interface IDestructible
    {
        void Heal(float amount, IActor byActor = null);
        void TakeDamage(float damage, IActor byActor = null);
        float GetCurrentValue();
    }
}