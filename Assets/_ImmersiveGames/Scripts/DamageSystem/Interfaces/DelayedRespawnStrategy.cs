namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DelayedRespawnStrategy : IRespawnStrategy
    {
        public void Execute(DamageReceiver receiver)
        {
            switch (receiver.respawnTime)
            {
                case 0f:
                    receiver.Revive();
                    break;
                case > 0f: {
                    if (receiver.deactivateOnDeath && !receiver.destroyOnDeath) receiver.gameObject.SetActive(false);
                    receiver.Invoke(nameof(receiver.ExecuteDelayedRespawn), receiver.respawnTime);
                    break;
                }
                default:
                    receiver.FinalizeDeath();
                    break;
            }
        }
    }
}