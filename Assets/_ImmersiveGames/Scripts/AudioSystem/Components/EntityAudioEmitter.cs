using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Emissor de áudio reutilizável por entidade. Responsável por gerenciar o pool local
    /// de <see cref="SoundEmitter"/> e aplicar as configurações globais do sistema.
    /// Componentes de gameplay devem injetar seus próprios <see cref="SoundData"/> e chamar
    /// diretamente os métodos públicos para reproduzir efeitos sonoros locais.
    /// </summary>
    [DisallowMultipleComponent]
    public class EntityAudioEmitter : MonoBehaviour
    {
        [Header("Defaults")]
        [SerializeField] private AudioConfig defaults;
        [SerializeField] private Pool.SoundEmitterPoolData poolData;

        private ObjectPool _localPool;
        private IAudioMathService _math;
        private IAudioVolumeService _volumeService;
        private AudioServiceSettings _serviceSettings;

        public AudioConfig Defaults => defaults;
        public bool HasPool => _localPool != null;
        public bool UsesSpatialBlend => defaults?.useSpatialBlend ?? true;

        private void Awake()
        {
            AudioSystemInitializer.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _math);
                DependencyManager.Provider.TryGetGlobal(out _volumeService);
                DependencyManager.Provider.TryGetGlobal(out _serviceSettings);
            }

            _math ??= new AudioMathUtility();
            _volumeService ??= new AudioVolumeService(_math);

            if (poolData != null)
            {
                CreateLocalPool();
            }
            else
            {
                DebugUtility.LogVerbose<EntityAudioEmitter>(
                    $"{name} sem pool configurado — utilizando fontes temporárias para SFX locais.",
                    context: this);
            }
        }

        private void CreateLocalPool()
        {
            if (_localPool != null) return;

            var poolGo = new GameObject($"{gameObject.name}_Pool_{poolData.ObjectName}");
            poolGo.transform.SetParent(transform, false);
            var pool = poolGo.AddComponent<ObjectPool>();
            pool.SetData(poolData);
            pool.Initialize();
            _localPool = pool;
            DebugUtility.Log<EntityAudioEmitter>(
                $"Pool local '{poolData.ObjectName}' criado para {name}",
                DebugUtility.Colors.Success,
                this);
        }

        /// <summary>
        /// Reproduz um efeito sonoro local usando o pool dedicado (quando disponível) ou uma fonte temporária.
        /// </summary>
        public void Play(SoundData soundData, AudioContext ctx, float fadeInSeconds = 0f)
        {
            if (soundData == null || soundData.clip == null) return;

            if (_localPool != null)
                PlayWithPool(soundData, ctx, fadeInSeconds);
            else
                PlayWithTemporarySource(soundData, ctx, fadeInSeconds);
        }

        public SoundBuilder CreateBuilder()
        {
            if (_localPool == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] Pool local não inicializado ao criar SoundBuilder — SoundBuilder requer pool",
                    this);
                return null;
            }

            return new SoundBuilder(_localPool, _math, _volumeService, _serviceSettings, defaults);
        }

        private void PlayWithPool(SoundData soundData, AudioContext ctx, float fadeInSeconds)
        {
            var emitter = _localPool.GetObject(ctx.position, null, null, false) as SoundEmitter;
            if (emitter == null)
            {
                DebugUtility.LogWarning<EntityAudioEmitter>(
                    $"[{name}] Falha ao obter SoundEmitter do pool local",
                    this);
                return;
            }

            emitter.Initialize(soundData);
            emitter.SetSpatialBlend(ctx.useSpatial ? soundData.spatialBlend : 0f);

            float maxDist = defaults != null ? defaults.maxDistance : soundData.maxDistance;
            emitter.SetMaxDistance(maxDist);

            var mixer = soundData.mixerGroup ?? defaults?.defaultMixerGroup;
            emitter.SetMixerGroup(mixer);

            float finalVol = _volumeService.CalculateSfxVolume(soundData, defaults, _serviceSettings, ctx);

            float multiplier = soundData.volume > 0f ? Mathf.Clamp01(finalVol / soundData.volume) : Mathf.Clamp01(finalVol);
            emitter.SetVolumeMultiplier(multiplier);

            DebugUtility.LogVerbose<EntityAudioEmitter>(
                $"[SFX Pool] {soundData.clip?.name} finalVol={finalVol:F3} mult={multiplier:F3} (DataVol={soundData.volume:F2})",
                context: this);

            if (soundData.randomPitch)
                emitter.WithRandomPitch(-soundData.pitchVariation, soundData.pitchVariation);

            emitter.Activate(ctx.position);
            if (fadeInSeconds > 0f) emitter.PlayWithFade(multiplier, fadeInSeconds);
            else emitter.Play();
        }

        private void PlayWithTemporarySource(SoundData soundData, AudioContext ctx, float fadeInSeconds)
        {
            var tempGo = new GameObject($"TempAudio_{soundData.clip?.name}");
            tempGo.transform.SetParent(transform);
            tempGo.transform.position = ctx.position;
            var src = tempGo.AddComponent<AudioSource>();

            src.clip = soundData.clip;
            src.outputAudioMixerGroup = soundData.mixerGroup ?? defaults?.defaultMixerGroup;
            src.priority = soundData.priority;
            src.loop = soundData.loop;
            src.playOnAwake = false;
            src.spatialBlend = ctx.useSpatial ? soundData.spatialBlend : 0f;
            src.maxDistance = defaults != null ? defaults.maxDistance : soundData.maxDistance;

            float finalVol = _volumeService.CalculateSfxVolume(soundData, defaults, _serviceSettings, ctx);

            if (soundData.randomPitch)
                src.pitch = _math?.ApplyRandomPitch(1f, soundData.pitchVariation) ??
                    Random.Range(1f - soundData.pitchVariation, 1f + soundData.pitchVariation);

            if (fadeInSeconds > 0f)
            {
                src.volume = 0f;
                src.Play();
                StartCoroutine(FadeVolume(src, 0f, finalVol, fadeInSeconds));
            }
            else
            {
                src.volume = finalVol;
                src.Play();
            }

            if (!soundData.loop && soundData.clip != null)
            {
                Destroy(tempGo, soundData.clip.length + fadeInSeconds + 0.1f);
            }
        }

        private System.Collections.IEnumerator FadeVolume(AudioSource src, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            src.volume = to;
        }
    }
}