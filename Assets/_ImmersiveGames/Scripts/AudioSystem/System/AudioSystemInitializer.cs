using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    /// <summary>
    /// Garante a existência do AudioManager e registra o AudioMathUtility no DI.
    /// </summary>
    public static class AudioSystemInitializer
    {
        private static AudioManager _cachedAudioManager;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureAudioSystemInitialized()
        {
            // registra math service se DI disponível e ainda não registrado
            if (DependencyManager.Provider != null && !DependencyManager.Provider.TryGetGlobal<IAudioMathService>(out var mathService))
            {
                mathService = new AudioMathUtility();
                DependencyManager.Provider.RegisterGlobal<IAudioMathService>(mathService);
                DebugUtility.Log(
                    typeof(AudioSystemInitializer),
                    "AudioMathUtility registrado no DI",
                    DebugUtility.Colors.CrucialInfo);
            }

            // registra volume service dependente do math service
            if (DependencyManager.Provider != null && !DependencyManager.Provider.TryGetGlobal<IAudioVolumeService>(out _))
            {
                mathService ??= DependencyManager.Provider.TryGetGlobal<IAudioMathService>(out var globalMath)
                    ? globalMath
                    : new AudioMathUtility();

                DependencyManager.Provider.RegisterGlobal<IAudioVolumeService>(new Services.AudioVolumeService(mathService));
                DebugUtility.Log(
                    typeof(AudioSystemInitializer),
                    "AudioVolumeService registrado no DI",
                    DebugUtility.Colors.CrucialInfo);
            }

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
            var audioManagerPrefab = Resources.Load<AudioManager>($"Audio/Prefabs/AudioManager");
            if (audioManagerPrefab == null)
            {
                DebugUtility.LogError(typeof(AudioSystemInitializer),
                    "Prefab do AudioManager não encontrado em Resources/Audio/Prefabs/AudioManager");
                return;
            }

            _cachedAudioManager = Object.Instantiate(audioManagerPrefab);
            DebugUtility.Log(
                typeof(AudioSystemInitializer),
                "AudioManager criado a partir de Resources",
                DebugUtility.Colors.Success);
        }

        private static bool IsInitialized() => DependencyManager.Provider?.TryGetGlobal<IAudioService>(out _) ?? false;

        public static IAudioService GetAudioService()
        {
            return DependencyManager.Provider == null ? null : (DependencyManager.Provider.TryGetGlobal<IAudioService>(out var s) ? s : null);
        }
    }
}