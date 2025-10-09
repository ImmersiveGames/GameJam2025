using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Pool;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Controlador base de áudio por entidade.
    /// Agora cada controlador pode possuir seu próprio pool de SoundEmitters (local).
    /// Se pool local não estiver configurado, faz fallback para IAudioService global.
    /// </summary>
    public abstract class AudioControllerBase : MonoBehaviour
    {
        [Header("Entity Audio Config")]
        [SerializeField] protected AudioConfig audioConfig;

        [Header("Local Emitter Pool (opcional)")]
        [SerializeField] private SoundEmitterPoolData soundEmitterPoolData;

        protected IAudioService audioService;
        protected bool isInitialized;

        // pool local (registrado no PoolManager)
        private ObjectPool _localPool;

        public AudioConfig AudioConfig => audioConfig;

        protected virtual void Awake()
        {
            InitializeAudioController();
        }

        protected virtual void OnDestroy()
        {
            // Opcional: não removemos pool do PoolManager para não interferir em outros usuários
        }

        private void InitializeAudioController()
        {
            if (isInitialized) return;

            AudioSystemInitializer.EnsureAudioSystemInitialized();
            audioService = AudioSystemInitializer.GetAudioService();
            isInitialized = audioService != null;

            if (!isInitialized)
            {
                DebugUtility.LogError<AudioControllerBase>("AudioService não encontrado");
                return;
            }

            InitializeLocalPoolIfNeeded();

            DebugUtility.LogVerbose<AudioControllerBase>($"{GetType().Name} inicializado ({name})", "green");
        }

        private void InitializeLocalPoolIfNeeded()
        {
            if (soundEmitterPoolData == null) return;

            // Registrar/obter pool via PoolManager (será criado se necessário)
            if (!PoolManager.Instance) return;

            PoolManager.Instance.RegisterPool(soundEmitterPoolData);
            _localPool = PoolManager.Instance.GetPool(soundEmitterPoolData.ObjectName);

            if (_localPool == null)
            {
                DebugUtility.LogWarning<AudioControllerBase>($"Falha ao inicializar pool local '{soundEmitterPoolData?.name}' em '{name}'");
            }
            else
            {
                DebugUtility.LogVerbose<AudioControllerBase>($"Pool local '{soundEmitterPoolData.ObjectName}' inicializado para '{name}'", "blue");
            }
        }

        /// <summary>
        /// Método genérico único para tocar som a partir de um controlador/entidade.
        /// Ele reúne contexto (position, spatial) e tenta usar pool local; se não houver pool, usa o AudioService global.
        /// </summary>
        public virtual void PlaySound(SoundData sound, AudioContextMode mode = AudioContextMode.Auto, float volumeMultiplier = 1f)
        {
            if (!isInitialized || sound == null || sound.clip == null) return;

            bool useSpatial = DecideSpatial(sound, mode);

            // volume efetivo = SoundData.volume * volumeMultiplier * audioConfig.defaultVolume (se houver)
            float effectiveMultiplier = sound.GetEffectiveVolume(volumeMultiplier) * (audioConfig?.defaultVolume ?? 1f);

            var ctx = AudioContext.Default(transform.position, useSpatial, effectiveMultiplier);

            // Preferir pool local (dono do fluxo local)
            if (_localPool != null)
            {
                var emit = _localPool.GetObject(ctx.position, null, null, false) as SoundEmitter;
                if (emit != null)
                {
                    emit.Initialize(sound, audioService);
                    emit.SetSpatialBlend(ctx.useSpatial ? sound.spatialBlend : 0f);
                    emit.SetMaxDistance(audioConfig != null ? audioConfig.maxDistance : sound.maxDistance);
                    emit.SetVolumeMultiplier(ctx.volumeMultiplier);
                    // mixer group será tomado do SoundData (se null, SoundEmitter manterá seu fallback)
                    emit.SetMixerGroup(sound.mixerGroup);
                    if (sound.randomPitch) emit.WithRandomPitch(-sound.pitchVariation, sound.pitchVariation);

                    emit.Activate(ctx.position);
                    emit.Play();

                    if (sound.frequentSound)
                        audioService?.RegisterFrequentSound(emit);

                    return;
                }
            }

            // Fallback: usar o serviço global (AudioManager)
            audioService?.PlaySound(sound, ctx, audioConfig);
        }

        protected void PlaySoundAtCamera(SoundData sound, float volumeMultiplier = 1f)
        {
            if (!isInitialized || sound == null) return;
            var cam = Camera.main;
            var pos = cam != null ? cam.transform.position : transform.position;
            var ctx = AudioContext.NonSpatial(sound.GetEffectiveVolume(volumeMultiplier) * (audioConfig?.defaultVolume ?? 1f));
            audioService?.PlaySound(sound, ctx, audioConfig);
        }

        /// <summary>
        /// Cria um SoundBuilder que usa este controlador (pool local + audioService).
        /// </summary>
        protected internal SoundBuilder CreateSoundBuilder()
        {
            return new SoundBuilder(audioService, _localPool);
        }

        private bool DecideSpatial(SoundData sound, AudioContextMode mode)
        {
            return mode switch
            {
                AudioContextMode.Spatial => true,
                AudioContextMode.NonSpatial => false,
                _ => (audioConfig?.useSpatialBlend ?? false) || sound.IsSpatial
            };
        }
    }
}
