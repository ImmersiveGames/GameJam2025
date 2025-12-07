using System.Collections;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Services;
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
        private IAudioVolumeService _volumeService;
        private AudioServiceSettings _resolvedSettings;

        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBgm;
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        private void OnDestroy()
        {
            IsInitialized = false;
        }

        private void Initialize()
        {
            if (IsInitialized) return;

            // DI: obter math (AudioSystemInitializer garante registro)
            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _math);
                DependencyManager.Provider.TryGetGlobal(out _volumeService);
                DependencyManager.Provider.TryGetGlobal(out _resolvedSettings);
            }

            _math ??= new AudioMathUtility();
            _volumeService ??= new AudioVolumeService(_math);
            _resolvedSettings ??= settings;

            DependencyManager.Provider?.RegisterGlobal<IAudioService>(this, true);

            // torna settings disponível via DI se for configurado
            if (settings != null)
            {
                DependencyManager.Provider?.RegisterGlobal(settings);
            }

            EnsureBgmAudioSource();

            DontDestroyOnLoad(gameObject);

            IsInitialized = true;
            DebugUtility.Log<AudioManager>(
                "AudioManager (BGM) inicializado",
                DebugUtility.Colors.CrucialInfo);
        }

        #region BGM

        public AudioSource BgmAudioSource => bgmAudioSource; // getter útil para debug/tests

        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            if (!IsInitialized || bgmData == null || bgmData.clip == null) return;
            if (!EnsureBgmAudioSource()) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            ApplyBgmData(bgmData, loop);

            // cálculo de volume centralizado no service para manter SRP/OCP
            float targetVolume = _volumeService.CalculateBgmVolume(bgmData, ResolveSettings());

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
            DebugUtility.LogVerbose<AudioManager>(
                $"BGM iniciado: {bgmData.clip?.name} (targetVolume={targetVolume:F3})",
                DebugUtility.Colors.Success);
        }

        public void StopBGM(float fadeOutDuration = 0f)
        {
            if (!IsSourceValid(bgmAudioSource) || !bgmAudioSource.isPlaying) return;

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            if (fadeOutDuration > 0f)
                _bgmFadeCoroutine = StartCoroutine(FadeVolume(bgmAudioSource, bgmAudioSource.volume, 0f, fadeOutDuration, true));
            else
                bgmAudioSource.Stop();

            _currentBgm = null;
            DebugUtility.LogVerbose<AudioManager>(
                "BGM parado",
                DebugUtility.Colors.Success);
        }
        public void StopBGMImmediate()
        {
            if (!IsSourceValid(bgmAudioSource)) return;

            bgmAudioSource.Stop();
            DebugUtility.LogVerbose<AudioManager>(
                "BGM parado",
                DebugUtility.Colors.Success);
        }

        public void PauseBGM()
        {
            if (!IsSourceValid(bgmAudioSource)) return;

            if (bgmAudioSource.isPlaying) bgmAudioSource.Pause();
        }
        public void ResumeBGM()
        {
            if (!IsSourceValid(bgmAudioSource)) return;

            if (!bgmAudioSource.isPlaying && _currentBgm != null) bgmAudioSource.Play();
        }

        public void SetBGMVolume(float volume)
        {
            if (audioMixer != null) audioMixer.SetFloat(bgmParameter, _math?.ToDecibels(volume) ?? 20f * Mathf.Log10(Mathf.Max(volume, 0.0001f)));
            else if (IsSourceValid(bgmAudioSource)) bgmAudioSource.volume = Mathf.Clamp01(volume);
        }

        public void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f)
        {
            if (newBgmData == null || newBgmData.clip == null) return;
            if (!EnsureBgmAudioSource()) return;
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = StartCoroutine(CrossfadeRoutine(newBgmData, fadeDuration));
        }

        private IEnumerator CrossfadeRoutine(SoundData newBgmData, float duration)
        {
            if (!IsSourceValid(bgmAudioSource)) yield break;

            float half = duration * 0.5f;
            float old = bgmAudioSource.volume;
            yield return StartCoroutine(FadeVolume(bgmAudioSource, old, 0f, half));
            if (!IsSourceValid(bgmAudioSource)) yield break;

            ApplyBgmData(newBgmData, true);
            bgmAudioSource.volume = 0f;
            bgmAudioSource.Play();
            float target = _volumeService.CalculateBgmVolume(newBgmData, ResolveSettings());
            yield return StartCoroutine(FadeVolume(bgmAudioSource, 0f, target, half));
            _currentBgm = newBgmData;
        }

        private IEnumerator FadeVolume(AudioSource src, float from, float to, float duration, bool stopAfter = false)
        {
            if (src == null || !src)
                yield break;

            float t = 0f;
            while (t < duration)
            {
                if (src == null || !src)
                    yield break;

                t += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            if (src == null || !src)
                yield break;

            src.volume = to;
            if (stopAfter && to <= 0f)
                src.Stop();
        }


        private void ApplyBgmData(SoundData data, bool loop)
        {
            if (!IsSourceValid(bgmAudioSource)) return;

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
            if (_math == null && DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _math);
                DependencyManager.Provider.TryGetGlobal(out _volumeService);
            }

            _math ??= new AudioMathUtility();
            _volumeService ??= new AudioVolumeService(_math);

            float finalVol = _volumeService.CalculateSfxVolume(soundData, config, ResolveSettings(), context);

            // cria AudioSource temporário (usa mixer-group do SoundData ou do config)
            var go = new GameObject($"OneShot_{soundData.clip.name}")
            {
                transform =
                {
                    position = context.position
                }
            };
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

        private AudioServiceSettings ResolveSettings()
        {
            if (_resolvedSettings != null) return _resolvedSettings;

            if (DependencyManager.Provider != null && DependencyManager.Provider.TryGetGlobal(out AudioServiceSettings settingsFromDi))
            {
                _resolvedSettings = settingsFromDi;
            }
            else
            {
                _resolvedSettings = settings;
            }

            return _resolvedSettings;
        }

        private bool EnsureBgmAudioSource()
        {
            if (IsSourceValid(bgmAudioSource)) return true;

            bgmAudioSource = gameObject.GetComponent<AudioSource>();
            if (!IsSourceValid(bgmAudioSource))
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
                if (!IsSourceValid(bgmAudioSource)) return false;
            }

            bgmAudioSource.playOnAwake = false;
            bgmAudioSource.loop = true;
            bgmAudioSource.spatialBlend = 0f;
            return true;
        }

        private static bool IsSourceValid(AudioSource source) => source != null && source;
    }
}
