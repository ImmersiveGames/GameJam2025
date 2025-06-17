using System;
using _ImmersiveGames.Scripts.ActorSystems;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerMaster : DetectorsMaster
    {
        public event Action EventPlayerTakeDamage;
        public override void Reset()
        {
            IsActive = true;
        }

        public virtual void OnEventPlayerTakeDamage()
        {
            EventPlayerTakeDamage?.Invoke();
        }
    }
}