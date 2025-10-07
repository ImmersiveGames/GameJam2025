using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class PlayerAudioController : AudioControllerBase
    {
        private PlayerMaster _playerMaster;
        private EventBinding<PlayerDiedEvent> _playerDiedEvent;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _playerMaster);
        }

        private void OnEnable()
        {
            if (_playerMaster != null)
            {
                _playerMaster.EventPlayerShoot += PlayShootSound;
                _playerMaster.EventPlayerTakeDamage += PlayHitSound;
            }

            _playerDiedEvent = new EventBinding<PlayerDiedEvent>(PlayDeathSound);
            EventBus<PlayerDiedEvent>.Register(_playerDiedEvent);
        }

        private void OnDisable()
        {
            if (_playerMaster != null)
            {
                _playerMaster.EventPlayerShoot -= PlayShootSound;
                _playerMaster.EventPlayerTakeDamage -= PlayHitSound;
            }

            EventBus<PlayerDiedEvent>.Unregister(_playerDiedEvent);
        }

        private void PlayShootSound(IActor byActor) 
        {
            if (audioConfig?.shootSound != null)
                PlaySound(audioConfig.shootSound);
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