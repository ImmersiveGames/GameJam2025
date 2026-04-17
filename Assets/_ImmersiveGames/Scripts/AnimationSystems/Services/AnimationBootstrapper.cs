using _ImmersiveGames.Scripts.AnimationSystems.Config;
using ImmersiveGames.GameJam2025.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AnimationSystems.Services
{
    public abstract class AnimationBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
#if NEWSCRIPTS_MODE
            DebugUtility.Log(typeof(AnimationBootstrapper), "NEWSCRIPTS_MODE ativo: AnimationBootstrapper ignorado.");
#else
            var configProvider = new AnimationConfigProvider();

            // Registra configs padrão por tipo de controller (nome da classe)
            RegisterDefaultConfig(configProvider, "PlayerAnimationController", "DefaultPlayerAnimationConfig");
            RegisterDefaultConfig(configProvider, "EaterAnimationController", "DefaultEaterAnimationConfig");
            RegisterDefaultConfig(configProvider, "EnemyAnimationController", "DefaultEnemyAnimationConfig");

            configProvider.Initialize();

            var globalAnimationService = new GlobalAnimationService();
            globalAnimationService.Initialize();

            DebugUtility.LogVerbose<AnimationBootstrapper>(
                "AnimationBootstrapper inicializado (AnimationConfigProvider + GlobalAnimationService registrados).",
                DebugUtility.Colors.Info);
#endif
        }

        private static void RegisterDefaultConfig(AnimationConfigProvider provider, string controllerTypeName, string resourcePath)
        {
            var config = Resources.Load<AnimationConfig>(resourcePath);
            if (config != null)
            {
                provider.RegisterConfig(controllerTypeName, config);
                DebugUtility.LogVerbose<AnimationBootstrapper>(
                    $"AnimationConfig registrada para '{controllerTypeName}' a partir de Resources ('{resourcePath}').",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogWarning<AnimationBootstrapper>(
                    $"AnimationConfig n�o encontrada em Resources para '{controllerTypeName}' (path: '{resourcePath}').");
            }
        }
    }
}

