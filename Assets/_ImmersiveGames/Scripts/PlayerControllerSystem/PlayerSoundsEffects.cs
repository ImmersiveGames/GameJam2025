using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    public class PlayerSoundsEffects : MonoBehaviour
    {
        [SerializeField] SoundData shootSoundEffect;
        [SerializeField] SoundData getHitSoundEffect;
        [SerializeField] SoundData dieSoundEffect;

        private PlayerMaster _playerMaster;

        private EventBinding<PlayerDiedEvent> _playerDiedEvent;

        private void Awake()
        {
            TryGetComponent(out _playerMaster);

        }

        void OnEnable()
        {
            _playerMaster.EventPlayerShoot += PlayShootSoundEffect;
            _playerMaster.EventPlayerTakeDamage += PlayGetHitSoundEffect;

            _playerDiedEvent = new EventBinding<PlayerDiedEvent>(PlayDieSoundEffect);
            EventBus<PlayerDiedEvent>.Register(_playerDiedEvent);
        }

        void OnDisable()
        {
            _playerMaster.EventPlayerShoot -= PlayShootSoundEffect;
            _playerMaster.EventPlayerTakeDamage -= PlayGetHitSoundEffect;

            EventBus<PlayerDiedEvent>.Unregister(_playerDiedEvent);
        }

        void PlaySoundEffect(SoundData soundData) 
        {
            SoundManager.Instance.CreateSound()
                .WithSoundData(soundData)
                .WithRandomPitch(soundData.randomPitch)
                .WithPosition(this.transform.position)
                .Play();
        }

        public void PlayShootSoundEffect(IActor byActor)
        {
            PlaySoundEffect(shootSoundEffect);
        }

        public void PlayGetHitSoundEffect(IActor byActor) 
        {
            PlaySoundEffect(getHitSoundEffect);
        }

        public void PlayDieSoundEffect() 
        {
            PlaySoundEffect(dieSoundEffect);
        }
    }
}
