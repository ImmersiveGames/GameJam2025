
// DelayedRespawnStrategy.cs
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DelayedRespawnStrategy : IRespawnStrategy
    {
        public void Execute(DamageReceiver receiver)
        {
            switch (receiver.RespawnTime)
            {
                case 0f:
                    receiver.Revive();
                    break;
                case > 0f:
                    if (receiver.DeactivateOnDeath && !receiver.DestroyOnDeath) 
                        receiver.gameObject.SetActive(false);
                    receiver.Invoke(nameof(receiver.ExecuteDelayedRespawn), receiver.RespawnTime);
                    break;
                default:
                    receiver.FinalizeDeath();
                    break;
            }
        }
    }
}

// ImmediateRespawnStrategy.cs (Exemplo de estratégia alternativa)
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class ImmediateRespawnStrategy : IRespawnStrategy
    {
        public void Execute(DamageReceiver receiver)
        {
            receiver.Revive();
        }
    }
}

// NoRespawnStrategy.cs (Exemplo de estratégia alternativa)
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class NoRespawnStrategy : IRespawnStrategy
    {
        public void Execute(DamageReceiver receiver)
        {
            receiver.FinalizeDeath();
        }
    }
}