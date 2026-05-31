using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.CameraPresentation.Authoring;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using UnityEngine;
using UnityEngine.Serialization;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum SessionOperationalRouteCompletionHandoffKind
    {
        NoHandoff = 0,
        SessionActivityEntry = 1,
    }

    public enum SessionOperationalRouteTransitionMode
    {
        None = 0,
        Profile = 1,
    }

    public enum SessionOperationalRouteAudioMode
    {
        None = 0,
        Cue = 1,
    }

    public enum SessionOperationalRouteAudioTiming
    {
        BeforeFadeOut = 0,
    }

    public enum OperationalSurfaceKind
    {
        None = 0,
        FrontendMenu = 1,
        SessionActivity = 2,
        Overlay = 3,
        LoadingOnly = 4,
    }

    public readonly struct RouteActivitySavePolicy
    {
        public RouteActivitySavePolicy(
            bool loadActivitySaveOnEnter,
            bool saveActivityOnExit)
        {
            LoadActivitySaveOnEnter = loadActivitySaveOnEnter;
            SaveActivityOnExit = saveActivityOnExit;
        }

        public bool LoadActivitySaveOnEnter { get; }
        public bool SaveActivityOnExit { get; }
        public bool IsValid => true;

        public override string ToString()
        {
            return $"loadActivitySaveOnEnter='{LoadActivitySaveOnEnter}' saveActivityOnExit='{SaveActivityOnExit}'";
        }
    }

    [CreateAssetMenu(
        fileName = "SessionOperationalRoute",
        menuName = "ImmersiveGames/NewScripts/Session Operational/Operational Route/OperationalRoute",
        order = 40)]
    public sealed class OperationalRouteAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string routeIdentity;

        [Header("Transition")]
        [SerializeField] private SessionOperationalRouteTransitionMode transitionMode = SessionOperationalRouteTransitionMode.None;
        [SerializeField] private SceneTransitionProfile transitionProfile;

        [Header("Loading")]
        [SerializeField] private SessionOperationalRouteLoadingMode loadingMode = SessionOperationalRouteLoadingMode.RuntimeDefault;
        [SerializeField] private RuntimeLoadingProfileAsset loadingProfile;

        [Header("Scenes")]
        [SerializeField] private List<SceneKeyAsset> scenesToLoad = new();
        [SerializeField] private List<SceneKeyAsset> scenesToUnload = new();
        [SerializeField] private SceneKeyAsset activeScene;
        [SerializeField] private bool unloadPreviousRouteOwnedScenes;

        [Header("Completion")]
        [SerializeField] private SessionOperationalRouteCompletionHandoffKind completionHandoff = SessionOperationalRouteCompletionHandoffKind.NoHandoff;
        [SerializeField] private string handoffSessionStateId;
        [SerializeField] private OperationalSurfaceKind operationalSurfaceKind = OperationalSurfaceKind.None;

        [Header("Input")]
        [SerializeField] private SessionOperationalInputPolicy inputPolicy = SessionOperationalInputPolicy.Unknown;

        [Header("Route Activity Save")]
        [SerializeField] private bool loadActivitySaveOnEnter;
        [SerializeField] private bool saveActivityOnExit;

        [Header("Route Session Participants")]
        [FormerlySerializedAs("actorSetDefinition")]
        [FormerlySerializedAs("playerSetDefinition")]
        [SerializeField] private PlayerSetDefinitionAsset routeParticipantSetDefinition;

        [Header("Audio")]
        [SerializeField] private SessionOperationalRouteAudioMode routeAudioMode = SessionOperationalRouteAudioMode.None;
        [SerializeField] private AudioCueAsset routeAudioCue;
        [SerializeField] private SessionOperationalRouteAudioTiming routeAudioTiming = SessionOperationalRouteAudioTiming.BeforeFadeOut;
        [SerializeField] private bool stopPreviousRouteAudio;

        [Header("Surface Presentation")]
        // Perfil declarativo de apresentação de surface; não executa câmera por si só.
        [SerializeField] private SurfacePresentationProfileAsset surfacePresentationProfile;

        [Header("Activity Presentation")]
        // Perfil declarativo de apresentação de activity; não executa câmera por si só.
        [SerializeField] private ActivityPresentationProfileAsset activityPresentationProfile;

        public string RouteIdentity => Normalize(routeIdentity);
        public SessionOperationalRouteTransitionMode TransitionMode => transitionMode;
        public SceneTransitionProfile TransitionProfile => transitionProfile;
        public SessionOperationalRouteLoadingMode LoadingMode => loadingMode;
        public RuntimeLoadingProfileAsset LoadingProfile => loadingProfile;
        public IReadOnlyList<SceneKeyAsset> ScenesToLoad => scenesToLoad;
        public IReadOnlyList<SceneKeyAsset> ScenesToUnload => scenesToUnload;
        public SceneKeyAsset ActiveSceneKey => activeScene;
        public bool UnloadPreviousRouteOwnedScenes => unloadPreviousRouteOwnedScenes;
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff => completionHandoff;
        public string HandoffSessionStateId => Normalize(handoffSessionStateId);
        public OperationalSurfaceKind OperationalSurfaceKind => operationalSurfaceKind;
        public SessionOperationalInputPolicy InputPolicy => inputPolicy;
        public bool LoadActivitySaveOnEnter => loadActivitySaveOnEnter;
        public bool SaveActivityOnExit => saveActivityOnExit;
        public RouteActivitySavePolicy ActivitySavePolicy => new(loadActivitySaveOnEnter, saveActivityOnExit);
        public PlayerSetDefinitionAsset RouteParticipantSetDefinition => routeParticipantSetDefinition;
        public SessionOperationalRouteAudioMode RouteAudioMode => routeAudioMode;
        public AudioCueAsset RouteAudioCue => routeAudioCue;
        public SessionOperationalRouteAudioTiming RouteAudioTiming => routeAudioTiming;
        public bool StopPreviousRouteAudio => stopPreviousRouteAudio;
        public SurfacePresentationProfileAsset SurfacePresentationProfile => surfacePresentationProfile;
        public ActivityPresentationProfileAsset ActivityPresentationProfile => activityPresentationProfile;
        public bool UsesTransition => TransitionMode == SessionOperationalRouteTransitionMode.Profile;
        public bool UsesLoading => LoadingMode != SessionOperationalRouteLoadingMode.None;
        public string LoadingProfileLabel => loadingProfile != null && !string.IsNullOrWhiteSpace(loadingProfile.ProfileId) ? loadingProfile.ProfileId.Trim() : string.Empty;
        public string TransitionProfileLabel => transitionProfile != null && !string.IsNullOrWhiteSpace(transitionProfile.name) ? transitionProfile.name.Trim() : string.Empty;

        public bool IsValid
        {
            get
            {
                return TryValidate(out _);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            if (TryValidate(out string errorMessage) || string.IsNullOrWhiteSpace(errorMessage))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(OperationalRouteAsset),
                $"[Config][Editor] routeIdentity='{RouteIdentity}' invalida. detail='{errorMessage}'");
        }
