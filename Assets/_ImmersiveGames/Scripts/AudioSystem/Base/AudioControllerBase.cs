using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.System;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Base
{
    public abstract class AudioControllerBase : MonoBehaviour
    {
        [SerializeField] protected AudioConfig audioConfig;

        private IAudioService _audioService;
        private bool _isInitialized;

        protected virtual void Awake()
        {
            InitializeAudioController();
        }

        private void InitializeAudioController()
        {
            if (_isInitialized) return;

            // Garante que o sistema de áudio está inicializado
            AudioSystemInitializer.EnsureAudioSystemInitialized();
            _audioService = AudioSystemInitializer.GetAudioService();
            
            if (_audioService != null)
            {
                _isInitialized = true;
                DebugUtility.LogVerbose<AudioControllerBase>($"Controlador de áudio inicializado: {name}", "green");
            }
            else
            {
                DebugUtility.LogError<AudioControllerBase>("Falha ao obter AudioService");
            }
        }

        protected void PlaySound(SoundData soundData)
        {
            if (!_isInitialized || _audioService == null)
            {
                DebugUtility.LogWarning<AudioControllerBase>("AudioController não inicializado");
                return;
            }

            _audioService.PlaySound(soundData, transform.position, audioConfig);
        }

        protected void PlaySound(SoundData soundData, Vector3 position)
        {
            if (!_isInitialized || _audioService == null) return;
            _audioService.PlaySound(soundData, position, audioConfig);
        }

        protected void PlaySoundAtCamera(SoundData soundData)
        {
            if (!_isInitialized || _audioService == null) return;
            
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _audioService.PlaySound(soundData, mainCamera.transform.position, audioConfig);
            }
        }
    }
}