using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    public abstract class AudioControllerBase : MonoBehaviour
    {
        [SerializeField] protected AudioConfig audioConfig;
        private IAudioService _audioService;
        private bool _isInitialized;
        
        public AudioConfig AudioConfig => audioConfig;

        protected virtual void Awake()
        {
            InitializeAudioController();
        }

        private void InitializeAudioController()
        {
            if (_isInitialized) return;

            AudioSystemInitializer.EnsureAudioSystemInitialized();
            _audioService = AudioSystemInitializer.GetAudioService();
            _isInitialized = _audioService != null;

            if (!_isInitialized)
            {
                DebugUtility.LogError<AudioControllerBase>("AudioService não encontrado");
            }
        }

        /// <summary>
        /// Método genérico e único para tocar som (público para herdeiros ou eventos locais).
        /// </summary>
        public virtual void PlaySound(SoundData sound, AudioContextMode mode = AudioContextMode.Auto, float volumeMultiplier = 1f)
        {
            if (!_isInitialized || sound == null || sound.clip == null) return;

            bool useSpatial = DecideSpatial(sound, mode);
            var ctx = AudioContext.Default(transform.position, useSpatial, sound.GetEffectiveVolume(volumeMultiplier) * (audioConfig?.defaultVolume ?? 1f));
            _audioService.PlaySound(sound, ctx, audioConfig);
        }

        // Novo: Método simples encapsulado para casos diretos (ex.: tiros)
        protected void PlaySimpleSound(SoundData sound, float volumeMultiplier = 1f, bool useSpatial = true)
        {
            if (sound == null) return;
            var ctx = AudioContext.Default(transform.position, useSpatial, sound.GetEffectiveVolume(volumeMultiplier));
            _audioService.PlaySound(sound, ctx, audioConfig);
        }

        // Integrado de AudioSystemHelper: Protected para uso interno
        protected void PlaySoundNonSpatial(SoundData soundData, float volumeMultiplier = 1f)
        {
            var ctx = AudioContext.NonSpatial(volumeMultiplier);
            _audioService.PlaySound(soundData, ctx, audioConfig);
        }

        // Integrado de AudioSystemHelper: Outros helpers como SetSfxVolume (se local, mas global via serviço)
        protected void SetSfxVolume(float volume) => _audioService?.SetSfxVolume(volume);

        // Novo: Factory para SoundBuilder (para casos avançados, uso fluente interno)
        protected internal SoundBuilder CreateSoundBuilder()
        {
            return new SoundBuilder(_audioService);
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