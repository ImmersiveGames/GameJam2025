using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// SoundBuilder que usa pool local do controller.
    /// </summary>
    public class SoundBuilder
    {
        private readonly ObjectPool _pool;
        private readonly IAudioMathService _math;
        private readonly IAudioVolumeService _volumeService;
        private readonly AudioServiceSettings _settings;
        private readonly AudioConfig _audioConfig;

        private SoundData _sound;
        private Vector3 _position = Vector3.zero;
        private bool _useSpatial = true;
        private bool _randomPitch;
        private float _volumeMultiplier = 1f;
        private float _fadeIn;

        public SoundBuilder(ObjectPool pool, IAudioMathService math, IAudioVolumeService volumeService, AudioServiceSettings settings, AudioConfig audioCfg)
        {
            _pool = pool;
            _math = math;
            _volumeService = volumeService;
            _settings = settings;
            _audioConfig = audioCfg;
        }

        public SoundBuilder WithSoundData(SoundData sd)
        {
            _sound = sd;
            return this;
        }
        public SoundBuilder AtPosition(Vector3 pos, bool spatial = true)
        {
            _position = pos;
            _useSpatial = spatial;
            return this;
        }
        public SoundBuilder WithRandomPitch(bool v = true)
        {
            _randomPitch = v;
            return this;
        }
        public SoundBuilder WithVolumeMultiplier(float m)
        {
            _volumeMultiplier = m;
            return this;
        }
        public SoundBuilder WithFadeIn(float seconds)
        {
            _fadeIn = seconds;
            return this;
        }

        public void Play()
        {
            if (_sound == null || _pool == null) return;
            var ctx = AudioContext.Default(_position, _useSpatial, _volumeMultiplier);

            var emitter = _pool.GetObject(_position, null, null, false) as SoundEmitter;
            if (emitter == null) return;

            emitter.Initialize(_sound);
            emitter.SetSpatialBlend(_useSpatial ? _sound.spatialBlend : 0f);
            emitter.SetMaxDistance(_audioConfig != null ? _audioConfig.maxDistance : _sound.maxDistance);
            emitter.SetMixerGroup(_sound.mixerGroup ?? _audioConfig?.defaultMixerGroup);

            float master = _settings != null ? _settings.masterVolume : 1f;
            float catVol = _settings != null ? _settings.sfxVolume : 1f;
            float catMul = _settings != null ? _settings.sfxMultiplier : 1f;
            float configDefault = _audioConfig != null ? _audioConfig.defaultVolume : 1f;

            float finalVol = _volumeService?.CalculateSfxVolume(_sound, _audioConfig, _settings, ctx) ??
                             _math?.CalculateFinalVolume(_sound.volume, configDefault, catVol, catMul, master, ctx.volumeMultiplier, ctx.volumeOverride) ??
                             Mathf.Clamp01(_sound.volume * configDefault * catVol * master * ctx.volumeMultiplier);

            float multiplier = _sound.volume > 0f ? Mathf.Clamp01(finalVol / _sound.volume) : Mathf.Clamp01(finalVol);
            emitter.SetVolumeMultiplier(multiplier);

            if (_randomPitch || _sound.randomPitch) emitter.WithRandomPitch(-_sound.pitchVariation, _sound.pitchVariation);

            emitter.Activate(_position);
            if (_fadeIn > 0f) emitter.PlayWithFade(multiplier, _fadeIn);
            else emitter.Play();
        }
    }
}
