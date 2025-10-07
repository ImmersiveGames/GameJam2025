using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.System
{
    public static class AudioSystemInitializer
    {
        private static AudioManager _cachedAudioManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureAudioSystemInitialized()
        {
            // Se já está inicializado, não faz nada
            if (IsInitialized())
            {
                return;
            }

            // Tenta encontrar um AudioManager existente na cena
            _cachedAudioManager = Object.FindAnyObjectByType<AudioManager>();
            if (_cachedAudioManager != null)
            {
                // AudioManager se auto-inicializa no Awake, então só precisamos verificar
                if (!_cachedAudioManager.IsInitialized)
                {
                    DebugUtility.LogWarning(typeof(AudioSystemInitializer), 
                        "AudioManager encontrado mas não inicializado");
                }
                return;
            }

            // Se não encontrou, cria um novo a partir de Resources
            CreateAudioManagerFromResources();
        }

        private static void CreateAudioManagerFromResources()
        {
            var audioManagerPrefab = Resources.Load<AudioManager>($"Audio/Prefabs/AudioManager");
            if (audioManagerPrefab == null)
            {
                DebugUtility.LogError(typeof(AudioSystemInitializer),
                    "Prefab do AudioManager não encontrado em Resources/Audio/Prefabs/AudioManager");
                return;
            }

            _cachedAudioManager = Object.Instantiate(audioManagerPrefab);
            // Não precisa chamar Initialize() - já é chamado no Awake
            
            DebugUtility.LogVerbose(typeof(AudioSystemInitializer),
                "AudioManager criado a partir de Resources", "green");
        }

        public static bool IsInitialized()
        {
            return DependencyManager.Instance?.TryGetGlobal<IAudioService>(out _) ?? false;
        }

        public static IAudioService GetAudioService()
        {
            if (DependencyManager.Instance.TryGetGlobal<IAudioService>(out var service))
            {
                return service;
            }
            return null;
        }
    }
}