#endif

        public bool TryValidate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(RouteIdentity))
            {
                errorMessage = "routeIdentity is required.";
                return false;
            }

            bool validTransitionMode =
                transitionMode == SessionOperationalRouteTransitionMode.None ||
                transitionMode == SessionOperationalRouteTransitionMode.Profile;

            if (!validTransitionMode)
            {
                errorMessage = $"transitionMode is invalid routeIdentity='{RouteIdentity}' transitionMode='{transitionMode}'.";
                return false;
            }

            if (transitionMode == SessionOperationalRouteTransitionMode.Profile)
            {
                if (transitionProfile == null)
                {
                    errorMessage = $"transitionProfile is required when transitionMode=Profile routeIdentity='{RouteIdentity}'.";
                    return false;
                }

                if (!transitionProfile.TryValidate(out string profileValidationError))
                {
                    errorMessage = $"transitionProfile is invalid routeIdentity='{RouteIdentity}' profile='{TransitionProfileLabel}' detail='{profileValidationError}'.";
                    return false;
                }
            }

            bool validLoadingMode =
                loadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault ||
                loadingMode == SessionOperationalRouteLoadingMode.None ||
                loadingMode == SessionOperationalRouteLoadingMode.Profile;

            if (!validLoadingMode)
            {
                errorMessage = $"loadingMode is invalid routeIdentity='{RouteIdentity}' loadingMode='{loadingMode}'.";
                return false;
            }

            if (loadingMode == SessionOperationalRouteLoadingMode.Profile)
            {
                if (loadingProfile == null)
                {
                    errorMessage = $"loadingProfile is required when loadingMode=Profile routeIdentity='{RouteIdentity}'.";
                    return false;
                }

                if (!loadingProfile.TryValidate(out string loadingProfileValidationError))
                {
                    errorMessage = $"loadingProfile is invalid routeIdentity='{RouteIdentity}' profile='{LoadingProfileLabel}' detail='{loadingProfileValidationError}'.";
                    return false;
                }
            }

            if (!TryResolveSceneName(ActiveSceneKey, nameof(activeScene), out string activeSceneName, out errorMessage))
            {
                return false;
            }

            if (scenesToLoad == null || scenesToUnload == null)
            {
                errorMessage = "scenesToLoad and scenesToUnload are required.";
                return false;
            }

            if (!ValidateSceneList(scenesToLoad, nameof(scenesToLoad), out string loadValidationError))
            {
                errorMessage = loadValidationError;
                return false;
            }

            if (!ValidateSceneList(scenesToUnload, nameof(scenesToUnload), out string unloadValidationError))
            {
                errorMessage = unloadValidationError;
                return false;
            }

            if (ContainsScene(scenesToUnload, activeSceneName))
            {
                errorMessage = $"activeScene cannot be listed in scenesToUnload routeIdentity='{RouteIdentity}' activeScene='{activeSceneName}'.";
                return false;
            }

            bool validHandoffKind =
                completionHandoff == SessionOperationalRouteCompletionHandoffKind.NoHandoff ||
                completionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry;

            if (!validHandoffKind)
            {
                errorMessage = $"completionHandoff is invalid routeIdentity='{RouteIdentity}' completionHandoff='{completionHandoff}'.";
                return false;
            }

            if (completionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry &&
                string.IsNullOrWhiteSpace(HandoffSessionStateId))
            {
                errorMessage = $"handoffSessionStateId is required when completionHandoff=SessionActivityEntry routeIdentity='{RouteIdentity}'.";
                return false;
            }

            bool validOperationalSurfaceKind =
                operationalSurfaceKind == OperationalSurfaceKind.None ||
                operationalSurfaceKind == OperationalSurfaceKind.FrontendMenu ||
                operationalSurfaceKind == OperationalSurfaceKind.SessionActivity ||
                operationalSurfaceKind == OperationalSurfaceKind.Overlay ||
                operationalSurfaceKind == OperationalSurfaceKind.LoadingOnly;

            if (!validOperationalSurfaceKind)
            {
                errorMessage = $"operationalSurfaceKind is invalid routeIdentity='{RouteIdentity}' operationalSurfaceKind='{operationalSurfaceKind}'.";
                return false;
            }

            if (completionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry &&
                operationalSurfaceKind != OperationalSurfaceKind.SessionActivity)
            {
                errorMessage = $"completionHandoff=SessionActivityEntry requires operationalSurfaceKind=SessionActivity routeIdentity='{RouteIdentity}' operationalSurfaceKind='{operationalSurfaceKind}'.";
                return false;
            }

            if (completionHandoff == SessionOperationalRouteCompletionHandoffKind.NoHandoff &&
                operationalSurfaceKind != OperationalSurfaceKind.None &&
                operationalSurfaceKind != OperationalSurfaceKind.FrontendMenu &&
                operationalSurfaceKind != OperationalSurfaceKind.Overlay &&
                operationalSurfaceKind != OperationalSurfaceKind.LoadingOnly)
            {
                errorMessage = $"completionHandoff=NoHandoff requires operationalSurfaceKind in [None, FrontendMenu, Overlay, LoadingOnly] routeIdentity='{RouteIdentity}' operationalSurfaceKind='{operationalSurfaceKind}'.";
                return false;
            }

            bool validInputPolicy =
                inputPolicy == SessionOperationalInputPolicy.MenuNavigation ||
                inputPolicy == SessionOperationalInputPolicy.ActivityGameplay ||
                inputPolicy == SessionOperationalInputPolicy.OverlayNavigation ||
                inputPolicy == SessionOperationalInputPolicy.InputLocked;

            if (!validInputPolicy)
            {
                errorMessage = $"inputPolicy is invalid routeIdentity='{RouteIdentity}' inputPolicy='{inputPolicy}'.";
                return false;
            }

            if (loadActivitySaveOnEnter && completionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                errorMessage = $"loadActivitySaveOnEnter requires completionHandoff=SessionActivityEntry routeIdentity='{RouteIdentity}' completionHandoff='{completionHandoff}'.";
                return false;
            }

            if (saveActivityOnExit && completionHandoff != SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
            {
                errorMessage = $"saveActivityOnExit requires completionHandoff=SessionActivityEntry routeIdentity='{RouteIdentity}' completionHandoff='{completionHandoff}'.";
                return false;
            }

            if (routeParticipantSetDefinition != null && !routeParticipantSetDefinition.TryValidate(out string playerSetValidationError))
            {
                errorMessage = $"routeParticipantSetDefinition is invalid routeIdentity='{RouteIdentity}' asset='{routeParticipantSetDefinition.name}' detail='{playerSetValidationError}'.";
                return false;
            }

            bool validAudioMode =
                routeAudioMode == SessionOperationalRouteAudioMode.None ||
                routeAudioMode == SessionOperationalRouteAudioMode.Cue;

            if (!validAudioMode)
            {
                errorMessage = $"routeAudioMode is invalid routeIdentity='{RouteIdentity}' routeAudioMode='{routeAudioMode}'.";
                return false;
            }

            if (routeAudioMode == SessionOperationalRouteAudioMode.Cue)
            {
                if (routeAudioCue == null)
                {
                    errorMessage = $"routeAudioCue is required when routeAudioMode=Cue routeIdentity='{RouteIdentity}'.";
                    return false;
                }

                if (!routeAudioCue.ValidateRuntime(out string routeAudioCueValidationError))
                {
                    errorMessage = $"routeAudioCue is invalid routeIdentity='{RouteIdentity}' cue='{routeAudioCue.name}' detail='{routeAudioCueValidationError}'.";
                    return false;
                }
            }
            else if (routeAudioCue != null)
            {
                errorMessage = $"routeAudioCue must be null when routeAudioMode=None routeIdentity='{RouteIdentity}' cue='{routeAudioCue.name}'.";
                return false;
            }

            if (routeAudioTiming != SessionOperationalRouteAudioTiming.BeforeFadeOut)
            {
                errorMessage = $"routeAudioTiming is invalid routeIdentity='{RouteIdentity}' routeAudioTiming='{routeAudioTiming}'.";
                return false;
            }

            if (surfacePresentationProfile != null &&
                !surfacePresentationProfile.TryValidate(out string surfacePresentationProfileValidationReason))
            {
                errorMessage = $"route_surface_presentation_profile_invalid:{surfacePresentationProfileValidationReason} routeIdentity='{RouteIdentity}' profile='{surfacePresentationProfile.name}'.";
                return false;
            }

            if (completionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry &&
                surfacePresentationProfile != null &&
                surfacePresentationProfile.RouteCameraPresentationMode == CameraPresentation.Models.RouteCameraPresentationMode.SurfaceOnly)
            {
                errorMessage =
                    $"completionHandoff=SessionActivityEntry does not allow surfacePresentationProfile routeCameraPresentationMode=SurfaceOnly routeIdentity='{RouteIdentity}' profile='{surfacePresentationProfile.name}' mode='{surfacePresentationProfile.RouteCameraPresentationMode}'.";
                return false;
            }

            if (activityPresentationProfile != null &&
                !activityPresentationProfile.TryValidate(out string activityPresentationProfileValidationReason))
            {
                errorMessage = $"route_activity_presentation_profile_invalid:{activityPresentationProfileValidationReason} routeIdentity='{RouteIdentity}' profile='{activityPresentationProfile.name}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool TryValidateAgainstPersistentScenesPolicy(
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            out string errorMessage)
        {
            if (!TryValidate(out errorMessage))
            {
                return false;
            }

            if (persistentScenesPolicy == null)
            {
                errorMessage = string.Empty;
                return true;
            }

            IReadOnlyList<string> persistentSceneNames = persistentScenesPolicy.ResolveSceneNamesOrFail(nameof(OperationalRouteAsset));
            HashSet<string> persistentSceneSet = new(persistentSceneNames, StringComparer.Ordinal);

            if (!TryResolveSceneName(ActiveSceneKey, nameof(activeScene), out string activeSceneName, out errorMessage))
            {
                return false;
            }

            if (persistentSceneSet.Contains(activeSceneName))
            {
                errorMessage = $"activeScene cannot be runtime persistent routeIdentity='{RouteIdentity}' activeScene='{activeSceneName}' policyId='{persistentScenesPolicy.PolicyId}'.";
                return false;
            }

            if (TryFindSceneConflict(scenesToLoad, persistentSceneSet, nameof(scenesToLoad), RouteIdentity, out errorMessage))
            {
                return false;
            }

            if (TryFindSceneConflict(scenesToUnload, persistentSceneSet, nameof(scenesToUnload), RouteIdentity, out errorMessage))
            {
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool TryResolveSceneName(SceneKeyAsset sceneKey, string fieldName, out string sceneName, out string errorMessage)
        {
            sceneName = string.Empty;
            errorMessage = string.Empty;

            if (sceneKey == null)
            {
                errorMessage = $"{fieldName} is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                errorMessage = $"{fieldName} requires a SceneKeyAsset with a non-empty SceneName. asset='{sceneKey.name}'.";
                return false;
            }

            sceneName = sceneKey.SceneName.Trim();
            return true;
        }

        private static bool ValidateSceneList(IReadOnlyList<SceneKeyAsset> scenes, string fieldName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (scenes == null)
            {
                errorMessage = $"{fieldName} is required.";
                return false;
            }

            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!TryResolveSceneName(scenes[i], $"{fieldName}[{i}]", out string normalized, out errorMessage))
                {
                    return false;
                }

                if (!dedupe.Add(normalized))
                {
                    errorMessage = $"{fieldName} contains duplicate scene='{normalized}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool ContainsScene(IReadOnlyList<SceneKeyAsset> scenes, string sceneName)
        {
            if (scenes == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            string normalizedSceneName = Normalize(sceneName);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (!TryResolveSceneName(scenes[i], $"scenes[{i}]", out string normalized, out _))
                {
                    continue;
                }

                if (string.Equals(normalized, normalizedSceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindSceneConflict(
            IReadOnlyList<SceneKeyAsset> scenes,
            HashSet<string> persistentSceneSet,
            string fieldName,
            string routeIdentity,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (scenes == null || persistentSceneSet == null || persistentSceneSet.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < scenes.Count; i++)
            {
                if (!TryResolveSceneName(scenes[i], $"{fieldName}[{i}]", out string normalizedSceneName, out errorMessage))
                {
                    return true;
                }

                if (persistentSceneSet.Contains(normalizedSceneName))
                {
                    errorMessage = $"{fieldName} cannot contain runtime persistent scene='{normalizedSceneName}'. routeIdentity='{routeIdentity}'.";
                    return true;
                }
            }

            return false;
        }

    }

    public readonly struct SessionOperationalRouteCommand
    {
        public SessionOperationalRouteCommand(
            SessionOperationalRoutePlan plan,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            Plan = plan;
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRoutePlan Plan { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }
        public string RouteIdentity => Plan.RouteIdentity;
        public SessionOperationalRouteTransitionMode TransitionMode => Plan.TransitionMode;
        public SceneTransitionProfile TransitionProfile => Plan.TransitionProfile;
        public SessionOperationalRouteAudioCommand Audio => Plan.Audio;
        public RouteActivitySavePolicy ActivitySavePolicy => Plan.ActivitySavePolicy;
        public IReadOnlyList<SceneKeyAsset> ScenesToLoad => Plan.ScenesToLoad;
        public IReadOnlyList<SceneKeyAsset> ScenesToUnload => Plan.ScenesToUnload;
        public IReadOnlyList<SceneKeyAsset> FinalScenesToLoad => Plan.FinalScenesToLoad;
        public IReadOnlyList<SceneKeyAsset> AutoScenesToUnload => Plan.AutoScenesToUnload;
        public IReadOnlyList<SceneKeyAsset> FinalScenesToUnload => Plan.FinalScenesToUnload;
        public SceneKeyAsset ActiveSceneKey => Plan.ActiveSceneKey;
        public SessionOperationalRouteCompletionHandoffKind CompletionHandoff => Plan.CompletionHandoff;
        public string HandoffSessionStateId => Plan.HandoffSessionStateId;
        public OperationalSurfaceKind SurfaceKind => Plan.OperationalSurfaceKind;
        public SessionOperationalInputPolicy InputPolicy => Plan.InputPolicy;
        public PlayerSetDefinitionAsset RouteParticipantSetDefinition => Plan.RouteParticipantSetDefinition;
        public SurfacePresentationProfileAsset SurfacePresentationProfile => Plan.SurfacePresentationProfile;
        public ActivityPresentationProfileAsset ActivityPresentationProfile => Plan.ActivityPresentationProfile;
        public bool UsesTransition => Plan.UsesTransition;
        public string TransitionProfileLabel => Plan.TransitionProfileLabel;

        public bool IsValid =>
            Plan.IsValid &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', activeScene='{ResolveSceneName(ActiveSceneKey)}', activeSceneKey='{ActiveSceneKey.name}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', transitionMode='{TransitionMode}', transitionProfile='{TransitionProfileLabel}', routeAudioMode='{Audio.RouteAudioMode}', routeAudioTiming='{Audio.RouteAudioTiming}', routeAudioCue='{Audio.RouteAudioCueName}', stopPreviousRouteAudio='{Audio.StopPreviousRouteAudio}', completionHandoff='{CompletionHandoff}', handoffSessionStateId='{HandoffSessionStateId}', loadActivitySaveOnEnter='{ActivitySavePolicy.LoadActivitySaveOnEnter}', saveActivityOnExit='{ActivitySavePolicy.SaveActivityOnExit}', finalScenesToLoadCount='{FinalScenesToLoad.Count}', autoScenesToUnloadCount='{AutoScenesToUnload.Count}', finalScenesToUnloadCount='{FinalScenesToUnload.Count}', source='{Source}', reason='{Reason}'"
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

    public readonly struct SessionOperationalRouteAudioCommand
    {
        public SessionOperationalRouteAudioCommand(
            SessionOperationalRouteAudioMode routeAudioMode,
            AudioCueAsset routeAudioCue,
            SessionOperationalRouteAudioTiming routeAudioTiming,
            bool stopPreviousRouteAudio)
        {
            RouteAudioMode = routeAudioMode;
            RouteAudioCue = routeAudioCue;
            RouteAudioTiming = routeAudioTiming;
            StopPreviousRouteAudio = stopPreviousRouteAudio;
        }

        public SessionOperationalRouteAudioMode RouteAudioMode { get; }
        public AudioCueAsset RouteAudioCue { get; }
        public SessionOperationalRouteAudioTiming RouteAudioTiming { get; }
        public bool StopPreviousRouteAudio { get; }
        public string RouteAudioCueName => RouteAudioCue != null ? RouteAudioCue.name : "<none>";

        public bool IsValid
        {
            get
            {
                if (RouteAudioMode == SessionOperationalRouteAudioMode.None)
                {
                    return RouteAudioCue == null && RouteAudioTiming == SessionOperationalRouteAudioTiming.BeforeFadeOut;
                }

                return RouteAudioMode == SessionOperationalRouteAudioMode.Cue &&
                       RouteAudioCue != null &&
                       RouteAudioTiming == SessionOperationalRouteAudioTiming.BeforeFadeOut;
            }
        }

        public override string ToString()
        {
            return IsValid
                ? $"routeAudioMode='{RouteAudioMode}', routeAudioTiming='{RouteAudioTiming}', routeAudioCue='{RouteAudioCueName}', stopPreviousRouteAudio='{StopPreviousRouteAudio}'"
                : "<none>";
        }
    }

    public readonly struct SessionOperationalRouteCompletedFact
    {
        public SessionOperationalRouteCompletedFact(
            SessionOperationalRouteCommand command,
            string correlationId,
            string message)
        {
            Command = command;
            CorrelationId = Normalize(correlationId);
            Message = Normalize(message);
        }

        public SessionOperationalRouteCommand Command { get; }
        public string CorrelationId { get; }
        public string Message { get; }
        public string RouteIdentity => Command.RouteIdentity;
        public string RouteOperationId => Command.RouteOperationId;
        public string TransitionId => Command.TransitionId;
        public int RouteSequence => Command.RouteSequence;
        public bool IsValid => Command.IsValid && !string.IsNullOrWhiteSpace(CorrelationId);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', correlationId='{CorrelationId}', message='{Message}'"
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

}


