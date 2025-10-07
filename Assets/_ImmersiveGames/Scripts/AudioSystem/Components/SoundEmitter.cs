using System.Collections;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour, IPoolable
    {
        public SoundData Data { get; private set; }
        
        private AudioSource _audioSource;
        private Coroutine _playingCoroutine;
        private IAudioService _audioManager;
        private bool _isInitialized = false;

        // IPoolable fields
        private PoolableObjectData _config;
        private ObjectPool _pool;
        private IActor _spawner;
        
        private float _volumeMultiplier = 1f;

        #region IPoolable Implementation
        public void Configure(PoolableObjectData config, ObjectPool pool, IActor spawner = null)
        {
            _config = config;
            _pool = pool;
            _spawner = spawner;
            
            _audioSource = gameObject.GetOrAdd<AudioSource>();
            gameObject.SetActive(false);
        }

        public void Activate(Vector3 position, Vector3? direction = null, IActor spawner = null)
        {
            transform.position = position;
            gameObject.SetActive(true);
            
            if (spawner != null)
                _spawner = spawner;
        }

        public void Deactivate()
        {
            ResetEmitter();
            gameObject.SetActive(false);
        }

        public void PoolableReset()
        {
            ResetEmitter();
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            gameObject.SetActive(false);
            _spawner = null;
        }

        public void Reconfigure(PoolableObjectData config)
        {
            _config = config;
        }

        public GameObject GetGameObject() => gameObject;

        public T GetData<T>() where T : PoolableObjectData => _config as T;
        #endregion

        #region SoundEmitter Specific Methods
        public void Initialize(SoundData data, IAudioService audioManager)
        {
            Data = data;
            _audioManager = audioManager;
            
            ApplySoundData(data);
            _isInitialized = true;
        }

        public void Play()
        {
            if (!_isInitialized || _audioManager == null)
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
            _pool?.ReturnObject(this);
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
        public void SetVolumeMultiplier(float multiplier)
        {
            _volumeMultiplier = multiplier;
            if (_audioSource != null)
            {
                _audioSource.volume = Data.volume * _volumeMultiplier;
            }
        }

        public void ResetEmitter()
        {
            _audioSource.pitch = 1.0f;
            _audioSource.spatialBlend = 0f;
            _audioSource.maxDistance = 500f;
            _volumeMultiplier = 1f; // Reset do multiplier
            _audioSource.Stop();
            
            if (_playingCoroutine != null)
            {
                StopCoroutine(_playingCoroutine);
                _playingCoroutine = null;
            }
        }

        private void ApplySoundData(SoundData data)
        {
            _audioSource.clip = data.clip;
            _audioSource.outputAudioMixerGroup = data.mixerGroup;
            _audioSource.volume = data.volume * _volumeMultiplier; // Aplica multiplier aqui
            _audioSource.priority = data.priority;
            _audioSource.loop = data.loop;
            _audioSource.playOnAwake = data.playOnAwake;
            _audioSource.spatialBlend = data.spatialBlend;
            _audioSource.maxDistance = data.maxDistance;
        }
        #endregion
    }
}