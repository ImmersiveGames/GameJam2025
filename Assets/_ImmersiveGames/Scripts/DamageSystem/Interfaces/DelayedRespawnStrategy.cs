using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DelayedRespawnStrategy : IRespawnStrategy
    {
        public void Execute(DamageReceiver receiver)
        {
            if (receiver.respawnTime == 0f)
            {
                receiver.Revive();
            }
            else if (receiver.respawnTime > 0f)
            {
                if (receiver.deactivateOnDeath && !receiver.destroyOnDeath) receiver.gameObject.SetActive(false);
                receiver.Invoke(nameof(receiver.ExecuteDelayedRespawn), receiver.respawnTime);
            }
            else
            {
                receiver.FinalizeDeath();
            }
        }
    }
}