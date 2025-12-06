using System.Collections.Generic;
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

            var template = LoadPoolTemplate();
            var poolData = template != null
                ? ScriptableObject.Instantiate(template)
                : ScriptableObject.CreateInstance<SoundEmitterPoolData>();

            ConfigurePoolData(poolData, template, sound, poolableData);

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

        private static SoundEmitterPoolData LoadPoolTemplate()
        {
            return Resources.Load<SoundEmitterPoolData>("Audio/SoundEmitters/PD_SoundEmitter");
        }

        private static void ConfigurePoolData(
            SoundEmitterPoolData poolData,
            SoundEmitterPoolData template,
            SoundData sound,
            SoundEmitterPoolableData poolable)
        {
            var initialPoolSize = Mathf.Max(template?.InitialPoolSize ?? 3, 1);
            var maxSoundInstances = Mathf.Max(template?.MaxSoundInstances ?? 30, initialPoolSize);
            var config = new PoolDataConfig
            {
                objectName = sound.name ?? sound.clip?.name ?? template?.ObjectName ?? "SoundEmitter",
                initialPoolSize = initialPoolSize,
                canExpand = template?.CanExpand ?? true,
                objectConfigs = new PoolableObjectData[] { poolable },
                reconfigureOnReturn = template?.ReconfigureOnReturn ?? false,
                maxSoundInstances = maxSoundInstances
            };

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(config), poolData);
        }

        private static void ConfigurePoolableData(SoundEmitterPoolableData poolable, SoundData sound, GameObject prefab)
        {
            float fallbackLifetime = Mathf.Max(sound.clip != null ? sound.clip.length : 1f, 0.1f);
            var config = new PoolableObjectConfig
            {
                objectName = sound.name ?? sound.clip?.name ?? "SoundEmitter",
                prefab = prefab,
                lifetime = fallbackLifetime
            };

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(config), poolable);
        }

        [System.Serializable]
        private class PoolDataConfig
        {
            public string objectName;
            public int initialPoolSize;
            public bool canExpand;
            public PoolableObjectData[] objectConfigs;
            public bool reconfigureOnReturn;
            public int maxSoundInstances;
        }

        [System.Serializable]
        private class PoolableObjectConfig
        {
            public string objectName;
            public GameObject prefab;
            public float lifetime;
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
