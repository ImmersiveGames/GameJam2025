using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.AudioSystem.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AudioSystem.Core
{
    /// <summary>
    /// Ponto central de bootstrap do sistema de áudio.
    /// - Garante o registro de serviços base (Math, Volume, SFX) no DI.
    /// - Garante a existência de um GlobalBgmAudioService em cena.
    /// </summary>
    public static class AudioSystemBootstrap
    {
        private static GlobalBgmAudioService _cachedGlobalBgmAudioService;

        /// <summary>
        /// Método chamado automaticamente após o carregamento da primeira cena.
        /// Pode ser chamado manualmente por outros sistemas que precisem garantir
        /// que o áudio está pronto antes do uso.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void EnsureAudioSystemInitialized()
        {
            // Garantir serviços base no DI
            RegisterMathServiceIfNeeded();
            RegisterVolumeServiceIfNeeded();
            RegisterSfxServiceIfNeeded();

            // Se já existe um serviço global de BGM válido, não há mais nada a fazer.
            if (IsInitialized())
                return;

            // Garantir referência do GlobalBgmAudioService em cena
            RefreshCachedAudioManagerReference();
            if (_cachedGlobalBgmAudioService != null)
            {
                if (!_cachedGlobalBgmAudioService.IsInitialized)
                {
                    DebugUtility.LogWarning(
                        typeof(AudioSystemBootstrap),
                        "GlobalBgmAudioService encontrado mas ainda não inicializado. Verifique a ordem de inicialização.");
                }

                return;
            }

            CreateAudioManagerFromResources();
        }

        #region Registro de serviços base

        private static void RegisterMathServiceIfNeeded()
        {
            if (DependencyManager.Provider == null)
                return;

            if (!DependencyManager.Provider.TryGetGlobal<IAudioMathService>(out _))
            {
                DependencyManager.Instance.RegisterGlobal<IAudioMathService>(new AudioMathService());

                DebugUtility.LogVerbose(
                    typeof(AudioSystemBootstrap),
                    "AudioMathService registrado no DI",
                    DebugUtility.Colors.CrucialInfo);
            }
        }

        private static IAudioMathService ResolveMathService()
        {
            // Se não há DI disponível, devolve uma instância local simples.
            if (DependencyManager.Provider == null)
                return new AudioMathService();

            if (DependencyManager.Provider.TryGetGlobal<IAudioMathService>(out var math))
                return math;

            var newMath = new AudioMathService();
            DependencyManager.Instance.RegisterGlobal<IAudioMathService>(newMath);

            DebugUtility.LogVerbose(
                typeof(AudioSystemBootstrap),
                "AudioMathService registrado no DI",
                DebugUtility.Colors.CrucialInfo);

            return newMath;
        }

        private static void RegisterVolumeServiceIfNeeded()
        {
            if (DependencyManager.Provider == null)
                return;

            if (DependencyManager.Provider.TryGetGlobal<IAudioVolumeService>(out _))
                return;

            var mathService = ResolveMathService();
            DependencyManager.Provider.RegisterGlobal<IAudioVolumeService>(new AudioVolumeService(mathService));

            DebugUtility.Log(
                typeof(AudioSystemBootstrap),
                "AudioVolumeService registrado no DI",
                DebugUtility.Colors.CrucialInfo);
        }

        private static void RegisterSfxServiceIfNeeded()
        {
            if (DependencyManager.Provider == null)
                return;

            if (DependencyManager.Provider.TryGetGlobal<IAudioSfxService>(out _))
                return;

            // Volume service
            var resolvedVolume = DependencyManager.Provider.TryGetGlobal<IAudioVolumeService>(out var volumeService)
                ? volumeService
                : new AudioVolumeService(ResolveMathService());

            // AudioServiceSettings (fonte da verdade dos volumes globais)
            var resolvedSettings = DependencyManager.Provider.TryGetGlobal(out AudioServiceSettings serviceSettings)
                ? serviceSettings
                : LoadDefaultAudioServiceSettings();

            // AudioConfig padrão (perfil base de SFX)
            var resolvedConfig = DependencyManager.Provider.TryGetGlobal(out AudioConfig defaultConfig)
                ? defaultConfig
                : LoadDefaultAudioConfig();

            var sfxService = new AudioSfxService(resolvedVolume, resolvedSettings, resolvedConfig);
            DependencyManager.Provider.RegisterGlobal<IAudioSfxService>(sfxService);

            DebugUtility.Log(
                typeof(AudioSystemBootstrap),
                "AudioSfxService registrado no DI",
                DebugUtility.Colors.CrucialInfo);
        }

        #endregion

        #region Gerenciamento do GlobalBgmAudioService

        private static void RefreshCachedAudioManagerReference()
        {
            // Se o cache ainda aponta para um objeto válido, nada a fazer.
            if (_cachedGlobalBgmAudioService != null && _cachedGlobalBgmAudioService)
                return;

            _cachedGlobalBgmAudioService = Object.FindAnyObjectByType<GlobalBgmAudioService>();
        }

        private static void CreateAudioManagerFromResources()
        {
            var audioManagerPrefab =
                Resources.Load<GlobalBgmAudioService>("Audio/Prefabs/GlobalBgmAudioService");

            if (audioManagerPrefab == null)
            {
                DebugUtility.LogError(
                    typeof(AudioSystemBootstrap),
                    "Prefab do GlobalBgmAudioService não encontrado em Resources/Audio/Prefabs/GlobalBgmAudioService");
                return;
            }

            _cachedGlobalBgmAudioService = Object.Instantiate(audioManagerPrefab);

            DebugUtility.LogVerbose(
                typeof(AudioSystemBootstrap),
                "GlobalBgmAudioService criado a partir de Resources",
                DebugUtility.Colors.Success);
        }

        private static bool IsInitialized()
        {
            if (DependencyManager.Provider == null)
                return false;

            if (!DependencyManager.Provider.TryGetGlobal<IBgmAudioService>(out var service))
                return false;

            return IsServiceValid(service);
        }

        private static bool IsServiceValid(IBgmAudioService service)
        {
            if (service == null)
                return false;

            if (service is Object unityObj)
                return unityObj;

            return true;
        }

        #endregion

        #region Helpers de carregamento de configs

        private static AudioServiceSettings LoadDefaultAudioServiceSettings()
        {
            AudioServiceSettings[] loaded =
                Resources.LoadAll<AudioServiceSettings>("Audio/AudioConfigs");

            return loaded is { Length: > 0 } ? loaded[0] : null;
        }

        private static AudioConfig LoadDefaultAudioConfig()
        {
            AudioConfig[] loaded = Resources.LoadAll<AudioConfig>("Audio/AudioConfigs");
            return loaded is { Length: > 0 } ? loaded[0] : null;
        }

        /// <summary>
        /// Helper para obter o serviço de BGM atual via DI.
        /// </summary>
        public static IBgmAudioService GetAudioService()
        {
            if (DependencyManager.Provider == null)
                return null;

            return DependencyManager.Provider.TryGetGlobal<IBgmAudioService>(out var service)
                ? service
                : null;
        }

        #endregion
    }
}
