using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Bindings;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime
{
    /// <summary>
    /// Implementacao padrao: caminho canonico por contexto de sessao valido.
    /// Fallback por marker e restrito ao trilho explicito de QA/dev.
    /// </summary>
    public sealed class DefaultGameplaySceneClassifier : IGameplaySceneClassifier
    {
        public bool IsGameplayScene()
        {
            if (TryResolveCanonicalSessionContext(out bool isGameplayScene))
            {
                return isGameplayScene;
            }

            return ResolveQaDevMarkerFallback();
        }

        private static bool TryResolveCanonicalSessionContext(out bool isGameplayScene)
        {
            isGameplayScene = false;

            if (DependencyManager.Provider == null)
            {
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var sessionIntegrationService) ||
                sessionIntegrationService == null)
            {
                return false;
            }

            if (!sessionIntegrationService.TryGetCurrentSessionContext(out GameplaySessionContextSnapshot currentSession) ||
                !currentSession.IsValid)
            {
                return false;
            }

            isGameplayScene = true;
            return true;
        }

        private static bool ResolveQaDevMarkerFallback()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                DebugUtility.LogVerbose<DefaultGameplaySceneClassifier>(
                    "[OBS][Readiness][QA] MarkerFallbackIgnored reason='active_scene_invalid'.",
                    DebugUtility.Colors.Info);
                return false;
            }

            bool hasMarker = HasMarkerInScene(activeScene);
            if (hasMarker)
            {
                DebugUtility.LogWarning<DefaultGameplaySceneClassifier>(
                    $"[OBS][Readiness][QA] MarkerFallbackApplied scene='{activeScene.name}' path='qa-dev-non-canonical'.");
            }

            return hasMarker;
#else
            return false;
#endif
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
