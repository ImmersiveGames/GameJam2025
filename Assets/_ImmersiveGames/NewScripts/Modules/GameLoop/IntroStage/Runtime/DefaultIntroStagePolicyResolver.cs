#nullable enable
using System;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime
{
    /// <summary>
    /// Resolver padrao de politica da IntroStageController (preparado para producao).
    /// Decide apenas por sinais canonicos de cena alvo.
    /// </summary>
    public sealed class DefaultIntroStagePolicyResolver : IIntroStagePolicyResolver
    {
        private const string FallbackGameplaySceneName = "GameplayScene";
        private readonly IGameplaySceneClassifier _sceneClassifier;

        public DefaultIntroStagePolicyResolver(IGameplaySceneClassifier? sceneClassifier)
        {
            _sceneClassifier = sceneClassifier ?? new DefaultGameplaySceneClassifier();
        }

        public IntroStagePolicy Resolve(string targetScene, string reason)
        {
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
            if (activeScene.IsValid() && string.Equals(activeScene.name, targetScene, StringComparison.Ordinal))
            {
                return _sceneClassifier.IsGameplayScene();
            }

            return string.Equals(targetScene, FallbackGameplaySceneName, StringComparison.Ordinal);
        }
    }
}
