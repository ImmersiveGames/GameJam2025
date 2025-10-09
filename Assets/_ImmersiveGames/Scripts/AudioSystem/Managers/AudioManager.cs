using System.Collections;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
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

        [Header("BGM Channel")]
        [SerializeField]
        internal AudioSource bgmAudioSource;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;

        [Header("Optional Settings")]
        [SerializeField] private AudioServiceSettings settings;

        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBgm;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
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

            // Register serviço global
            DependencyManager.Instance.RegisterGlobal<IAudioService>(this);

            // Garantir source de BGM
            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.loop = true;
                bgmAudioSource.spatialBlend = 0f;
                bgmAudioSource.outputAudioMixerGroup = bgmMixerGroup;
            }

            IsInitialized = true;
            DebugUtility.LogVerbose<AudioManager>("AudioManager inicializado (BGM + Mixer)", "green");
        }

        #region IAudioService - SFX (delegado para pools locais)
        public void PlaySound(SoundData soundData, AudioContext context, AudioConfig config = null)
        {
            // AudioManager não gerencia pools locais — espera que controllers usem pools.
            // Se um sistema chamar AudioManager diretamente, podemos executar via um SoundEmitter temporário
            // (não ideal para sons frequentes). Aqui faremos um fallback simples: criar um AudioSource temporário.

            if (!IsInitialized || soundData == null || soundData.clip == null) return;

            // Fallback: AudioSource temporário
            var go = new GameObject($"SFX_{soundData.clip.name}");
            go.transform.position = context.position;
            var src = go.AddComponent<AudioSource>();
            src.clip = soundData.clip;
            src.outputAudioMixerGroup = soundData.mixerGroup;
            src.volume = soundData.GetEffectiveVolume(context.volumeMultiplier);
            src.spatialBlend = context.useSpatial ? soundData.spatialBlend : 0f;
            src.maxDistance = soundData.maxDistance;
            src.priority = soundData.priority;
            src.loop = soundData.loop;
            src.Play();

            if (!soundData.loop)
                Destroy(go, soundData.clip.length + 0.1f);
        }

        public void SetSfxVolume(float volume)
        {
            if (audioMixer != null) audioMixer.SetFloat(sfxParameter, LinearToDecibel(volume));
        }

        public void StopAllSounds()
        {
            // Não gerenciamos emitters locais (controladores o fazem).
            // Poderíamos parar todos AudioSources no cena se necessário (opcional).
        }
        #endregion

        #region BGM
        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            if (!IsInitialized || bgmData == null || bgmData.clip == null) return;
            if (bgmAudioSource == null) Initialize();

            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            bgmAudioSource.clip = bgmData.clip;
            bgmAudioSource.loop = loop;
            bgmAudioSource.outputAudioMixerGroup = bgmData.mixerGroup ?? bgmMixerGroup;

            if (fadeInDuration > 0f)
            {
                _bgmFadeCoroutine = StartCoroutine(FadeBgm(0f, bgmData.volume, fadeInDuration, play: true));
            }
            else
            {
                bgmAudioSource.volume = bgmData.volume;
                bgmAudioSource.Play();
            }

            _currentBgm = bgmData;
            DebugUtility.LogVerbose<AudioManager>($"BGM started: {bgmData.clip?.name}", "cyan");
        }

        public void StopBGM(float fadeOutDuration = 0f)
        {
            if (bgmAudioSource == null || !bgmAudioSource.isPlaying) return;
            if (_bgmFadeCoroutine != null) StopCoroutine(_bgmFadeCoroutine);

            if (fadeOutDuration > 0f)
                _bgmFadeCoroutine = StartCoroutine(FadeBgm(bgmAudioSource.volume, 0f, fadeOutDuration, play: false));
            else
            {
                bgmAudioSource.Stop();
                _currentBgm = null;
            }

            DebugUtility.LogVerbose<AudioManager>("BGM stopped", "yellow");
        }

        public void PauseBGM() { if (bgmAudioSource != null) bgmAudioSource.Pause(); }
        public void ResumeBGM() { if (bgmAudioSource != null) bgmAudioSource.UnPause(); }
        public void SetBGMVolume(float volume)
        {
            if (audioMixer != null) audioMixer.SetFloat(bgmParameter, LinearToDecibel(volume));
            else if (bgmAudioSource != null) bgmAudioSource.volume = volume;
        }

        public void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f)
        {
            if (!IsInitialized || newBgmData == null || newBgmData.clip == null) return;
            StartCoroutine(CrossfadeRoutine(newBgmData, fadeDuration));
        }

        private IEnumerator FadeBgm(float from, float to, float duration, bool play = false)
        {
            if (play && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmAudioSource.volume = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            bgmAudioSource.volume = to;
            if (to <= 0f && bgmAudioSource.isPlaying) bgmAudioSource.Stop();
        }

        private IEnumerator CrossfadeRoutine(SoundData newBgmData, float duration)
        {
            var oldSource = bgmAudioSource;
            var newSourceGO = new GameObject("BGM_Crossfade");
            newSourceGO.transform.SetParent(transform);
            var newSource = newSourceGO.AddComponent<AudioSource>();
            newSource.clip = newBgmData.clip;
            newSource.loop = true;
            newSource.outputAudioMixerGroup = newBgmData.mixerGroup ?? bgmMixerGroup;
            newSource.spatialBlend = 0f;
            newSource.volume = 0f;
            newSource.Play();

            float elapsed = 0f;
            float oldStart = oldSource != null ? oldSource.volume : 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (oldSource != null) oldSource.volume = Mathf.Lerp(oldStart, 0f, t);
                newSource.volume = Mathf.Lerp(0f, newBgmData.volume, t);
                yield return null;
            }

            if (oldSource != null) Destroy(oldSource.gameObject);
            bgmAudioSource = newSource;
            _currentBgm = newBgmData;
        }
        #endregion

        #region Pool helper (no-op since pools are local)
        public bool CanPlaySound(SoundData soundData)
        {
            // AudioManager não conhece regras de emitters locais
            return soundData != null;
        }

        public SoundEmitter GetEmitterFromPool() => null;
        public void ReturnEmitterToPool(SoundEmitter emitter) { /* not used */ }
        public void RegisterFrequentSound(SoundEmitter emitter) { /* not used */ }
        #endregion

        private float LinearToDecibel(float linear)
        {
            if (linear <= 0.0001f) return -80f;
            return Mathf.Log10(linear) * 20f;
        }
    }
}
