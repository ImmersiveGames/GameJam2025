using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Builder fluente para tocar sons dinamicamente via AudioService ou pool local.
    /// Agora recebe opcionalmente um ObjectPool (local) para obter emitters diretamente.
    /// </summary>
    public class SoundBuilder
    {
        private readonly IAudioService _audioService;
        private readonly ObjectPool _localPool;
        private SoundData _soundData;
        private Vector3 _position = Vector3.zero;
        private bool _useSpatial = true;
        private bool _randomPitch;
        private float _volumeMultiplier = 1f;

        public SoundBuilder(IAudioService audioService, ObjectPool localPool = null)
        {
            _audioService = audioService;
            _localPool = localPool;
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
            if (_soundData == null) return;

            var ctx = AudioContext.Default(_position, _useSpatial, _soundData.GetEffectiveVolume(_volumeMultiplier));

            // Tentar usar pool local primeiro
            if (_localPool != null)
            {
                var emit = _localPool.GetObject(ctx.position, null, null, false) as SoundEmitter;
                if (emit != null)
                {
                    emit.Initialize(_soundData, _audioService);
                    emit.SetSpatialBlend(ctx.useSpatial ? _soundData.spatialBlend : 0f);
                    emit.SetMaxDistance(_soundData.maxDistance);
                    emit.SetVolumeMultiplier(ctx.volumeMultiplier);
                    if (_soundData.randomPitch || _randomPitch)
                        emit.WithRandomPitch(-_soundData.pitchVariation, _soundData.pitchVariation);
                    emit.Activate(ctx.position);
                    emit.Play();
                    if (_soundData.frequentSound) _audioService?.RegisterFrequentSound(emit);
                    return;
                }
            }

            // fallback global
            _audioService?.PlaySound(_soundData, ctx);
        }
    }
}
