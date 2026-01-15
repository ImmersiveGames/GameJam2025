#nullable enable
using System;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Resolver padrão de política do Pregame (preparado para produção).
    /// </summary>
    public sealed class DefaultPregamePolicyResolver : IPregamePolicyResolver
    {
        private const string FallbackGameplaySceneName = "GameplayScene";
        private readonly IGameplaySceneClassifier _sceneClassifier;

        public DefaultPregamePolicyResolver(IGameplaySceneClassifier sceneClassifier)
        {
            _sceneClassifier = sceneClassifier ?? new DefaultGameplaySceneClassifier();
        }

        public PregamePolicy Resolve(SceneFlowProfileId profile, string targetScene, string reason)
        {
            if (!profile.IsGameplay)
            {
                return PregamePolicy.Disabled;
            }

            if (!IsGameplayTargetScene(targetScene))
            {
                return PregamePolicy.Disabled;
            }

            return PregamePolicy.Manual;
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
