using _ImmersiveGames.Scripts.AudioSystem.Configs;
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
            IAudioMathService mathService = null;

            // registra math service se DI disponível e ainda não registrado
            if (DependencyManager.Provider != null && !DependencyManager.Provider.TryGetGlobal(out mathService))
            {
                DependencyManager.Instance.RegisterGlobal<IAudioMathService>(new AudioMathUtility());
                DebugUtility.LogVerbose(
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

                DependencyManager.Provider.RegisterGlobal<IAudioVolumeService>(new AudioVolumeService(mathService));
                DebugUtility.Log(
                    typeof(AudioSystemInitializer),
                    "AudioVolumeService registrado no DI",
                    DebugUtility.Colors.CrucialInfo);
            }

            // registra serviço de SFX global (pooling) dependente de volume service
            if (DependencyManager.Provider != null && !DependencyManager.Provider.TryGetGlobal<IAudioSfxService>(out _))
            {
                var resolvedVolume = DependencyManager.Provider.TryGetGlobal<IAudioVolumeService>(out var volumeService)
                    ? volumeService
                    : new AudioVolumeService(mathService ?? new AudioMathUtility());

                var resolvedSettings = DependencyManager.Provider.TryGetGlobal(out AudioServiceSettings serviceSettings)
                    ? serviceSettings
                    : LoadDefaultAudioServiceSettings();

                var resolvedConfig = DependencyManager.Provider.TryGetGlobal(out AudioConfig defaultConfig)
                    ? defaultConfig
                    : LoadDefaultAudioConfig();

                var sfxService = new AudioSfxService(resolvedVolume, resolvedSettings, resolvedConfig);
                DependencyManager.Provider.RegisterGlobal<IAudioSfxService>(sfxService);

                DebugUtility.Log(
                    typeof(AudioSystemInitializer),
                    "AudioSfxService registrado no DI",
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
            DebugUtility.LogVerbose(
                typeof(AudioSystemInitializer),
                "AudioManager criado a partir de Resources",
                DebugUtility.Colors.Success);
        }

        private static bool IsInitialized() => DependencyManager.Provider?.TryGetGlobal<IAudioService>(out _) ?? false;

        private static AudioServiceSettings LoadDefaultAudioServiceSettings()
        {
            var loaded = Resources.LoadAll<AudioServiceSettings>("Audio/AudioConfigs");
            return loaded != null && loaded.Length > 0 ? loaded[0] : null;
        }

        private static AudioConfig LoadDefaultAudioConfig()
        {
            var loaded = Resources.LoadAll<AudioConfig>("Audio/AudioConfigs");
            return loaded != null && loaded.Length > 0 ? loaded[0] : null;
        }

        public static IAudioService GetAudioService()
        {
            return DependencyManager.Provider == null ? null : (DependencyManager.Provider.TryGetGlobal<IAudioService>(out var s) ? s : null);
        }
    }
}
