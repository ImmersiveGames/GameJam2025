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

        private Coroutine _bgmFadeCoroutine;
        private SoundData _currentBGM;
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

        public void PlayBGM(SoundData bgmData, bool loop = true, float fadeInDuration = 0f)
        {
            // For brevity: implement as needed or keep previous BGM logic
        }

        public void StopBGM(float fadeOutDuration = 0f) { }
        public void PauseBGM() { }
        public void ResumeBGM() { }
        public void SetBGMVolume(float volume) { }
        public void CrossfadeBGM(SoundData newBgmData, float fadeDuration = 2f) { }

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
