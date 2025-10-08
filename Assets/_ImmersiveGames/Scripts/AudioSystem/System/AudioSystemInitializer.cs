// Path: _ImmersiveGames/Scripts/AudioSystem/AudioSystemInitializer.cs

using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Garante que o AudioManager esteja inicializado e registrado como IAudioService global.
    /// Pode ser usado por qualquer componente (como AudioControllerBase) antes de tocar sons.
    /// </summary>
    public static class AudioSystemInitializer
    {
        private static AudioManager _cachedAudioManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureAudioSystemInitialized()
        {
            if (IsInitialized()) return;

            _cachedAudioManager = Object.FindAnyObjectByType<AudioManager>();
            if (_cachedAudioManager != null)
            {
                if (!_cachedAudioManager.IsInitialized)
                    DebugUtility.LogWarning(typeof(AudioSystemInitializer), "AudioManager encontrado mas não inicializado");
                return;
            }

            CreateAudioManagerFromResources();
        }

        private static void CreateAudioManagerFromResources()
        {
            // Ajuste o caminho conforme seu projeto: Resources/Audio/Prefabs/AudioManager
            var audioManagerPrefab = Resources.Load<AudioManager>("Audio/Prefabs/AudioManager");
            if (audioManagerPrefab == null)
            {
                DebugUtility.LogError(typeof(AudioSystemInitializer),
                    "Prefab do AudioManager não encontrado em Resources/Audio/Prefabs/AudioManager");
                return;
            }

            _cachedAudioManager = Object.Instantiate(audioManagerPrefab);
            DebugUtility.LogVerbose(typeof(AudioSystemInitializer), "AudioManager criado a partir de Resources", "green");
        }

        private static bool IsInitialized()
        {
            return DependencyManager.Instance?.TryGetGlobal<IAudioService>(out _) ?? false;
        }

        public static IAudioService GetAudioService()
        {
            if (DependencyManager.Instance == null) return null;
            return DependencyManager.Instance.TryGetGlobal<IAudioService>(out var service) ? service : null;
        }
    }
}
