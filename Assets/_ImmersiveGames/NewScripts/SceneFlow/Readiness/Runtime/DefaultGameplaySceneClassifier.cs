using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SessionIntegration.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Readiness.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Readiness.Runtime
{
    /// <summary>
    /// Implementacao padrao: contexto canonico de gameplay da sessao, com fallback compatível por marker.
    /// </summary>
    public sealed class DefaultGameplaySceneClassifier : IGameplaySceneClassifier
    {
        public bool IsGameplayScene()
        {
            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var sessionIntegrationService) &&
                sessionIntegrationService != null &&
                sessionIntegrationService.TryGetCurrentSessionContext(out GameplaySessionContextSnapshot currentSession) &&
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

