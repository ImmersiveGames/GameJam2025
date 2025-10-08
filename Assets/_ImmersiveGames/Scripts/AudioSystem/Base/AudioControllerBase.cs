using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Base
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
        /// Método genérico e único para tocar som.
        /// </summary>
        public virtual void PlaySound(SoundData sound, AudioContextMode mode = AudioContextMode.Auto, float volumeMultiplier = 1f)
        {
            if (!_isInitialized || sound == null || sound.clip == null) return;

            bool useSpatial = DecideSpatial(sound, mode);
            var ctx = AudioContext.Default(transform.position, useSpatial, sound.GetEffectiveVolume(volumeMultiplier) * (audioConfig?.defaultVolume ?? 1f));
            _audioService.PlaySound(sound, ctx, audioConfig);
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