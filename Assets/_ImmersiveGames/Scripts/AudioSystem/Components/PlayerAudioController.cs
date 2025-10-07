using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class PlayerAudioController : AudioControllerBase
    {
        private ActorMaster _actorMaster;
        private EventBinding<PlayerDiedEvent> _playerDiedEvent;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _actorMaster);
        }

        private void OnEnable()
        {
            if (_actorMaster != null)
            {
                // Sons de tiro agora são gerenciados pelo InputSpawnerComponent
                //_actorMaster.EventPlayerTakeDamage += PlayHitSound;
            }

            _playerDiedEvent = new EventBinding<PlayerDiedEvent>(PlayDeathSound);
            EventBus<PlayerDiedEvent>.Register(_playerDiedEvent);
        }

        private void OnDisable()
        {
            if (_actorMaster != null)
            {
                //_actorMaster.EventPlayerTakeDamage -= PlayHitSound;
            }

            EventBus<PlayerDiedEvent>.Unregister(_playerDiedEvent);
        }

        private void PlayHitSound(IActor byActor) 
        {
            if (audioConfig?.hitSound != null)
                PlaySound(audioConfig.hitSound);
        }

        private void PlayDeathSound() 
        {
            if (audioConfig?.deathSound != null)
                PlaySound(audioConfig.deathSound);
        }
    }
}