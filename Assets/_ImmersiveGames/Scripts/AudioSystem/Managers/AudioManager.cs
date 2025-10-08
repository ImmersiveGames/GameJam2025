using System.Collections;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Pool;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Serviço de áudio que gerencia pool de emitters, BGM e mixer.
    /// Substitui / consolida o AudioManager anterior.
    /// </summary>
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmParameter = "BGM_Volume";
        [SerializeField] private string sfxParameter = "SFX_Volume";

        [Header("Pool Settings")]
        [SerializeField] private SoundEmitterPoolData soundEmitterPoolData;
        [SerializeField] private AudioServiceSettings settings;

        private ObjectPool _emitterPool;
        private readonly Queue<SoundEmitter> _frequentEmitters = new();

        internal AudioSource _bgmSource; // Novo: Fonte dedicada para BGM
        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBGM;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>(); // Inicializa fonte dedicada
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true; // Default para BGM
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized) return;
            if (DependencyManager.Instance == null)
            {
                DebugUtility.LogError<AudioManager>("DependencyManager não disponível");
                return;
            }

            if (!InitializePool()) return;

            DependencyManager.Instance.RegisterGlobal<IAudioService>(this);

            IsInitialized = true;
            DebugUtility.LogVerbose<AudioManager>("AudioManager inicializado", "green");
        }

        private bool InitializePool()
        {
            if (soundEmitterPoolData == null)
            {
                DebugUtility.LogError<AudioManager>("SoundEmitterPoolData não configurado");
                return false;
            }

            PoolManager.Instance.RegisterPool(soundEmitterPoolData);
            _emitterPool = PoolManager.Instance.GetPool(soundEmitterPoolData.ObjectName);

            if (_emitterPool == null)
            {
                DebugUtility.LogError<AudioManager>("Pool de emitters não encontrado");
                return false;
            }

            DebugUtility.LogVerbose<AudioManager>($"Pool '{soundEmitterPoolData.ObjectName}' inicializado", "blue");
            return true;
        }

        #region IAudioService Implementation

        public void PlaySound(SoundData soundData, AudioContext context, AudioConfig config = null)
        {
            if (!IsInitialized) return;
            if (soundData == null || soundData.clip == null) return;

            if (!CanPlaySound(soundData)) return;

            var emitterObj = _emitterPool.GetObject(context.position, null, null, false) as SoundEmitter;
            if (emitterObj == null)
            {
                DebugUtility.LogWarning<AudioManager>("Pool não retornou SoundEmitter");
                return;
            }

            emitterObj.Initialize(soundData, this);
            emitterObj.SetSpatialBlend(context.useSpatial ? soundData.spatialBlend : 0f);
            emitterObj.SetMaxDistance(config != null ? config.maxDistance : soundData.maxDistance);
            emitterObj.SetVolumeMultiplier(context.volumeMultiplier);

            if (soundData.randomPitch)
                emitterObj.WithRandomPitch(-soundData.pitchVariation, soundData.pitchVariation);

            emitterObj.Activate(context.position);
            emitterObj.Play();

            if (settings != null && settings.debugEmitters)
                Debug.Log($"[AudioManager] Emitting '{soundData.clip?.name}' at {context.position}");
        }

        public void SetSfxVolume(float volume)
        {
            if (audioMixer != null)
                audioMixer.SetFloat(sfxParameter, LinearToDecibel(volume));
        }

        public void StopAllSounds()
        {
            // Implementation: iterate active emitters via pool or keep references. For now rely on pool.
        }

        // Novo: Implementação completa para BGM
        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            if (!IsInitialized || bgmData == null || bgmData.clip == null) return;
            if (!CanPlaySound(bgmData)) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            ApplyBgmData(bgmData, loop);

            if (fadeInDuration > 0)
            {
                _bgmSource.volume = 0f;
                _bgmSource.Play(); // Novo: Garantir play antes de fade in
                _bgmFadeCoroutine = StartCoroutine(FadeVolume(0f, bgmData.volume, fadeInDuration));
            }
            else
            {
                _bgmSource.volume = bgmData.volume;
                _bgmSource.Play();
            }

            _currentBGM = bgmData;
            DebugUtility.LogVerbose<AudioManager>($"BGM '{bgmData.clip.name}' started with loop: {loop}, playing: {_bgmSource.isPlaying}", "green");
        }

        public void StopBGM(float fadeOutDuration = 0f)
        {
            if (_bgmSource == null || !_bgmSource.isPlaying) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            if (fadeOutDuration > 0)
            {
                _bgmFadeCoroutine = StartCoroutine(FadeVolume(_bgmSource.volume, 0f, fadeOutDuration, stopAfterFade: true));
            }
            else
            {
                _bgmSource.Stop();
            }

            _currentBGM = null;
            DebugUtility.LogVerbose<AudioManager>("BGM stopped", "green");
        }

        public void PauseBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.Pause();
                DebugUtility.LogVerbose<AudioManager>("BGM paused", "green");
            }
        }

        public void ResumeBGM()
        {
            if (_bgmSource != null && !_bgmSource.isPlaying && _currentBGM != null)
            {
                _bgmSource.Play();
                DebugUtility.LogVerbose<AudioManager>("BGM resumed", "green");
            }
        }

        public void SetBGMVolume(float volume)
        {
            if (audioMixer != null)
            {
                audioMixer.SetFloat(bgmParameter, LinearToDecibel(volume));
                DebugUtility.LogVerbose<AudioManager>($"BGM mixer volume set to {volume} (dB: {LinearToDecibel(volume)})", "blue"); // Novo debug
            }
        }

        public void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f)
        {
            if (!IsInitialized || newBgmData == null || newBgmData.clip == null) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            _bgmFadeCoroutine = StartCoroutine(CrossfadeCoroutine(newBgmData, fadeDuration));
        }

        private IEnumerator CrossfadeCoroutine(SoundData newBgmData, float duration)
        {
            float startVolume = _bgmSource.volume;
            float elapsed = 0f;

            ApplyBgmData(newBgmData, true);
            _bgmSource.volume = 0f;
            _bgmSource.Play();
            DebugUtility.LogVerbose<AudioManager>($"Crossfade started: new clip '{newBgmData.clip.name}', current playing: {_bgmSource.isPlaying}", "blue"); // Novo debug

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _bgmSource.volume = Mathf.Lerp(0f, newBgmData.volume, t);
                yield return null;
            }

            _bgmSource.volume = newBgmData.volume;
            _currentBGM = newBgmData;
            DebugUtility.LogVerbose<AudioManager>($"Crossfade to '{newBgmData.clip.name}' completed, volume: {_bgmSource.volume}", "green");
        }

        private IEnumerator FadeVolume(float start, float end, float duration, bool stopAfterFade = false)
        {
            _bgmSource.volume = start;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _bgmSource.volume = Mathf.Lerp(start, end, elapsed / duration);
                DebugUtility.LogVerbose<AudioManager>($"Fade progress: volume = {_bgmSource.volume} (elapsed {elapsed}/{duration}), playing: {_bgmSource.isPlaying}", "blue"); // Debug aprimorado
                yield return null;
            }
            _bgmSource.volume = end;

            if (stopAfterFade && end == 0f)
            {
                _bgmSource.Stop();
                DebugUtility.LogVerbose<AudioManager>($"Fade complete, stopped BGM, playing: {_bgmSource.isPlaying}", "blue");
            }
        }

        private void ApplyBgmData(SoundData data, bool loop)
        {
            _bgmSource.clip = data.clip;
            _bgmSource.outputAudioMixerGroup = data.mixerGroup;
            _bgmSource.priority = data.priority;
            _bgmSource.loop = loop;
            _bgmSource.volume = data.volume; // Inicial, ajustado por fade/mixer
            _bgmSource.spatialBlend = 0f; // BGM tipicamente não espacial
            DebugUtility.LogVerbose<AudioManager>($"Applied BGM data: clip '{data.clip.name}', mixerGroup '{data.mixerGroup?.name ?? "None"}', volume {data.volume}", "blue"); // Novo debug
        }

        public bool CanPlaySound(SoundData soundData)
        {
            if (soundData == null) return false;
            if (!soundData.frequentSound) return true;

            // manage frequent emitters limit (simplified)
            if (_frequentEmitters.Count >= (soundEmitterPoolData?.MaxSoundInstances ?? 30))
            {
                var e = _frequentEmitters.Dequeue();
                try { e.Stop(); } catch { /*ignored*/ }
            }

            return true;
        }

        public SoundEmitter GetEmitterFromPool()
        {
            var obj = _emitterPool?.GetObject(Vector3.zero, null, null, false);
            return obj as SoundEmitter;
        }

        public void ReturnEmitterToPool(SoundEmitter emitter)
        {
            _emitterPool?.ReturnObject(emitter);
        }

        public void RegisterFrequentSound(SoundEmitter emitter)
        {
            if (emitter != null && emitter.Data != null && emitter.Data.frequentSound)
                _frequentEmitters.Enqueue(emitter);
        }
        #endregion

        private float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f) return -80f;
            return Mathf.Log10(linear) * 20f;
        }
    }
}