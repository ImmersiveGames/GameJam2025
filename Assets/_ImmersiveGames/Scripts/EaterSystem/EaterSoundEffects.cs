using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.EaterSystem.Events;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    public class EaterSoundEffects : MonoBehaviour
    {
        [SerializeField] SoundData biteSoundEffect;
        [SerializeField] SoundData getHitSoundEffect;
        [SerializeField] SoundData dieSoundEffect;
        [SerializeField] SoundData happySoundEffect;
        [SerializeField] SoundData madSoundEffect;

        private EaterMaster _eaterMaster;

        private EventBinding<EaterDeathEvent> _eaterDeathBinding;

        private void Awake()
        {
            TryGetComponent(out _eaterMaster);

        }

        void OnEnable()
        {
            _eaterMaster.EventEaterBite += PlayBiteSoundEffect;
            _eaterMaster.EventEaterTakeDamage += PlayGetHitSoundEffect;
            _eaterMaster.EventConsumeResource += OnConsumeResource;

            _eaterDeathBinding = new EventBinding<EaterDeathEvent>(PlayDieSoundEffect);
            EventBus<EaterDeathEvent>.Register(_eaterDeathBinding);
        }

        void OnDisable()
        {
            _eaterMaster.EventEaterBite -= PlayBiteSoundEffect;
            _eaterMaster.EventEaterTakeDamage -= PlayGetHitSoundEffect;
            _eaterMaster.EventConsumeResource -= OnConsumeResource;

            EventBus<EaterDeathEvent>.Unregister(_eaterDeathBinding);
        }

        void PlaySoundEffect(SoundData soundData)
        {
            SoundManager.Instance.CreateSound()
                .WithSoundData(soundData)
                .WithRandomPitch(soundData.randomPitch)
                .WithPosition(transform.position)
                .Play();
        }

        private void PlayBiteSoundEffect(IActor byActor) { PlaySoundEffect(biteSoundEffect); }
        public void PlayGetHitSoundEffect(IActor byActor) { PlaySoundEffect(getHitSoundEffect); }
        public void PlayDieSoundEffect() { PlaySoundEffect(dieSoundEffect); }
        
        public void OnConsumeResource(IDetectable obj, bool isHappy, IActor byActor) 
        {
            if (byActor is not EaterMaster) return;
            PlaySoundEffect(isHappy ? happySoundEffect : madSoundEffect);
        }
    }
}