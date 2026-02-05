using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness
{
    /// <summary>
    /// Serviço de classificação de cenas de gameplay.
    /// </summary>
    public interface IGameplaySceneClassifier
    {
        bool IsGameplayScene();
    }

    /// <summary>
    /// Implementação padrão: marker explícito na cena ativa, com fallback por nome.
    /// </summary>
    public sealed class DefaultGameplaySceneClassifier : IGameplaySceneClassifier
    {
        private const string FallbackGameplaySceneName = "GameplayScene";

        public bool IsGameplayScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return false;
            }

            if (HasMarkerInScene(activeScene))
            {
                return true;
            }

            return string.Equals(activeScene.name, FallbackGameplaySceneName, StringComparison.Ordinal);
        }

        private static bool HasMarkerInScene(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                if (roots[i].GetComponentInChildren<GameplaySceneMarker>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

