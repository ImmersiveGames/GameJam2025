namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public interface IDestructible
    {
        void Heal(float amount);
        void TakeDamage(float damage);
        float GetCurrentValue();
    }
}