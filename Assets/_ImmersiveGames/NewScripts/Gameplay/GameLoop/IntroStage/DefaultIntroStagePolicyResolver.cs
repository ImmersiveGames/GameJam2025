#nullable enable
using System;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Resolver padrão de política da IntroStage (preparado para produção).
    /// </summary>
    public sealed class DefaultIntroStagePolicyResolver : IIntroStagePolicyResolver
    {
        private const string FallbackGameplaySceneName = "GameplayScene";
        private readonly IGameplaySceneClassifier _sceneClassifier;

        public DefaultIntroStagePolicyResolver(IGameplaySceneClassifier sceneClassifier)
        {
            _sceneClassifier = sceneClassifier ?? new DefaultGameplaySceneClassifier();
        }

        public IntroStagePolicy Resolve(SceneFlowProfileId profile, string targetScene, string reason)
        {
            if (!profile.IsGameplay)
            {
                return IntroStagePolicy.Disabled;
            }

            if (!IsGameplayTargetScene(targetScene))
            {
                return IntroStagePolicy.Disabled;
            }

            return IntroStagePolicy.Manual;
        }

        private bool IsGameplayTargetScene(string targetScene)
        {
            if (string.IsNullOrWhiteSpace(targetScene))
            {
                return false;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid()
                && string.Equals(activeScene.name, targetScene, StringComparison.Ordinal))
            {
                return _sceneClassifier.IsGameplayScene();
            }

            return string.Equals(targetScene, FallbackGameplaySceneName, StringComparison.Ordinal);
        }
    }
}
