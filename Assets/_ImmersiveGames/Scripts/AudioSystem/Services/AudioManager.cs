using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
namespace _ImmersiveGames.Scripts.AudioSystem.Services
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmParameter = "BGM_Volume";
        [SerializeField] private string sfxParameter = "SFX_Volume";

        [Header("BGM Settings")]
        [SerializeField] private AudioSource bgmAudioSource; // Arraste um AudioSource dedicado para BGM
        [SerializeField] private float defaultFadeDuration = 2f;

        [Header("Pool Settings")]
        [SerializeField] private SoundEmitter soundEmitterPrefab;
        [SerializeField] private bool collectionCheck = true;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private int maxSoundInstances = 30;

        private IObjectPool<SoundEmitter> _soundEmitterPool;
        private readonly List<SoundEmitter> _activeSoundEmitters = new();
        private readonly Queue<SoundEmitter> _frequentSoundEmitters = new();
        
        private bool _isInitialized = false;
        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBGM;

        public bool IsInitialized => _isInitialized;
        public bool IsBGMPlaying => bgmAudioSource != null && bgmAudioSource.isPlaying;
        public SoundData CurrentBGM => _currentBGM;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            if (DependencyManager.Instance == null)
            {
                DebugUtility.LogError<AudioManager>("DependencyManager não está disponível");
                return;
            }

            InitializePool();
            RegisterServices();
            InitializeBGM();

            _isInitialized = true;
            DebugUtility.LogVerbose<AudioManager>("AudioManager inicializado com sucesso", "green");
        }

        private void InitializeBGM()
        {
            // Garante que temos um AudioSource para BGM
            if (bgmAudioSource == null)
            {
                // Cria um AudioSource dedicado para BGM
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.loop = true;
                bgmAudioSource.spatialBlend = 0f; // 2D
                
                DebugUtility.LogWarning<AudioManager>("BGM AudioSource criado automaticamente");
            }
        }

        private void RegisterServices()
        {
            DependencyManager.Instance.RegisterGlobal<IAudioService>(this);
        }

        #region BGM Implementation
        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            if (bgmData == null || bgmAudioSource == null) return;

            // Para fade coroutine atual se existir
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _currentBGM = bgmData;
            bgmAudioSource.loop = loop;

            // Aplica configurações do SoundData ao AudioSource
            ApplySoundDataToAudioSource(bgmData, bgmAudioSource);

            if (fadeInDuration > 0f)
            {
                _bgmFadeCoroutine = StartCoroutine(FadeBGM(0f, bgmData.volume, fadeInDuration, play: true));
            }
            else
            {
                bgmAudioSource.volume = bgmData.volume;
                bgmAudioSource.Play();
            }

            DebugUtility.LogVerbose<AudioManager>($"BGM iniciado: {bgmData.clip?.name}", "cyan");
        }

        public void StopBGM(float fadeOutDuration = 0f)
        {
            if (bgmAudioSource == null || !bgmAudioSource.isPlaying) return;

            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            if (fadeOutDuration > 0f)
            {
                _bgmFadeCoroutine = StartCoroutine(FadeBGM(bgmAudioSource.volume, 0f, fadeOutDuration, play: false));
            }
            else
            {
                bgmAudioSource.Stop();
                _currentBGM = null;
            }

            DebugUtility.LogVerbose<AudioManager>("BGM parado", "yellow");
        }

        public void PauseBGM()
        {
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Pause();
            }
        }

        public void ResumeBGM()
        {
            if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
            {
                bgmAudioSource.UnPause();
            }
        }

        public void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f)
        {
            if (newBgmData == null) return;

            float halfDuration = fadeDuration * 0.5f;

            // Fade out do BGM atual e fade in do novo
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _bgmFadeCoroutine = StartCoroutine(CrossfadeBGMCoroutine(newBgmData, halfDuration));
        }

        private IEnumerator CrossfadeBGMCoroutine(SoundData newBgmData, float halfDuration)
        {
            float initialVolume = bgmAudioSource.volume;

            // Fade out
            yield return StartCoroutine(FadeBGM(initialVolume, 0f, halfDuration, play: false));

            // Troca para nova música
            _currentBGM = newBgmData;
            ApplySoundDataToAudioSource(newBgmData, bgmAudioSource);
            bgmAudioSource.Play();

            // Fade in
            yield return StartCoroutine(FadeBGM(0f, newBgmData.volume, halfDuration, play: false));

            _bgmFadeCoroutine = null;
        }

        private IEnumerator FadeBGM(float fromVolume, float toVolume, float duration, bool play = false)
        {
            if (play && !bgmAudioSource.isPlaying)
            {
                bgmAudioSource.volume = fromVolume;
                bgmAudioSource.Play();
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                bgmAudioSource.volume = Mathf.Lerp(fromVolume, toVolume, t);
                yield return null;
            }

            bgmAudioSource.volume = toVolume;

            // Se volume chegou a zero, para a música
            if (toVolume <= 0f && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Stop();
                _currentBGM = null;
            }

            _bgmFadeCoroutine = null;
        }

        private void ApplySoundDataToAudioSource(SoundData soundData, AudioSource audioSource)
        {
            audioSource.clip = soundData.clip;
            audioSource.outputAudioMixerGroup = soundData.mixerGroup;
            audioSource.volume = soundData.volume;
            audioSource.priority = soundData.priority;
            audioSource.loop = soundData.loop;
            audioSource.playOnAwake = soundData.playOnAwake;
            audioSource.spatialBlend = soundData.spatialBlend;
            audioSource.maxDistance = soundData.maxDistance;
        }
        #endregion

        #region Volume Control
        public void SetBGMVolume(float volume)
        {
            if (audioMixer != null)
            {
                audioMixer.SetFloat(bgmParameter, LinearToDecibel(volume));
            }

            // Atualiza também o volume local se estiver tocando
            if (bgmAudioSource != null && _currentBGM != null)
            {
                bgmAudioSource.volume = volume * _currentBGM.volume;
            }
        }

        public void SetSfxVolume(float volume)
        {
            if (audioMixer != null)
            {
                audioMixer.SetFloat(sfxParameter, LinearToDecibel(volume));
            }
        }
        #endregion

        
        private void InitializePool()
        {
            if (soundEmitterPrefab == null)
            {
                DebugUtility.LogError<AudioManager>("SoundEmitter prefab não atribuído");
                return;
            }

            _soundEmitterPool = new ObjectPool<SoundEmitter>(
                CreateSoundEmitter,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionCheck,
                defaultCapacity,
                maxPoolSize);

            DebugUtility.LogVerbose<AudioManager>("Pool de SoundEmitters inicializado", "blue");
        }

        #region IAudioService Implementation
        public void PlaySound(SoundData soundData, Vector3 position, AudioConfig config = null)
        {
            if (!_isInitialized)
            {
                DebugUtility.LogWarning<AudioManager>("AudioManager não inicializado");
                return;
            }

            if (soundData == null)
            {
                DebugUtility.LogWarning<AudioManager>("SoundData é nulo");
                return;
            }

            var soundBuilder = CreateSound()
                .WithSoundData(soundData)
                .WithPosition(position);

            // Aplica configurações do AudioConfig
            if (config != null)
            {
                if (config.useSpatialBlend)
                {
                    soundBuilder.WithSpatialBlend(1.0f);
                }
                soundBuilder.WithMaxDistance(config.maxDistance);
            }

            if (soundData.randomPitch)
            {
                soundBuilder.WithRandomPitch();
            }

            soundBuilder.Play();
            
            DebugUtility.LogVerbose<AudioManager>($"Som tocado: {soundData.clip?.name}", "blue");
        }

        public void StopAllSounds()
        {
            foreach (var emitter in _activeSoundEmitters.ToArray())
            {
                if (emitter != null)
                {
                    emitter.Stop();
                }
            }
            _activeSoundEmitters.Clear();
            _frequentSoundEmitters.Clear();
        }
        #endregion

        #region IAudioManager Implementation
        public SoundBuilder CreateSound() => new SoundBuilder(this);

        public bool CanPlaySound(SoundData data)
        {
            if (data == null) return false;

            if (!data.frequentSound) 
                return true;

            // Gerencia sons frequentes
            if (_frequentSoundEmitters.Count >= maxSoundInstances &&
                _frequentSoundEmitters.TryDequeue(out var soundEmitter))
            {
                try
                {
                    if (soundEmitter != null)
                    {
                        soundEmitter.Stop();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    DebugUtility.LogWarning<AudioManager>($"Erro ao parar soundEmitter: {e.Message}");
                    return false;
                }
            }
            return true;
        }

        public void RegisterFrequentSound(SoundEmitter soundEmitter)
        {
            if (soundEmitter != null && soundEmitter.Data != null && soundEmitter.Data.frequentSound)
            {
                _frequentSoundEmitters.Enqueue(soundEmitter);
            }
        }

        public SoundEmitter Get() 
        { 
            if (_soundEmitterPool == null)
            {
                DebugUtility.LogError<AudioManager>("Pool de sound emitters não inicializado");
                return null;
            }
            return _soundEmitterPool.Get(); 
        }

        public void ReturnToPool(SoundEmitter soundEmitter)
        {
            if (soundEmitter == null) return;
            _soundEmitterPool?.Release(soundEmitter);
        }
        #endregion

        #region Pool Management
        private SoundEmitter CreateSoundEmitter()
        {
            var soundEmitter = Instantiate(soundEmitterPrefab);
            soundEmitter.gameObject.SetActive(false);
            return soundEmitter;
        }

        private void OnTakeFromPool(SoundEmitter soundEmitter)
        {
            soundEmitter.gameObject.SetActive(true);
            _activeSoundEmitters.Add(soundEmitter);
            DebugUtility.LogVerbose<AudioManager>("SoundEmitter retirado do pool", "green");
        }

        private void OnReturnedToPool(SoundEmitter soundEmitter)
        {
            if (soundEmitter == null) return;

            soundEmitter.gameObject.SetActive(false);
            soundEmitter.ResetEmitter();
            _activeSoundEmitters.Remove(soundEmitter);
            DebugUtility.LogVerbose<AudioManager>("SoundEmitter devolvido ao pool", "yellow");
        }

        private void OnDestroyPoolObject(SoundEmitter soundEmitter)
        {
            if (soundEmitter != null)
            {
                Destroy(soundEmitter.gameObject);
            }
        }
        #endregion
        
        private float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f)
                return -80f;
            return Mathf.Log10(linear) * 20;
        }

        private void OnDestroy()
        {
            StopAllSounds();
            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
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
        */