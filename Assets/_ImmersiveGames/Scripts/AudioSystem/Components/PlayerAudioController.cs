using System;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class PlayerAudioController : AudioControllerBase
    {
        private DamageReceiver _damageReceiver;

        private Action<float, IActor> _onDamageReceived;
        private Action<IActor> _onDeath;
        private Action<IActor> _onRevive;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _damageReceiver);

            if (_damageReceiver != null && audioConfig != null)
            {
                _onDamageReceived = (_, _) => PlaySoundLocal(audioConfig.hitSound, AudioContext.Default(transform.position, audioConfig.useSpatialBlend));
                _onDeath = _ => PlaySoundLocal(audioConfig.deathSound, AudioContext.Default(transform.position, audioConfig.useSpatialBlend));
                _onRevive = _ => PlaySoundLocal(audioConfig.reviveSound, AudioContext.Default(transform.position, audioConfig.useSpatialBlend));

                /*_damageReceiver.EventDamageReceived += _onDamageReceived;
                _damageReceiver.EventDeath += _onDeath;
                _damageReceiver.EventRevive += _onRevive;*/
            }
        }

        private void OnDestroy()
        {
            if (_damageReceiver == null) return;
            /*_damageReceiver.EventDamageReceived -= _onDamageReceived;
            _damageReceiver.EventDeath -= _onDeath;
            _damageReceiver.EventRevive -= _onRevive;*/
        }

        public void PlayShootSound(SoundData strategySound = null, float volumeMultiplier = 1f)
        {
            var soundToPlay = strategySound ?? audioConfig?.shootSound;
            if (soundToPlay == null) return;
            var ctx = AudioContext.Default(transform.position, audioConfig?.useSpatialBlend ?? true, volumeMultiplier);
            PlaySoundLocal(soundToPlay, ctx);
        }

        // helpers para testes/debug
        public AudioConfig GetAudioConfig() => audioConfig;
        public SoundBuilder CreateSoundBuilderPublic() => CreateSoundBuilder();
        public void TestPlaySoundPublic(SoundData sound, float volMult = 1f)
        {
            if (sound == null) return;
            PlaySoundLocal(sound, AudioContext.Default(transform.position, audioConfig?.useSpatialBlend ?? true, volMult));
        }
    }
}
