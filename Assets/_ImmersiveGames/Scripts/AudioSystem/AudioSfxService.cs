using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Pool;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Serviço global de SFX baseado em pooling de <see cref="SoundEmitter"/>.
    /// Mantém um pool global e retorna handles seguros para controlar a reprodução.
    /// </summary>
    public class AudioSfxService : IAudioSfxService
    {
        private readonly IAudioVolumeService _volumeService;
        private readonly AudioServiceSettings _serviceSettings;
        private readonly AudioConfig _defaultConfig;

        private ObjectPool _sfxPool;

        private static readonly NullAudioHandle NullHandle = new();

        public AudioSfxService(IAudioVolumeService volumeService, AudioServiceSettings serviceSettings = null, AudioConfig defaultConfig = null)
        {
            _volumeService = volumeService;
            _serviceSettings = serviceSettings;
            _defaultConfig = defaultConfig;
        }

        public IAudioHandle PlayOneShot(SoundData sound, AudioContext context, float fadeInSeconds = 0f)
        {
            return PlayInternal(sound, context, fadeInSeconds, false);
        }

        public IAudioHandle PlayLoop(SoundData sound, AudioContext context, float fadeInSeconds = 0f)
        {
            return PlayInternal(sound, context, fadeInSeconds, true);
        }

        private IAudioHandle PlayInternal(SoundData sound, AudioContext context, float fadeInSeconds, bool forceLoop)
        {
            if (sound == null || sound.clip == null)
                return NullHandle;

            EnsurePoolInitialized();
            if (_sfxPool == null)
                return NullHandle;

            var poolable = _sfxPool.GetObject(context.position, null, null, false) as SoundEmitter;
            if (poolable == null)
                return NullHandle;

            bool originalLoop = sound.loop;
            if (forceLoop && !sound.loop)
            {
                sound.loop = true;
            }

            poolable.Initialize(sound);

            if (!originalLoop)
            {
                sound.loop = originalLoop;
            }

            poolable.SetSpatialBlend(context.useSpatial ? sound.spatialBlend : 0f);

            float maxDistance = _defaultConfig != null ? _defaultConfig.maxDistance : sound.maxDistance;
            poolable.SetMaxDistance(maxDistance);

            var mixer = sound.mixerGroup ?? _defaultConfig?.defaultMixerGroup;
            poolable.SetMixerGroup(mixer);

            float finalVolume = _volumeService != null
                ? _volumeService.CalculateSfxVolume(sound, _defaultConfig, _serviceSettings, context)
                : Mathf.Clamp01(sound.volume);

            float multiplier = sound.volume > 0f ? Mathf.Clamp01(finalVolume / sound.volume) : Mathf.Clamp01(finalVolume);
            poolable.SetVolumeMultiplier(multiplier);

            if (sound.randomPitch)
                poolable.WithRandomPitch(-sound.pitchVariation, sound.pitchVariation);

            poolable.Activate(context.position);

            if (fadeInSeconds > 0f)
                poolable.PlayWithFade(multiplier, fadeInSeconds);
            else
                poolable.Play();

            return new SoundEmitterHandle(poolable);
        }

        private void EnsurePoolInitialized()
        {
            if (_sfxPool != null)
                return;

            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("AudioSfxService: PoolManager instance not found. Unable to initialize SFX pool.");
                return;
            }

            var poolData = Resources.Load<SoundEmitterPoolData>("Audio/SoundEmitters/PD_SoundEmitter");
            if (poolData == null)
            {
                Debug.LogError("AudioSfxService: SoundEmitterPoolData not found at 'Audio/SoundEmitters/PD_SoundEmitter'.");
                return;
            }

            _sfxPool = poolManager.RegisterPool(poolData);
            if (_sfxPool == null)
            {
                Debug.LogError("AudioSfxService: Failed to register SFX pool via PoolManager.");
            }
        }

        private class SoundEmitterHandle : IAudioHandle
        {
            private readonly SoundEmitter _emitter;

            public SoundEmitterHandle(SoundEmitter emitter)
            {
                _emitter = emitter;
            }

            public bool IsPlaying
            {
                get
                {
                    return _emitter != null && _emitter.gameObject != null && _emitter.gameObject.activeInHierarchy;
                }
            }

            public void Stop(float fadeOutSeconds = 0f)
            {
                if (_emitter == null || _emitter.gameObject == null)
                    return;

                _emitter.Stop();
            }
        }

        private sealed class NullAudioHandle : IAudioHandle
        {
            public bool IsPlaying => false;
            public void Stop(float fadeOutSeconds = 0f) { /* noop */ }
        }
    }
}
