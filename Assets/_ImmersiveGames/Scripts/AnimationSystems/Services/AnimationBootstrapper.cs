using _ImmersiveGames.Scripts.AnimationSystems.Config;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AnimationSystems.Services
{
    public abstract class AnimationBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
            var configProvider = new AnimationConfigProvider();
            
            // Registra configs padrão
            configProvider.RegisterConfig("PlayerAnimationController", 
                Resources.Load<AnimationConfig>($"DefaultPlayerAnimationConfig"));
            configProvider.RegisterConfig("EnemyAnimationController", 
                Resources.Load<AnimationConfig>($"DefaultEnemyAnimationConfig"));
                
            configProvider.Initialize();
            
            var globalAnimationService = new GlobalAnimationService();
            globalAnimationService.Initialize();
        }
    }
}