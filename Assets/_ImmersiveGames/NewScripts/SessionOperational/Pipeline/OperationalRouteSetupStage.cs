using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteSetupResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteSetupCommand
    {
        public OperationalRouteSetupCommand(
            SessionOperationalRoutePlanResolution planResolution,
            string activeSceneName,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            RuntimeModeConfig runtimeModeConfig,
            bool hasPreviousCompletedRoute,
            string previousRouteIdentity,
            string previousRouteOperationId,
            int previousRouteSequence,
            SceneKeyAsset previousRouteActiveSceneKey,
            bool previousSaveActivityOnExit,
            string previousActivityIdentity,
            string previousActivitySaveKey,
            IReadOnlyList<SceneKeyAsset> previousRouteOwnedLoadedSceneKeys,
            string source,
            string reason)
        {
            PlanResolution = planResolution;
            ActiveSceneName = Normalize(activeSceneName);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            RuntimeModeConfig = runtimeModeConfig;
            HasPreviousCompletedRoute = hasPreviousCompletedRoute;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousRouteOperationId = Normalize(previousRouteOperationId);
            PreviousRouteSequence = previousRouteSequence < 0 ? 0 : previousRouteSequence;
            PreviousRouteActiveSceneKey = previousRouteActiveSceneKey;
            PreviousSaveActivityOnExit = previousSaveActivityOnExit;
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            PreviousActivitySaveKey = Normalize(previousActivitySaveKey);
            PreviousRouteOwnedLoadedSceneKeys = previousRouteOwnedLoadedSceneKeys ?? Array.Empty<SceneKeyAsset>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRoutePlanResolution PlanResolution { get; }
        public string ActiveSceneName { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public RuntimeModeConfig RuntimeModeConfig { get; }
        public bool HasPreviousCompletedRoute { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousRouteOperationId { get; }
        public int PreviousRouteSequence { get; }
        public SceneKeyAsset PreviousRouteActiveSceneKey { get; }
        public bool PreviousSaveActivityOnExit { get; }
        public string PreviousActivityIdentity { get; }
        public string PreviousActivitySaveKey { get; }
        public IReadOnlyList<SceneKeyAsset> PreviousRouteOwnedLoadedSceneKeys { get; }
        public string Source { get; }
        public string Reason { get; }
        public string RouteIdentity => PlanResolution.Plan.RouteIdentity;

        public bool IsValid =>
            PlanResolution.IsValid &&
            !string.IsNullOrWhiteSpace(ActiveSceneName) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            RuntimeModeConfig != null &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct OperationalRouteSetupResult
    {
        public OperationalRouteSetupResult(
            OperationalRouteSetupResultKind kind,
            SessionOperationalRoutePlanResolution planResolution,
            SessionOperationalRouteCommand routeCommand,
            RouteActivitySavePlan routeActivitySavePlan,
            SessionOperationalLoadingCommand loadingCommand,
            string activeSceneName,
            string reason,
            string detail)
        {
            Kind = kind;
            PlanResolution = planResolution;
            RouteCommand = routeCommand;
            RouteActivitySavePlan = routeActivitySavePlan;
            LoadingCommand = loadingCommand;
            ActiveSceneName = Normalize(activeSceneName);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteSetupResultKind Kind { get; }
        public SessionOperationalRoutePlanResolution PlanResolution { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public RouteActivitySavePlan RouteActivitySavePlan { get; }
        public SessionOperationalLoadingCommand LoadingCommand { get; }
        public string ActiveSceneName { get; }
        public string Reason { get; }
        public string Detail { get; }
        public string RouteIdentity => RouteCommand.RouteIdentity;
        public string RouteOperationId => RouteCommand.RouteOperationId;
        public string TransitionId => RouteCommand.TransitionId;
        public int RouteSequence => RouteCommand.RouteSequence;
        public bool IsCompleted => Kind == OperationalRouteSetupResultKind.Completed;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    /// <summary>
    /// Resolve o setup puro da rota operacional: command, loading plan, audio plan e RouteActivitySave plan.
    /// Não fecha cortina, não materializa cenas e não executa side-effects de consumer.
    /// </summary>
    public sealed class OperationalRouteSetupStage
    {
        public OperationalRouteSetupResult Execute(OperationalRouteSetupCommand setupCommand)
        {
            if (!setupCommand.IsValid)
            {
                throw new ArgumentException("OperationalRouteSetupCommand invalido.", nameof(setupCommand));
            }

            SessionOperationalRouteCommand routeCommand = new(
                setupCommand.PlanResolution.Plan,
                setupCommand.RouteOperationId,
                setupCommand.TransitionId,
                setupCommand.RouteSequence,
                setupCommand.Source,
                setupCommand.Reason);

            if (!routeCommand.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalRoute] runtime command invalid routeIdentity='{setupCommand.RouteIdentity}' routeOperationId='{setupCommand.RouteOperationId}' transitionId='{setupCommand.TransitionId}' routeSequence='{setupCommand.RouteSequence}'.");
            }

            RouteActivitySavePlan routeActivitySavePlan = RouteActivitySavePlanResolver.Resolve(
                routeCommand,
                setupCommand.HasPreviousCompletedRoute,
                setupCommand.PreviousRouteIdentity,
                setupCommand.PreviousRouteOperationId,
                setupCommand.PreviousRouteSequence,
                setupCommand.PreviousSaveActivityOnExit,
                setupCommand.PreviousActivityIdentity,
                setupCommand.PreviousActivitySaveKey);

            if (!routeActivitySavePlan.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] RouteActivitySavePlan invalido routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}'.");
            }

            SessionOperationalLoadingCommand loadingCommand = ResolveLoadingCommandOrFail(
                routeCommand.Plan,
                setupCommand.RuntimeModeConfig,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                routeCommand.Source,
                routeCommand.Reason);

            LogSetupFacts(setupCommand, routeCommand, routeActivitySavePlan, loadingCommand);

            return new OperationalRouteSetupResult(
                OperationalRouteSetupResultKind.Completed,
                setupCommand.PlanResolution,
                routeCommand,
                routeActivitySavePlan,
                loadingCommand,
                setupCommand.ActiveSceneName,
                "completed",
                "Operational route setup completed.");
        }

        private static void LogSetupFacts(
            OperationalRouteSetupCommand setupCommand,
            SessionOperationalRouteCommand routeCommand,
            RouteActivitySavePlan routeActivitySavePlan,
            SessionOperationalLoadingCommand loadingCommand)
        {
            if (setupCommand.PlanResolution.UnloadPreviousRouteOwnedScenes)
            {
                LogPreviousRouteUnloadPlan(setupCommand);
            }

            DebugUtility.Log(typeof(OperationalRouteSetupStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteSetupStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{routeCommand.Source}' reason='{routeCommand.Reason}'.",
                DebugUtility.Colors.Info);

            if (loadingCommand.IsEnabled)
            {
                DebugUtility.Log(typeof(OperationalRouteSetupStage),
                    $"[OBS][SessionOperationalPipeline][Loading] LoadingPlanReady routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' loadingScene='{loadingCommand.LoadingSceneName}' showImmediately='{loadingCommand.ShowImmediately}' hideAfterCompletion='{loadingCommand.HideAfterCompletion}' minimumVisibleSeconds='{loadingCommand.MinimumVisibleSeconds:0.###}' finalProgressHoldSeconds='{loadingCommand.FinalProgressHoldSeconds:0.###}' source='{routeCommand.Source}' reason='{routeCommand.Reason}'.",
                    DebugUtility.Colors.Info);
            }

            DebugUtility.Log(typeof(OperationalRouteSetupStage),
                $"[OBS][SessionOperationalPipeline][Audio] RouteAudioPlanReady routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' routeAudioMode='{routeCommand.Audio.RouteAudioMode}' routeAudioTiming='{routeCommand.Audio.RouteAudioTiming}' routeAudioCue='{routeCommand.Audio.RouteAudioCueName}' stopPreviousRouteAudio='{routeCommand.Audio.StopPreviousRouteAudio}' source='{routeCommand.Source}' reason='{routeCommand.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteSetupStage),
                $"[OBS][SessionOperationalPipeline][Route] command='OperationalRouteCommand' routeIdentity='{routeCommand.RouteIdentity}' activeScene='{setupCommand.ActiveSceneName}' activeSceneKey='{routeCommand.ActiveSceneKey?.name ?? string.Empty}' activeSceneImplicitLoad='{setupCommand.PlanResolution.ActiveSceneImplicitLoad}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' completionHandoff='{routeCommand.CompletionHandoff}' finalScenesToLoad=[{FormatSceneNames(routeCommand.FinalScenesToLoad)}] autoScenesToUnload=[{FormatSceneNames(routeCommand.AutoScenesToUnload)}] explicitScenesToUnload=[{FormatSceneNames(setupCommand.PlanResolution.ExplicitScenesToUnload)}] finalScenesToUnload=[{FormatSceneNames(routeCommand.FinalScenesToUnload)}] source='{routeCommand.Source}' reason='{routeCommand.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteSetupStage),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySavePlanReady routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' routeSequence='{routeCommand.RouteSequence}' loadActivitySaveOnEnter='{routeActivitySavePlan.CurrentPolicy.LoadActivitySaveOnEnter}' saveActivityOnExit='{routeActivitySavePlan.CurrentPolicy.SaveActivityOnExit}' loadShouldRun='{routeActivitySavePlan.LoadOnEnter.ShouldLoad}' saveOnExitShouldRun='{routeActivitySavePlan.SaveOnExit.ShouldSave}' saveOnExitSkipKind='{routeActivitySavePlan.SaveOnExit.SkipKind}' source='{routeCommand.Source}' reason='{routeCommand.Reason}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(OperationalRouteSetupStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteSetupCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{routeCommand.Source}' reason='{routeCommand.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private static SessionOperationalLoadingCommand ResolveLoadingCommandOrFail(
            SessionOperationalRoutePlan plan,
            RuntimeModeConfig runtimeModeConfig,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("SessionOperationalRoutePlan is invalid.", nameof(plan));
            }

            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalRouteSetupStage] RuntimeModeConfig obrigatorio ausente para resolver loading.");
            }

            SessionOperationalRouteLoadingMode effectiveLoadingMode = plan.LoadingMode;
            RuntimeLoadingProfileAsset effectiveLoadingProfile = plan.LoadingProfile;

            if (effectiveLoadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault)
            {
                SessionOperationalRuntimeLoadingDefaults loadingDefaults =
                    SessionOperationalRuntimeConfigResolver.ResolveLoadingDefaultsOrFail(runtimeModeConfig);

                effectiveLoadingMode = loadingDefaults.Mode;
                effectiveLoadingProfile = loadingDefaults.Profile;
            }

            if (effectiveLoadingMode == SessionOperationalRouteLoadingMode.Profile)
            {
                if (effectiveLoadingProfile == null)
                {
                    string message = $"[FATAL][Config][SessionOperationalRoute] loadingProfile is required when loadingMode=Profile routeIdentity='{plan.RouteIdentity}'.";
                    DebugUtility.LogError<OperationalRouteSetupStage>(message);
                    throw new InvalidOperationException(message);
                }

                if (!effectiveLoadingProfile.TryValidate(out string profileValidationError))
                {
                    string message = $"[FATAL][Config][SessionOperationalRoute] loadingProfile is invalid routeIdentity='{plan.RouteIdentity}' profile='{effectiveLoadingProfile.ProfileId}' detail='{profileValidationError}'.";
                    DebugUtility.LogError<OperationalRouteSetupStage>(message);
                    throw new InvalidOperationException(message);
                }
            }
            else if (effectiveLoadingMode == SessionOperationalRouteLoadingMode.None)
            {
                effectiveLoadingProfile = null;
            }

            string loadingSceneName = string.Empty;
            if (effectiveLoadingMode != SessionOperationalRouteLoadingMode.None)
            {
                RuntimePersistentScenesPolicyAsset persistentScenesPolicy = RuntimePolicyConfigResolver.ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);
                if (persistentScenesPolicy == null)
                {
                    string message = $"[FATAL][Config][SessionOperationalRouteSetupStage] RuntimePersistentScenesPolicyAsset obrigatorio ausente para loading efetivo routeIdentity='{plan.RouteIdentity}'.";
                    DebugUtility.LogError<OperationalRouteSetupStage>(message);
                    throw new InvalidOperationException(message);
                }

                loadingSceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(
                    RuntimePersistentSceneRole.Loading,
                    nameof(OperationalRouteSetupStage));
            }

            SessionOperationalLoadingCommand loadingCommand = new(
                plan.RouteIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                effectiveLoadingMode,
                effectiveLoadingProfile,
                loadingSceneName,
                effectiveLoadingProfile != null ? effectiveLoadingProfile.FinalProgressHoldSeconds : 0f);

            if (!loadingCommand.IsValid)
            {
                string message = $"[FATAL][Config][SessionOperationalRouteSetupStage] loading command invalid routeIdentity='{plan.RouteIdentity}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' loadingSceneName='{loadingCommand.LoadingSceneName}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<OperationalRouteSetupStage>(message);
                throw new InvalidOperationException(message);
            }

            return loadingCommand;
        }

        private static void LogPreviousRouteUnloadPlan(OperationalRouteSetupCommand setupCommand)
        {
            DebugUtility.Log(typeof(OperationalRouteSetupStage),
                $"[OBS][SessionOperationalPipeline][Route] unload='previous_route_owned_scenes_applied' previousRouteIdentity='{setupCommand.PreviousRouteIdentity}' currentRouteIdentity='{setupCommand.RouteIdentity}' previousRouteSequence='{setupCommand.PreviousRouteSequence}' previousRouteActiveSceneKey='{setupCommand.PreviousRouteActiveSceneKey?.name ?? string.Empty}' previousRouteOwnedSceneKeys=[{FormatSceneNames(setupCommand.PreviousRouteOwnedLoadedSceneKeys)}] autoScenesToUnload=[{FormatSceneNames(setupCommand.PlanResolution.Plan.AutoScenesToUnload)}] explicitScenesToUnload=[{FormatSceneNames(setupCommand.PlanResolution.ExplicitScenesToUnload)}] finalScenesToUnload=[{FormatSceneNames(setupCommand.PlanResolution.Plan.FinalScenesToUnload)}].",
                DebugUtility.Colors.Info);
        }

        private static string FormatSceneNames(IReadOnlyList<SceneKeyAsset> sceneKeys)
        {
            if (sceneKeys == null || sceneKeys.Count == 0)
            {
                return "<none>";
            }

            List<string> sceneNames = new(sceneKeys.Count);
            for (int i = 0; i < sceneKeys.Count; i++)
            {
                sceneNames.Add(ResolveSceneName(sceneKeys[i], $"sceneKeys[{i}]"));
            }

            return string.Join(", ", sceneNames);
        }

        private static string ResolveSceneName(SceneKeyAsset sceneKey, string fieldName)
        {
            if (sceneKey == null)
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalRouteSetupStage] {fieldName} is required.");
            }

            if (string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalRouteSetupStage] {fieldName} requires a SceneKeyAsset with a non-empty SceneName. asset='{sceneKey.name}'.");
            }

            return sceneKey.SceneName.Trim();
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
