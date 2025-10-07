// Path: _ImmersiveGames/Scripts/AudioSystem/Base/AudioControllerBase.cs
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Base
{
    /// <summary>
    /// Controlador base de áudio para entidades. Fornece métodos unificados para tocar sons
    /// com AudioContext respeitando o AudioConfig do objeto.
    /// </summary>
    public abstract class AudioControllerBase : MonoBehaviour
    {
        [SerializeField] protected AudioConfig audioConfig;

        private IAudioService _audioService;
        protected bool isInitialized;

        public AudioConfig AudioConfig => audioConfig;

        protected virtual void Awake()
        {
            InitializeAudioController();
        }

        private void InitializeAudioController()
        {
            if (isInitialized) return;

            AudioSystemInitializer.EnsureAudioSystemInitialized();
            _audioService = AudioSystemInitializer.GetAudioService();

            if (_audioService != null)
            {
                isInitialized = true;
                DebugUtility.LogVerbose<AudioControllerBase>($"AudioController inicializado: {name}", "green");
            }
            else
            {
                DebugUtility.LogError<AudioControllerBase>("Falha ao obter IAudioService");
            }
        }

        /// <summary>
        /// Método unificado para tocar SoundData com contexto definido.
        /// Subclasses devem usar esse método para reproduzir SFX do objeto.
        /// </summary>
        protected void Play(SoundData soundData, AudioContext context)
        {
            if (!isInitialized || _audioService == null || soundData == null) return;
            _audioService.PlaySound(soundData, context, audioConfig);
        }

        protected void PlayAtPosition(SoundData soundData, Vector3 position, float volumeMultiplier = 1f)
        {
            Play(soundData, new AudioContext { Position = position, UseSpatial = true, VolumeMultiplier = volumeMultiplier });
        }

        protected void PlayAtCamera(SoundData soundData, float volumeMultiplier = 1f)
        {
            var mainCamera = Camera.main;
            var pos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
            Play(soundData, new AudioContext { Position = pos, UseSpatial = false, VolumeMultiplier = volumeMultiplier });
        }
    }
}
