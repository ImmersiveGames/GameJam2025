#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public sealed class LevelPostRunHookPresenterScopeResolver : ILevelPostRunHookPresenterScopeResolver
    {
        public bool TryResolvePresenters(LevelPostRunHookContext context, out IReadOnlyList<ILevelPostRunHookPresenter> presenters)
        {
            var resolvedPresenters = new List<ILevelPostRunHookPresenter>();

            if (context.LevelRef == null)
            {
                presenters = resolvedPresenters;
                return false;
            }

            foreach (SceneBuildIndexRef sceneRef in context.LevelRef.AdditiveScenes)
            {
                if (sceneRef == null || sceneRef.BuildIndex < 0)
                {
                    continue;
                }

                Scene loadedScene = SceneManager.GetSceneByBuildIndex(sceneRef.BuildIndex);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    continue;
                }

                CollectPresentersFromScene(loadedScene, resolvedPresenters);
            }

            if (resolvedPresenters.Count == 0)
            {
                string targetSceneName = Normalize(context.SceneName);

                if (!string.IsNullOrWhiteSpace(targetSceneName))
                {
                    for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
                    {
                        Scene loadedScene = SceneManager.GetSceneAt(sceneIndex);
                        if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                        {
                            continue;
                        }

                        if (!string.Equals(loadedScene.name, targetSceneName, System.StringComparison.Ordinal))
                        {
                            continue;
                        }

                        CollectPresentersFromScene(loadedScene, resolvedPresenters);
                    }
                }
            }

            presenters = resolvedPresenters;
            return resolvedPresenters.Count > 0;
        }

        private static void CollectPresentersFromScene(Scene scene, ICollection<ILevelPostRunHookPresenter> resolvedPresenters)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component != null &&
                        component.isActiveAndEnabled &&
                        component is ILevelPostRunHookPresenter presenter)
                    {
                        resolvedPresenters.Add(presenter);
                    }
                }
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
