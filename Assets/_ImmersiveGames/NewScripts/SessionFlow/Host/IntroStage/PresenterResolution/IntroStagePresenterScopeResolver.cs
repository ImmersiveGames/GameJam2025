#nullable enable
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.SessionFlow.Host.IntroStage.PresenterResolution
{
    public sealed class IntroStagePresenterScopeResolver : IIntroStagePresenterScopeResolver
    {
        public bool TryResolvePresenters(IntroStageSession session, out IReadOnlyList<IIntroStagePresenter> presenters)
        {
            List<IIntroStagePresenter> resolvedPresenters = new List<IIntroStagePresenter>();
            HashSet<int> seenInstanceIds = new HashSet<int>();

            if (session.PhaseDefinitionRef != null && session.PhaseDefinitionRef.Content != null && session.PhaseDefinitionRef.Content.entries != null)
            {
                ResolveFromPhaseContent(session, resolvedPresenters, seenInstanceIds);
            }

            if (resolvedPresenters.Count == 0)
            {
                ResolveFromActiveScene(resolvedPresenters, seenInstanceIds);
            }

            if (resolvedPresenters.Count == 0)
            {
                DebugUtility.LogWarning<IntroStagePresenterScopeResolver>(
                    $"[WARN][OBS][IntroStage] No scene-local presenter could be resolved. phaseRef='{(session.PhaseDefinitionRef != null ? session.PhaseDefinitionRef.name : "<none>")}' activeScene='{SceneManager.GetActiveScene().name}' contentId='{session.LocalContentId}' signature='{session.SessionSignature}'.");
                presenters = new List<IIntroStagePresenter>();
                return false;
            }

            presenters = resolvedPresenters;
            return true;
        }

        private static void ResolveFromPhaseContent(
            IntroStageSession session,
            List<IIntroStagePresenter> resolvedPresenters,
            HashSet<int> seenInstanceIds)
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

                Scene loadedScene = SceneManager.GetSceneByName(entry.sceneRef.SceneName);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded)
                {
                    continue;
                }

                AppendPresentersFromScene(loadedScene, resolvedPresenters, seenInstanceIds);
            }
        }

        private static void ResolveFromActiveScene(
            List<IIntroStagePresenter> resolvedPresenters,
            HashSet<int> seenInstanceIds)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                return;
            }

            AppendPresentersFromScene(activeScene, resolvedPresenters, seenInstanceIds);
        }

        private static void AppendPresentersFromScene(
            Scene loadedScene,
            List<IIntroStagePresenter> resolvedPresenters,
            HashSet<int> seenInstanceIds)
        {
            foreach (GameObject root in loadedScene.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is IIntroStagePresenter presenter && seenInstanceIds.Add(component.GetInstanceID()))
                    {
                        resolvedPresenters.Add(presenter);
                    }
                }
            }
        }
    }
}

