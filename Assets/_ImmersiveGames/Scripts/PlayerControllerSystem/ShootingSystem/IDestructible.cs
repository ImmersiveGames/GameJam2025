namespace _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem
{
    public interface IDestructible
    {
        public void Deafeat();
        public void Heal(float amount);
        public void TakeDamage(float damage);
    }
}