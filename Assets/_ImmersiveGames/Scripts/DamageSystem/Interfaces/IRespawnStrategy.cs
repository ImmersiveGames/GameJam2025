namespace _ImmersiveGames.Scripts.DamageSystem
{
    public interface IRespawnStrategy
    {
        void Execute(DamageReceiver receiver);
    }
}