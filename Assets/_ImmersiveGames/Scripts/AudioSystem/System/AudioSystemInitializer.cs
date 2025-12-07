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
            // Garantir serviços base no DI
            RegisterMathServiceIfNeeded();
            RegisterVolumeServiceIfNeeded();
            RegisterSfxServiceIfNeeded();

            // Já existe serviço global de áudio válido?
            if (IsInitialized()) return;

            // Garantir referência do AudioManager em cena
            RefreshCachedAudioManagerReference();
            if (_cachedAudioManager != null)
            {
                if (!_cachedAudioManager.IsInitialized)
                    DebugUtility.LogWarning(typeof(AudioSystemInitializer), "AudioManager encontrado mas não inicializado");
                return;
            }

            CreateAudioManagerFromResources();
        }

        private static void RegisterMathServiceIfNeeded()
        {
            if (DependencyManager.Provider == null) return;

            if (!DependencyManager.Provider.TryGetGlobal<IAudioMathService>(out _))
            {
                DependencyManager.Instance.RegisterGlobal<IAudioMathService>(new AudioMathUtility());
                DebugUtility.LogVerbose(
                    typeof(AudioSystemInitializer),
                    "AudioMathUtility registrado no DI",
                    DebugUtility.Colors.CrucialInfo);
            }
        }

        private static IAudioMathService ResolveMathService()
        {
            if (DependencyManager.Provider == null)
                return new AudioMathUtility();

            if (DependencyManager.Provider.TryGetGlobal<IAudioMathService>(out var math))
                return math;

            var newMath = new AudioMathUtility();
            DependencyManager.Instance.RegisterGlobal<IAudioMathService>(newMath);
            DebugUtility.LogVerbose(
                typeof(AudioSystemInitializer),
                "AudioMathUtility registrado no DI",
                DebugUtility.Colors.CrucialInfo);
            return newMath;
        }

        private static void RegisterVolumeServiceIfNeeded()
        {
            if (DependencyManager.Provider == null) return;
            if (DependencyManager.Provider.TryGetGlobal<IAudioVolumeService>(out _)) return;

            var mathService = ResolveMathService();
            DependencyManager.Provider.RegisterGlobal<IAudioVolumeService>(new AudioVolumeService(mathService));
            DebugUtility.Log(
                typeof(AudioSystemInitializer),
                "AudioVolumeService registrado no DI",
                DebugUtility.Colors.CrucialInfo);
        }

        private static void RegisterSfxServiceIfNeeded()
        {
            if (DependencyManager.Provider == null) return;
            if (DependencyManager.Provider.TryGetGlobal<IAudioSfxService>(out _)) return;

            var resolvedVolume = DependencyManager.Provider.TryGetGlobal<IAudioVolumeService>(out var volumeService)
                ? volumeService
                : new AudioVolumeService(ResolveMathService());

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

        private static void RefreshCachedAudioManagerReference()
        {
            if (_cachedAudioManager != null && !_cachedAudioManager)
            {
                _cachedAudioManager = null;
            }

            if (_cachedAudioManager == null)
            {
                _cachedAudioManager = Object.FindAnyObjectByType<AudioManager>();
            }
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

        private static bool IsInitialized()
        {
            if (DependencyManager.Provider == null) return false;

            if (!DependencyManager.Provider.TryGetGlobal<IAudioService>(out var service))
                return false;

            return IsServiceValid(service);
        }

        private static bool IsServiceValid(IAudioService service)
        {
            if (service == null) return false;

            if (service is Object unityObj)
            {
                return unityObj;
            }

            return true;
        }

        private static AudioServiceSettings LoadDefaultAudioServiceSettings()
        {
            AudioServiceSettings[] loaded = Resources.LoadAll<AudioServiceSettings>("Audio/AudioConfigs");
            return loaded is { Length: > 0 } ? loaded[0] : null;
        }

        private static AudioConfig LoadDefaultAudioConfig()
        {
            AudioConfig[] loaded = Resources.LoadAll<AudioConfig>("Audio/AudioConfigs");
            return loaded is { Length: > 0 } ? loaded[0] : null;
        }

        public static IAudioService GetAudioService()
        {
            return DependencyManager.Provider == null ? null : (DependencyManager.Provider.TryGetGlobal<IAudioService>(out var s) ? s : null);
        }
    }
}
