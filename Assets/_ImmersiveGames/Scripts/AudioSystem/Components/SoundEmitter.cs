using System.Collections;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : PooledObject
    {
        public SoundData Data { get; private set; }

        private AudioSource _audioSource;
        private Coroutine _playingCoroutine;
        private IAudioService _audioManager;
        private bool _isInitialized;

        private float _volumeMultiplier = 1f;

        protected override void OnConfigured(PoolableObjectData config, IActor spawner)
        {
            // Nada especial aqui — PooledObject.Configure já tratou posicionamento/desativação
            _audioSource = gameObject.GetOrAdd<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            // Nada extra para ativação — Initialize deve ser chamado externamente antes de Play
        }

        protected override void OnDeactivated()
        {
            // Reset já chamado em PoolableReset/Deactivate do base
        }

        protected override void OnReset()
        {
            ResetEmitter();
        }

        protected override void OnReconfigured(PoolableObjectData config)
        {
            // se precisarmos aplicar novas configs específicas, aqui
        }

        public void Initialize(SoundData data, IAudioService audioManager)
        {
            Data = data;
            _audioManager = audioManager;

            if (_audioSource == null)
                _audioSource = gameObject.GetOrAdd<AudioSource>();

            ApplySoundData(data);
            _isInitialized = true;
        }

        public void Play()
        {
            if (!_isInitialized)
            {
#if UNITY_EDITOR
                Debug.LogError($"SoundEmitter '{name}' não inicializado corretamente.");
#endif
                ReturnToPoolIfPossible();
                return;
            }

            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
            }

            _audioSource.Play();

            if (!Data.loop)
            {
                _playingCoroutine = StartCoroutine(WaitForSoundToEnd());
            }
        }

        private IEnumerator WaitForSoundToEnd()
        {
            yield return new WaitWhile(() => _audioSource.isPlaying);
            ReturnToPoolIfPossible();
        }

        public void Stop()
        {
            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }

            _audioSource.Stop();
            ReturnToPoolIfPossible();
        }

        private void ReturnToPoolIfPossible()
        {
            // Use PooledObject.GetPool from base
            var pool = GetPool;
            if (pool != null)
            {
                pool.ReturnObject(this);
            }
            else
            {
                // sem pool: apenas desativa
                gameObject.SetActive(false);
            }
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            if (_audioSource != null)
                _audioSource.pitch = 1f + Random.Range(min, max);
        }

        public void SetSpatialBlend(float spatialBlend)
        {
            if (_audioSource != null)
                _audioSource.spatialBlend = spatialBlend;
        }

        public void SetMaxDistance(float maxDistance)
        {
            if (_audioSource != null)
                _audioSource.maxDistance = maxDistance;
        }

        public void SetVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = multiplier;
            if (_audioSource != null && Data != null)
            {
                _audioSource.volume = Data.volume * _volumeMultiplier;
            }
        }

        public void SetMixerGroup(UnityEngine.Audio.AudioMixerGroup mixerGroup)
        {
            if (_audioSource != null && mixerGroup != null)
                _audioSource.outputAudioMixerGroup = mixerGroup;
        }

        public void ResetEmitter()
        {
            if (_audioSource != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.spatialBlend = 0f;
                _audioSource.maxDistance = 500f;
                _audioSource.Stop();
            }

            _volumeMultiplier = 1f;

            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }
        }

        private void ApplySoundData(SoundData data)
        {
            if (_audioSource == null || data == null) return;

            _audioSource.clip = data.clip;
            _audioSource.outputAudioMixerGroup = data.mixerGroup;
            _audioSource.volume = data.volume * _volumeMultiplier;
            _audioSource.priority = data.priority;
            _audioSource.loop = data.loop;
            _audioSource.playOnAwake = data.playOnAwake;
            _audioSource.spatialBlend = data.spatialBlend;
            _audioSource.maxDistance = data.maxDistance;
        }
    }
}
