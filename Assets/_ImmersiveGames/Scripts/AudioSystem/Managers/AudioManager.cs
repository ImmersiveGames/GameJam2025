using System;
using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Pool;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.Audio;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmParameter = "BGM_Volume";
        [SerializeField] private string sfxParameter = "SFX_Volume";

        [Header("BGM Settings")]
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField] private float defaultFadeDuration = 2f;

        [Header("Pool Settings")]
        [SerializeField] private SoundEmitterPoolData soundEmitterPoolData;

        private ObjectPool _soundEmitterPool;
        private readonly Queue<SoundEmitter> _frequentSoundEmitters = new();
        
        private bool _isInitialized = false;
        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBGM;

        public bool IsInitialized => _isInitialized;
        public bool IsBGMPlaying => bgmAudioSource != null && bgmAudioSource.isPlaying;
        public SoundData CurrentBGM => _currentBGM;
        private int MaxSoundInstances => soundEmitterPoolData?.MaxSoundInstances ?? 30;

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
            if (!InitializePool())
            {
                DebugUtility.LogError<AudioManager>("Falha na inicialização do pool");
                return;
            }
            
            RegisterServices();
            InitializeBGM();

            _isInitialized = true;
            DebugUtility.LogVerbose<AudioManager>("AudioManager inicializado com sucesso", "green");
        }

        private bool InitializePool()
        {
            if (soundEmitterPoolData == null)
            {
                DebugUtility.LogError<AudioManager>("SoundEmitterPoolData não atribuído");
                return false;
            }

            // Registra o pool no PoolManager
            PoolManager.Instance.RegisterPool(soundEmitterPoolData);
            _soundEmitterPool = PoolManager.Instance.GetPool(soundEmitterPoolData.ObjectName);

            if (_soundEmitterPool == null)
            {
                DebugUtility.LogError<AudioManager>("Falha ao obter pool de SoundEmitter");
                return false;
            }

            DebugUtility.LogVerbose<AudioManager>($"Pool de SoundEmitter inicializado: {soundEmitterPoolData.ObjectName}", "blue");
            return true;
        }

        private void RegisterServices()
        {
            DependencyManager.Instance.RegisterGlobal<IAudioService>(this);
        }

        private void InitializeBGM()
        {
            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.loop = true;
                bgmAudioSource.spatialBlend = 0f;
                DebugUtility.LogWarning<AudioManager>("BGM AudioSource criado automaticamente");
            }
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

        public void SetBGMVolume(float volume)
        {
            if (audioMixer != null)
            {
                audioMixer.SetFloat(bgmParameter, LinearToDecibel(volume));
            }

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

        public void StopAllSounds()
        {
            // O PoolManager cuida de parar todos os sons ativos
            // Podemos expandir isso se necessário
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
            if (_frequentSoundEmitters.Count >= MaxSoundInstances  &&
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

            var poolable = _soundEmitterPool.GetObject(Vector3.zero, null, null, false);
            return poolable as SoundEmitter;
        }

        public void ReturnToPool(SoundEmitter soundEmitter)
        {
            if (soundEmitter == null) return;
            _soundEmitterPool?.ReturnObject(soundEmitter);
        }
        #endregion

        #region BGM Implementation
        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            if (bgmData == null || bgmAudioSource == null) return;

            if (_bgmFadeCoroutine != null)
            {
                StopCoroutine(_bgmFadeCoroutine);
            }

            _currentBGM = bgmData;
            bgmAudioSource.loop = loop;

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

        private float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f)
                return -80f;
            return Mathf.Log10(linear) * 20;
        }

        private void OnDestroy()
        {
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