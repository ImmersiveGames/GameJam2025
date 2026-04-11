using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Readiness.Runtime
{
    /// <summary>
    /// Implementacao padrao: contexto canonico de gameplay da sessao, com fallback compatível por marker.
    /// </summary>
    public sealed class DefaultGameplaySceneClassifier : IGameplaySceneClassifier
    {
        public bool IsGameplayScene()
        {
            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<IGameplaySessionContextService>(out var sessionContextService) &&
                sessionContextService != null &&
                sessionContextService.TryGetCurrent(out GameplaySessionContextSnapshot currentSession) &&
                currentSession.IsValid)
            {
                return true;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return false;
            }

            return HasMarkerInScene(activeScene);
        }

        private static bool HasMarkerInScene(Scene scene)
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
