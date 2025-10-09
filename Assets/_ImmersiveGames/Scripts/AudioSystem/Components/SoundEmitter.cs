using System.Collections;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.Audio;
using UnityUtils;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : PooledObject
    {
        private SoundData Data { get; set; }

        private AudioSource _audioSource;
        private Coroutine _playingCoroutine;
        private ObjectPool _poolRef;

        private float _volumeMultiplier = 1f;

        public void Initialize(SoundData data)
        {
            Data = data;
            if (_audioSource == null) _audioSource = gameObject.GetOrAdd<AudioSource>();
            ApplySoundData(data);
        }

        protected override void OnConfigured(PoolableObjectData config, IActor spawner) 
        {
            _poolRef = GetPool;
        }

        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner) { /* optional */ }
        protected override void OnDeactivated() { /* optional */ }
        protected override void OnReset() { /* optional */ }
        protected override void OnReconfigured(PoolableObjectData config) { /* optional */ }

        public void Play()
        {
            if (_audioSource == null || Data == null) return;

            if (_playingCoroutine != null) { StopCoroutine(_playingCoroutine); _playingCoroutine = null; }

            _audioSource.Play();

            if (!Data.loop)
            {
                _playingCoroutine = StartCoroutine(WaitForSoundToEnd());
            }
        }

        public void PlayWithFade(float targetMultiplier, float duration)
        {
            if (_audioSource == null || Data == null)
            {
                Play();
                return;
            }

            if (_playingCoroutine != null) { StopCoroutine(_playingCoroutine); _playingCoroutine = null; }
            StartCoroutine(PlayAndFadeRoutine(targetMultiplier, duration));
        }

        private IEnumerator PlayAndFadeRoutine(float targetMultiplier, float duration)
        {
            SetVolumeMultiplier(0f);
            _audioSource.Play();

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                SetVolumeMultiplier(Mathf.Lerp(0f, targetMultiplier, t));
                yield return null;
            }
            SetVolumeMultiplier(targetMultiplier);

            if (!Data.loop)
            {
                _playingCoroutine = StartCoroutine(WaitForSoundToEnd());
            }
        }

        public void Stop()
        {
            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }
            _audioSource.Stop();
            _poolRef?.ReturnObject(this);
        }

        private IEnumerator WaitForSoundToEnd()
        {
            yield return new WaitWhile(() => _audioSource.isPlaying);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _poolRef?.ReturnObject(this);
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            if (_audioSource != null)
                _audioSource.pitch = Random.Range(1f + min, 1f + max);
        }

        public void SetSpatialBlend(float spatialBlend) { if (_audioSource != null) _audioSource.spatialBlend = spatialBlend; }
        public void SetMaxDistance(float maxDistance) { if (_audioSource != null) _audioSource.maxDistance = maxDistance; }
        public void SetMixerGroup(AudioMixerGroup group) { if (_audioSource != null) _audioSource.outputAudioMixerGroup = group; }

        public void SetVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = multiplier;
            if (_audioSource != null && Data != null)
            {
                _audioSource.volume = Data.volume * _volumeMultiplier;
            }
        }

        private void ApplySoundData(SoundData data)
        {
            if (_audioSource == null || data == null) return;
            _audioSource.clip = data.clip;
            _audioSource.outputAudioMixerGroup = data.mixerGroup;
            _audioSource.priority = data.priority;
            _audioSource.loop = data.loop;
            _audioSource.playOnAwake = data.playOnAwake;
            _audioSource.spatialBlend = data.spatialBlend;
            _audioSource.maxDistance = data.maxDistance;
            _audioSource.volume = data.volume * _volumeMultiplier; // small note: ensure _audio_source variable name consistent
            _audioSource.pitch = 1f;
        }
    }
}
