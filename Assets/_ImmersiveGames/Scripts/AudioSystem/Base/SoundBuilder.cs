// Path: _ImmersiveGames/Scripts/AudioSystem/SoundBuilder.cs
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Builder fluente para tocar sons dinamicamente via AudioService.
    /// Ideal para efeitos contextuais ou customizados sem acoplar diretamente à lógica de controllers.
    /// </summary>
    public class SoundBuilder
    {
        private readonly IAudioService _audioService;
        private SoundData _soundData;
        private Vector3 _position = Vector3.zero;
        private bool _useSpatial = true;
        private bool _randomPitch;
        private float _volumeMultiplier = 1f;

        public SoundBuilder(IAudioService audioService)
        {
            _audioService = audioService;
        }

        public SoundBuilder WithSoundData(SoundData soundData)
        {
            _soundData = soundData;
            return this;
        }

        public SoundBuilder AtPosition(Vector3 position, bool spatial = true)
        {
            _position = position;
            _useSpatial = spatial;
            return this;
        }

        public SoundBuilder WithRandomPitch(bool enable = true)
        {
            _randomPitch = enable;
            return this;
        }

        public SoundBuilder WithVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = multiplier;
            return this;
        }

        public void Play()
        {
            if (_soundData == null || _audioService == null)
                return;

            var context = AudioContext.Default(_position, _useSpatial, _soundData.GetEffectiveVolume(_volumeMultiplier));
            _audioService.PlaySound(_soundData, context);

            if (_randomPitch)
            {
                // A aleatoriedade de pitch já é tratada internamente pelo SoundEmitter.
                // Esta flag apenas força que a propriedade randomPitch seja respeitada.
                _soundData.randomPitch = true;
            }
        }
    }
}
