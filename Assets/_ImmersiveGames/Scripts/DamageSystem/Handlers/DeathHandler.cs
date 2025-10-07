using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;

namespace _ImmersiveGames.Scripts.DamageSystem
{
    public interface IDeathHandler
    {
        void ExecuteDeath();
    }

    public class DeathHandler : IDeathHandler
    {
        private readonly DamageReceiver _receiver;

        public DeathHandler(DamageReceiver receiver)
        {
            _receiver = receiver;
        }

        public void ExecuteDeath()
        {
            RaiseDeathEvents();
            SpawnDeathEffect();
        }

        private void RaiseDeathEvents()
        {
            _receiver.OnEventDeath(_receiver.Actor);
            if (_receiver.InvokeEvents)
            {
                EventBus<ActorDeathEvent>.Raise(new ActorDeathEvent(_receiver.Actor, _receiver.transform.position));
            }
        }

        private void SpawnDeathEffect()
        {
            if (_receiver.DeathEffect != null)
            {
                _receiver.DestructionHandler.HandleEffectSpawn(
                    _receiver.DeathEffect, 
                    _receiver.transform.position, 
                    _receiver.transform.rotation
                );
            }
        }
    }
}