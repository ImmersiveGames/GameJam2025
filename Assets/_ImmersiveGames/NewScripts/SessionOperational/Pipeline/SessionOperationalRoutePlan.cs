using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public readonly struct SessionOperationalRoutePlan
    {
        public SessionOperationalRoutePlan(
            string routeIdentity,
            SessionOperationalRouteTransitionMode transitionMode,
            SceneTransitionProfile transitionProfile,
            SessionOperationalRouteLoadingMode loadingMode,
            RuntimeLoadingProfileAsset loadingProfile,
            IReadOnlyList<SceneKeyAsset> scenesToLoad,
            IReadOnlyList<SceneKeyAsset> scenesToUnload,
            IReadOnlyList<SceneKeyAsset> finalScenesToLoad,
            IReadOnlyList<SceneKeyAsset> autoScenesToUnload,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload,
            SceneKeyAsset activeSceneKey,
            SessionOperationalRouteCompletionHandoffKind completionHandoff,
            string handoffSessionStateId,
            OperationalSurfaceKind operationalSurfaceKind,
            SessionOperationalInputPolicy inputPolicy,
            RouteActivitySavePolicy activitySavePolicy,
            PlayerSetDefinitionAsset routeParticipantSetDefinition,
            SessionOperationalRouteAudioCommand audio,
            SurfacePresentationProfileAsset surfacePresentationProfile,
            ActivityPresentationProfileAsset activityPresentationProfile)
        {
            RouteIdentity = Normalize(routeIdentity);
            TransitionMode = transitionMode;
            TransitionProfile = transitionProfile;
            LoadingMode = loadingMode;
            LoadingProfile = loadingProfile;
            ScenesToLoad = scenesToLoad ?? Array.Empty<SceneKeyAsset>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<SceneKeyAsset>();
            FinalScenesToLoad = finalScenesToLoad ?? throw new ArgumentNullException(nameof(finalScenesToLoad));
            AutoScenesToUnload = autoScenesToUnload ?? throw new ArgumentNullException(nameof(autoScenesToUnload));
            FinalScenesToUnload = finalScenesToUnload ?? throw new ArgumentNullException(nameof(finalScenesToUnload));
            ActiveSceneKey = activeSceneKey;
            CompletionHandoff = completionHandoff;
            HandoffSessionStateId = Normalize(handoffSessionStateId);
            OperationalSurfaceKind = operationalSurfaceKind;
            InputPolicy = inputPolicy;
            ActivitySavePolicy = activitySavePolicy;
            RouteParticipantSetDefinition = routeParticipantSetDefinition;
            Audio = audio;
            SurfacePresentationProfile = surfacePresentationProfile;
            ActivityPresentationProfile = activityPresentationProfile;
        }

        public string RouteIdentity { get; }
        public SessionOperationalRouteTransitionMode TransitionMode { get; }
        public SceneTransitionProfile TransitionProfile { get; }
        public SessionOperationalRouteLoadingMode LoadingMode { get; }
        public RuntimeLoadingProfileAsset LoadingProfile { get; }
        public IReadOnlyList<SceneKeyAsset> ScenesToLoad { get; }
        public IReadOnlyList<SceneKeyAsset> ScenesToUnload { get; }
        public IReadOnlyList<SceneKeyAsset> FinalScenesToLoad { get; }
        public IReadOnlyList<SceneKeyAsset> AutoScenesToUnload { get; }
        public IReadOnlyList<SceneKeyAsset> FinalScenesToUnload { get; }
        public SceneKeyAsset ActiveSceneKey { get; }
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff { get; }
        public string HandoffSessionStateId { get; }
        public OperationalSurfaceKind OperationalSurfaceKind { get; }
        public SessionOperationalInputPolicy InputPolicy { get; }
        public RouteActivitySavePolicy ActivitySavePolicy { get; }
        public PlayerSetDefinitionAsset RouteParticipantSetDefinition { get; }
        public SessionOperationalRouteAudioCommand Audio { get; }
        public SurfacePresentationProfileAsset SurfacePresentationProfile { get; }
        public ActivityPresentationProfileAsset ActivityPresentationProfile { get; }
        public bool UsesTransition => TransitionMode == SessionOperationalRouteTransitionMode.Profile;
        public bool UsesLoading => LoadingMode != SessionOperationalRouteLoadingMode.None;
        public string TransitionProfileLabel => TransitionProfile != null && !string.IsNullOrWhiteSpace(TransitionProfile.name) ? TransitionProfile.name.Trim() : string.Empty;
        public string LoadingProfileLabel => LoadingProfile != null && !string.IsNullOrWhiteSpace(LoadingProfile.ProfileId) ? LoadingProfile.ProfileId.Trim() : string.Empty;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            (TransitionMode == SessionOperationalRouteTransitionMode.None || TransitionMode == SessionOperationalRouteTransitionMode.Profile) &&
            (!UsesTransition || (TransitionProfile != null && TransitionProfile.TryValidate(out _))) &&
            (LoadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault || LoadingMode == SessionOperationalRouteLoadingMode.None || LoadingMode == SessionOperationalRouteLoadingMode.Profile) &&
            (!UsesLoading || LoadingMode != SessionOperationalRouteLoadingMode.Profile || (LoadingProfile != null && LoadingProfile.TryValidate(out _))) &&
            ActiveSceneKey != null &&
            !string.IsNullOrWhiteSpace(ActiveSceneKey.SceneName) &&
            FinalScenesToLoad is { Count: > 0 } &&
            FinalScenesToUnload != null &&
            AutoScenesToUnload != null &&
            Audio.IsValid &&
            ActivitySavePolicy.IsValid &&
            (CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry || !string.IsNullOrWhiteSpace(HandoffSessionStateId));

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', activeScene='{ResolveSceneName(ActiveSceneKey)}', activeSceneKey='{ActiveSceneKey.name}', transitionMode='{TransitionMode}', transitionProfile='{TransitionProfileLabel}', completionHandoff='{CompletionHandoff}', handoffSessionStateId='{HandoffSessionStateId}', loadActivitySaveOnEnter='{ActivitySavePolicy.LoadActivitySaveOnEnter}', saveActivityOnExit='{ActivitySavePolicy.SaveActivityOnExit}', finalScenesToLoadCount='{FinalScenesToLoad.Count}', autoScenesToUnloadCount='{AutoScenesToUnload.Count}', finalScenesToUnloadCount='{FinalScenesToUnload.Count}'"
                : "<none>";
        }

        private static string ResolveSceneName(SceneKeyAsset sceneKey)
        {
            if (sceneKey == null || string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                return string.Empty;
            }

            return sceneKey.SceneName.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
