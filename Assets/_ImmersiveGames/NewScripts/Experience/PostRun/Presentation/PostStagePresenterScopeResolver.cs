using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Experience.PostRun.Handoff;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Presentation
{
    public sealed class PostStagePresenterScopeResolver : IPostStagePresenterScopeResolver
    {
        public bool TryResolvePresenters(PostStageContext context, out IReadOnlyList<IPostStagePresenter> presenters)
        {
            string targetSceneName = Normalize(context.SceneName);

            if (TryResolvePresentersInMatchingScenes(targetSceneName, out presenters))
            {
                return true;
            }

            if (SceneManager.sceneCount == 0)
            {
                presenters = new List<IPostStagePresenter>();
                return false;
            }

            var resolvedPresenters = new List<IPostStagePresenter>();
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(sceneIndex);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject root in loadedScene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component != null &&
                            component.isActiveAndEnabled &&
                            component is IPostStagePresenter presenter)
                        {
                            resolvedPresenters.Add(presenter);
                        }
                    }
                }
            }

            if (resolvedPresenters.Count == 0)
            {
                presenters = resolvedPresenters;
                return false;
            }

            if (TryPreferCanonicalPresenter(resolvedPresenters, out IReadOnlyList<IPostStagePresenter> canonicalPresenters))
            {
                presenters = canonicalPresenters;
                return true;
            }

            presenters = resolvedPresenters;
            return true;
        }

        private static bool TryResolvePresentersInMatchingScenes(string targetSceneName, out IReadOnlyList<IPostStagePresenter> presenters)
        {
            var resolvedPresenters = new List<IPostStagePresenter>();

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                presenters = resolvedPresenters;
                return false;
            }

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

            if (resolvedPresenters.Count == 0)
            {
                presenters = resolvedPresenters;
                return false;
            }

            if (TryPreferCanonicalPresenter(resolvedPresenters, out IReadOnlyList<IPostStagePresenter> canonicalPresenters))
            {
                presenters = canonicalPresenters;
                return true;
            }

            presenters = resolvedPresenters;
            return true;
        }

        private static bool TryPreferCanonicalPresenter(
            IReadOnlyList<IPostStagePresenter> resolvedPresenters,
            out IReadOnlyList<IPostStagePresenter> presenters)
        {
            presenters = resolvedPresenters;
            return false;
        }

        private static void CollectPresentersFromScene(Scene scene, ICollection<IPostStagePresenter> resolvedPresenters)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component != null &&
                        component.isActiveAndEnabled &&
                        component is IPostStagePresenter presenter)
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
