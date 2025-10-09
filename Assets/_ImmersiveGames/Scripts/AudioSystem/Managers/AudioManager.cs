using System.Collections;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Audio;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// AudioManager focado em BGM + mixer + one-shot fallback PlaySound (sem gerenciar pools).
    /// Pools e SFX frequentes devem ser responsabilidade dos Controllers (cada controller pode criar seu pool local).
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class AudioManager : MonoBehaviour, IAudioService
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string bgmParameter = "BGM_Volume";

        [Header("BGM")]
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private AudioSource bgmAudioSource;

        [Header("Service settings (optional)")]
        [SerializeField] private AudioServiceSettings settings;

        // injected
        private IAudioMathService _math;

        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBgm;
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (IsInitialized) return;

            // DI: obter math (AudioSystemInitializer garante registro)
            if (DependencyManager.Instance != null)
                DependencyManager.Instance.TryGetGlobal(out _math);

            DependencyManager.Instance?.RegisterGlobal<IAudioService>(this);

            // torna settings disponível via DI se for configurado
            if (settings != null)
            {
                DependencyManager.Instance?.RegisterGlobal(settings);
            }

            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.loop = true;
                bgmAudioSource.spatialBlend = 0f;
            }

            IsInitialized = true;
            DebugUtility.LogVerbose<AudioManager>("AudioManager (BGM) inicializado", "green");
        }

        #region BGM

        public AudioSource BgmAudioSource => bgmAudioSource; // getter útil para debug/tests

        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            if (!IsInitialized || bgmData == null || bgmData.clip == null) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            ApplyBgmData(bgmData, loop);

            // calcula volume final pelo math (se math disponível), caso contrário aplica básico
            float master = settings != null ? settings.masterVolume : 1f;
            float catVol = settings != null ? settings.bgmVolume : 1f;
            float catMul = settings != null ? settings.bgmMultiplier : 1f;
            float ctxMul = 1f;

            float targetVolume = _math?.CalculateFinalVolume(bgmData.volume, 1f, catVol, catMul, master, ctxMul) ?? Mathf.Clamp01(bgmData.volume * catVol * master);

            if (fadeInDuration > 0f)
            {
                bgmAudioSource.volume = 0f;
                bgmAudioSource.Play();
                _bgmFadeCoroutine = StartCoroutine(FadeVolume(bgmAudioSource, 0f, targetVolume, fadeInDuration));
            }
            else
            {
                bgmAudioSource.volume = targetVolume;
                bgmAudioSource.Play();
            }

            _currentBgm = bgmData;
            DebugUtility.LogVerbose<AudioManager>($"BGM iniciado: {bgmData.clip?.name} (targetVolume={targetVolume:F3})", "cyan");
        }

        public void StopBGM(float fadeOutDuration = 0f)
        {
            if (bgmAudioSource == null || !bgmAudioSource.isPlaying) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            if (fadeOutDuration > 0f)
                _bgmFadeCoroutine = StartCoroutine(FadeVolume(bgmAudioSource, bgmAudioSource.volume, 0f, fadeOutDuration, true));
            else
                bgmAudioSource.Stop();

            _currentBgm = null;
            DebugUtility.LogVerbose<AudioManager>("BGM parado", "yellow");
        }
        public void StopBGMImmediate()
        {
            bgmAudioSource.Stop();
            DebugUtility.LogVerbose<AudioManager>("BGM parado", "yellow");
        }

        public void PauseBGM() { if (bgmAudioSource != null && bgmAudioSource.isPlaying) bgmAudioSource.Pause(); }
        public void ResumeBGM() { if (bgmAudioSource != null && !bgmAudioSource.isPlaying && _currentBgm != null) bgmAudioSource.Play(); }

        public void SetBGMVolume(float volume)
        {
            if (audioMixer != null) audioMixer.SetFloat(bgmParameter, _math?.ToDecibels(volume) ?? 20f * Mathf.Log10(Mathf.Max(volume, 0.0001f)));
            else if (bgmAudioSource != null) bgmAudioSource.volume = Mathf.Clamp01(volume);
        }

        public void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f)
        {
            if (newBgmData == null || newBgmData.clip == null) return;
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(CrossfadeRoutine(newBgmData, fadeDuration));
        }

        private IEnumerator CrossfadeRoutine(SoundData newBgmData, float duration)
        {
            float half = duration * 0.5f;
            float old = bgmAudioSource.volume;
            yield return StartCoroutine(FadeVolume(bgmAudioSource, old, 0f, half));
            ApplyBgmData(newBgmData, true);
            bgmAudioSource.volume = 0f;
            bgmAudioSource.Play();
            float master = settings != null ? settings.masterVolume : 1f;
            float catVol = settings != null ? settings.bgmVolume : 1f;
            float catMul = settings != null ? settings.bgmMultiplier : 1f;
            float target = _math?.CalculateFinalVolume(newBgmData.volume, 1f, catVol, catMul, master, 1f) ?? newBgmData.volume * catVol * master;
            yield return StartCoroutine(FadeVolume(bgmAudioSource, 0f, target, half));
            _currentBgm = newBgmData;
        }

        private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration, bool stopAfter = false)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            src.volume = to;
            if (stopAfter && to <= 0f) src.Stop();
        }

        private void ApplyBgmData(SoundData data, bool loop)
        {
            bgmAudioSource.clip = data.clip;
            bgmAudioSource.outputAudioMixerGroup = data.mixerGroup ?? bgmMixerGroup;
            bgmAudioSource.loop = loop;
            bgmAudioSource.priority = data.priority;
            bgmAudioSource.spatialBlend = 0f;
        }

        #endregion

        #region One-shot PlaySound (fallback, no pool)

        /// <summary>
        /// Conveniência: PlaySound rápido usando AudioManager (sem pool). Controllers que possuem pool devem usar pool local.
        /// </summary>
        public void PlaySound(SoundData soundData, AudioContext context, AudioConfig config = null)
        {
            if (!IsInitialized || soundData == null || soundData.clip == null) return;

            // obter math e settings via DI (se não já injetados)
            if (_math == null && DependencyManager.Instance != null)
                DependencyManager.Instance.TryGetGlobal(out _math);

            var settingsLocal = DependencyManager.Instance != null && DependencyManager.Instance.TryGetGlobal<AudioServiceSettings>(out var s) ? s : settings;

            float master = settingsLocal != null ? settingsLocal.masterVolume : 1f;
            float categoryVol = settingsLocal != null ? settingsLocal.sfxVolume : 1f;
            float categoryMul = settingsLocal != null ? settingsLocal.sfxMultiplier : 1f;
            float configDefault = config != null ? config.defaultVolume : 1f;
            float ctxMul = context.volumeMultiplier;
            float overrideVol = context.volumeOverride;

            float finalVol = _math?.CalculateFinalVolume(soundData.volume, configDefault, categoryVol, categoryMul, master, ctxMul, overrideVol) ?? Mathf.Clamp01(soundData.volume * configDefault * categoryVol * master * ctxMul);

            // cria AudioSource temporário (usa mixer-group do SoundData ou do config)
            var go = new GameObject($"OneShot_{soundData.clip.name}");
            go.transform.position = context.position;
            var src = go.AddComponent<AudioSource>();
            src.clip = soundData.clip;
            src.outputAudioMixerGroup = soundData.mixerGroup ?? config?.defaultMixerGroup;
            src.spatialBlend = context.useSpatial ? soundData.spatialBlend : 0f;
            src.maxDistance = config != null ? config.maxDistance : soundData.maxDistance;
            src.loop = soundData.loop;
            src.priority = soundData.priority;
            src.playOnAwake = false;
            src.volume = finalVol;
            src.Play();

            if (!soundData.loop)
            {
                Destroy(go, soundData.clip.length + 0.1f);
            }
        }

        #endregion
    }
}
