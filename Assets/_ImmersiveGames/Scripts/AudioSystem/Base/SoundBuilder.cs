using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class SoundBuilder
    {
        private readonly IAudioService _audioManager;
        private SoundData _soundData;
        private Vector3 _position = Vector3.zero;
        private bool _randomPitch;
        private float _spatialBlend = 0f;
        private float _maxDistance = 50f;
        private float _volumeMultiplier = 1f;

        public SoundBuilder(IAudioService audioManager)
        {
            _audioManager = audioManager;
        }

        public SoundBuilder WithSoundData(SoundData soundData)
        {
            _soundData = soundData;
            return this;
        }

        public SoundBuilder WithPosition(Vector3 position)
        {
            _position = position;
            return this;
        }

        public SoundBuilder WithRandomPitch(bool randomPitch = true)
        {
            _randomPitch = randomPitch;
            return this;
        }

        public SoundBuilder WithSpatialBlend(float spatialBlend)
        {
            _spatialBlend = spatialBlend;
            return this;
        }

        public SoundBuilder WithMaxDistance(float maxDistance)
        {
            _maxDistance = maxDistance;
            return this;
        }

        public SoundBuilder WithVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = multiplier;
            return this;
        }

        public void Play()
        {
            if (_soundData == null || !_audioManager.CanPlaySound(_soundData))
                return;

            SoundEmitter soundEmitter = _audioManager.Get();
            if (soundEmitter == null)
                return;

            // Configura o SoundEmitter
            soundEmitter.Initialize(_soundData, _audioManager);
            soundEmitter.SetSpatialBlend(_spatialBlend);
            soundEmitter.SetMaxDistance(_maxDistance);

            // Aplica o multiplicador de volume
            soundEmitter.SetVolumeMultiplier(_volumeMultiplier);

            if (_randomPitch)
            {
                soundEmitter.WithRandomPitch();
            }

            // Ativa via pool system
            soundEmitter.Activate(_position);

            // Toca o som
            soundEmitter.Play();

            // Registra emitters de som frequente
            if (_soundData.frequentSound)
            {
                _audioManager.RegisterFrequentSound(soundEmitter);
            }
        }
    }
}
