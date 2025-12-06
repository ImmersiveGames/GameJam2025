using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Pool;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Serviço global de SFX baseado em pooling de <see cref="SoundEmitter"/>.
    /// Mantém um pool por SoundData e retorna handles seguros para controlar a reprodução.
    /// </summary>
    public class AudioSfxService : IAudioSfxService
    {
        private readonly IAudioVolumeService _volumeService;
        private readonly AudioServiceSettings _serviceSettings;
        private readonly AudioConfig _defaultConfig;

        private readonly Dictionary<SoundData, ObjectPool> _pools = new();

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

            var pool = GetOrCreatePool(sound);
            if (pool == null)
                return NullHandle;

            var poolable = pool.GetObject(context.position, null, null, false) as SoundEmitter;
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

        private ObjectPool GetOrCreatePool(SoundData sound)
        {
            if (_pools.TryGetValue(sound, out var existing) && existing != null)
                return existing;

            var poolRoot = AudioRuntimeRoot.Root;
            if (poolRoot == null)
                return null;

            var poolGo = new GameObject($"SfxPool_{sound.name ?? sound.clip?.name ?? "SoundEmitter"}");
            poolGo.transform.SetParent(poolRoot, false);

            var pool = poolGo.AddComponent<ObjectPool>();
            var poolData = CreatePoolData(sound);
            if (poolData == null)
            {
                Object.Destroy(poolGo);
                return null;
            }

            pool.SetData(poolData);
            pool.Initialize();

            _pools[sound] = pool;
            return pool;
        }

        private SoundEmitterPoolData CreatePoolData(SoundData sound)
        {
            var poolableData = ScriptableObject.CreateInstance<SoundEmitterPoolableData>();
            var emitterPrefab = LoadEmitterPrefab();
            if (emitterPrefab == null)
                return null;

            ConfigurePoolableData(poolableData, sound, emitterPrefab);

            var poolData = ScriptableObject.CreateInstance<SoundEmitterPoolData>();
            ConfigurePoolData(poolData, sound, poolableData);

            return poolData;
        }

        private static GameObject LoadEmitterPrefab()
        {
            var prefab = Resources.Load<GameObject>("Audio/Prefabs/SoundEmitter");
            if (prefab != null)
                return prefab;

            var fallback = new GameObject("SoundEmitterPrefab");
            fallback.AddComponent<AudioSource>();
            fallback.AddComponent<SoundEmitter>();
            fallback.SetActive(false);
            fallback.hideFlags = HideFlags.HideAndDontSave;
            return fallback;
        }

        private static void ConfigurePoolData(SoundEmitterPoolData poolData, SoundData sound, SoundEmitterPoolableData poolable)
        {
            SetField(poolData, "objectName", sound.name ?? sound.clip?.name ?? "SoundEmitter");
            SetField(poolData, "initialPoolSize", 3);
            SetField(poolData, "canExpand", true);
            SetField(poolData, "objectConfigs", new PoolableObjectData[] { poolable });
            SetField(poolData, "reconfigureOnReturn", false);
            SetField(poolData, "maxSoundInstances", poolData.MaxSoundInstances > 0 ? poolData.MaxSoundInstances : 30);
        }

        private static void ConfigurePoolableData(SoundEmitterPoolableData poolable, SoundData sound, GameObject prefab)
        {
            SetField(poolable, "objectName", sound.name ?? sound.clip?.name ?? "SoundEmitter");
            SetField(poolable, "prefab", prefab);
            SetField(poolable, "lifetime", 0f);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
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
