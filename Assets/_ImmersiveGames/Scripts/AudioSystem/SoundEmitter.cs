using System.Collections;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        public SoundData Data { get; private set; }
        
        private AudioSource _audioSource;
        private Coroutine _playingCoroutine;
        private IAudioService _audioService;
        private bool _isInitialized = false;

        private void Awake()
        {
            _audioSource = gameObject.GetOrAdd<AudioSource>();
        }

        public void Initialize(SoundData data, IAudioService audioService)
        {
            Data = data;
            _audioService = audioService;
            
            _audioSource.clip = data.clip;
            _audioSource.outputAudioMixerGroup = data.mixerGroup;
            _audioSource.volume = data.volume;
            _audioSource.priority = data.priority;
            _audioSource.loop = data.loop;
            _audioSource.playOnAwake = data.playOnAwake;
            
            _isInitialized = true;
        }

        public void Play()
        {
            if (!_isInitialized || _audioService == null)
            {
                Debug.LogError("SoundEmitter não inicializado corretamente");
                return;
            }

            if(_playingCoroutine != null) 
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
            ReturnToPool();
        }

        public void Stop() 
        {
            if (_playingCoroutine != null) 
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }

            _audioSource.Stop();
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _audioService?.ReturnToPool(this);
        }

        public void WithRandomPitch(float min = -0.05f, float max = 0.05f)
        {
            _audioSource.pitch += Random.Range(min, max);
        }

        public void SetSpatialBlend(float spatialBlend)
        {
            _audioSource.spatialBlend = spatialBlend;
        }

        public void SetMaxDistance(float maxDistance)
        {
            _audioSource.maxDistance = maxDistance;
        }

        // Reset do emitter para ser reutilizado
        public void ResetEmitter()
        {
            _audioSource.pitch = 1.0f;
            _audioSource.spatialBlend = 0f;
            _audioSource.maxDistance = 500f;
            _audioSource.Stop();
            
            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }
        }
    }
}
/*
Assets/
└── Resources/
    └── Audio/
        ├── AudioManager.prefab
        └── SoundEmitter.prefab
        
 Audio/
        ├── Prefabs/
        ├── Mixers/
        ├── SoundData/
        └── AudioConfigs/
        */