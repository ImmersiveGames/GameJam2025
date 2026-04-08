#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime
{
    public sealed class LevelIntroStagePresenterScopeResolver : ILevelIntroStagePresenterScopeResolver
    {
        public bool TryResolvePresenters(_ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session, out IReadOnlyList<ILevelIntroStagePresenter> presenters)
        {
            List<ILevelIntroStagePresenter> resolvedPresenters = new List<ILevelIntroStagePresenter>();

            if (session.PhaseDefinitionRef != null && session.PhaseDefinitionRef.Content != null && session.PhaseDefinitionRef.Content.entries != null)
            {
                ResolveFromPhaseContent(session, resolvedPresenters);
            }

            if (resolvedPresenters.Count == 0 && session.LevelRef != null)
            {
                ResolveFromLegacyLevel(session, resolvedPresenters);
            }

            if (resolvedPresenters.Count == 0)
            {
                presenters = new List<ILevelIntroStagePresenter>();
                return false;
            }

            presenters = resolvedPresenters;
            return true;
        }

        private static void ResolveFromPhaseContent(
            _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session,
            List<ILevelIntroStagePresenter> resolvedPresenters)
        {
            PhaseDefinitionAsset? phaseDefinitionRef = session.PhaseDefinitionRef;
            if (phaseDefinitionRef == null || phaseDefinitionRef.Content == null || phaseDefinitionRef.Content.entries == null)
            {
                return;
            }

            foreach (PhaseDefinitionAsset.PhaseContentEntry entry in phaseDefinitionRef.Content.entries)
            {
                if (entry == null ||
                    entry.sceneRef == null ||
                    string.IsNullOrWhiteSpace(entry.sceneRef.SceneName))
                {
                    continue;
                }

                if (entry.role != PhaseDefinitionAsset.PhaseSceneRole.Main &&
                    entry.role != PhaseDefinitionAsset.PhaseSceneRole.Additive)
                {
                    continue;
                }

                Scene loadedScene = SceneManager.GetSceneByName(entry.sceneRef.SceneName);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    continue;
                }

                AppendPresentersFromScene(loadedScene, resolvedPresenters);
            }
        }

        private static void ResolveFromLegacyLevel(
            _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Runtime.LevelIntroStageSession session,
            List<ILevelIntroStagePresenter> resolvedPresenters)
        {
            _ImmersiveGames.NewScripts.Game.Content.Definitions.Levels.Config.LevelDefinitionAsset? levelRef = session.LevelRef;
            if (levelRef == null)
            {
                return;
            }

            foreach (var sceneRef in levelRef.AdditiveScenes)
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

                AppendPresentersFromScene(loadedScene, resolvedPresenters);
            }
        }

        private static void AppendPresentersFromScene(Scene loadedScene, List<ILevelIntroStagePresenter> resolvedPresenters)
        {
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
    }
}
