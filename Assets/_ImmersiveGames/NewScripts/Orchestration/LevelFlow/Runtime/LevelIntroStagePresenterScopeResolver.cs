#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelFlow.Runtime
{
    public sealed class LevelIntroStagePresenterScopeResolver : ILevelIntroStagePresenterScopeResolver
    {
        public bool TryResolvePresenters(LevelIntroStageSession session, out IReadOnlyList<ILevelIntroStagePresenter> presenters)
        {
            presenters = new List<ILevelIntroStagePresenter>();

            if (session.LevelRef == null)
            {
                return false;
            }

            List<ILevelIntroStagePresenter> resolvedPresenters = new List<ILevelIntroStagePresenter>();

            foreach (var sceneRef in session.LevelRef.AdditiveScenes)
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

                foreach (GameObject root in loadedScene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component is ILevelIntroStagePresenter presenter)
                        {
                            resolvedPresenters.Add(presenter);
                        }
                    }
                }
            }

            if (resolvedPresenters.Count == 0)
            {
                return false;
            }

            presenters = resolvedPresenters;
            return true;
        }
    }
}
