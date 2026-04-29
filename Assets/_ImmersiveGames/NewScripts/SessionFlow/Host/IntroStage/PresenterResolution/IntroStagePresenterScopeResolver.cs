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

            // Resolve from phase content only.
            // No fallback to active scene: the scope must be explicit in the phase definition.
            if (session.PhaseDefinitionRef != null && session.PhaseDefinitionRef.Content != null && session.PhaseDefinitionRef.Content.entries != null)
            {
                ResolveFromPhaseContent(session, resolvedPresenters, seenInstanceIds);
            }

            if (resolvedPresenters.Count == 0)
            {
                DebugUtility.Log<IntroStagePresenterScopeResolver>(
                    $"[OBS][IntroStage] No presenter in phase scope. phaseRef='{(session.PhaseDefinitionRef != null ? session.PhaseDefinitionRef.name : "<none>")}' contentId='{session.LocalContentId}' signature='{session.SessionSignature}' outcome='no_content'.",
                    DebugUtility.Colors.Info);

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

