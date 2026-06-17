using System;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public readonly struct RouteActivitySavePlan
    {
        public RouteActivitySavePlan(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            RouteActivitySavePolicy currentPolicy,
            RouteActivitySaveLoadPlan loadOnEnter,
            RouteActivitySaveOnExitPlan saveOnExit)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            CurrentPolicy = currentPolicy;
            LoadOnEnter = loadOnEnter;
            SaveOnExit = saveOnExit;
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public RouteActivitySavePolicy CurrentPolicy { get; }
        public RouteActivitySaveLoadPlan LoadOnEnter { get; }
        public RouteActivitySaveOnExitPlan SaveOnExit { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            CurrentPolicy.IsValid &&
            LoadOnEnter.IsValid &&
            SaveOnExit.IsValid;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct RouteActivitySaveLoadPlan
    {
        public RouteActivitySaveLoadPlan(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            bool loadActivitySaveOnEnter,
            bool shouldLoad,
            RouteActivitySaveSkipKind skipKind,
            string skipDetail)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            ActivityIdentity = Normalize(activityIdentity);
            LoadActivitySaveOnEnter = loadActivitySaveOnEnter;
            ShouldLoad = shouldLoad;
            SkipKind = skipKind;
            SkipDetail = Normalize(skipDetail);
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string ActivityIdentity { get; }
        public bool LoadActivitySaveOnEnter { get; }
        public bool ShouldLoad { get; }
        public RouteActivitySaveSkipKind SkipKind { get; }
        public string SkipDetail { get; }

        public bool IsSkipped => !ShouldLoad;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            (ShouldLoad || SkipKind != RouteActivitySaveSkipKind.None);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct RouteActivitySaveOnExitPlan
    {
        public RouteActivitySaveOnExitPlan(
            string currentRouteIdentity,
            string currentRouteOperationId,
            string currentTransitionId,
            int currentRouteSequence,
            bool hasPreviousRoute,
            string previousRouteIdentity,
            string previousRouteOperationId,
            int previousRouteSequence,
            bool previousSaveActivityOnExit,
            RouteActivitySaveContributorScopePolicy previousRouteContributorScopePolicy,
            string previousActivityIdentity,
            string previousActivitySaveKey,
            bool shouldSave,
            RouteActivitySaveSkipKind skipKind,
            string skipDetail)
        {
            CurrentRouteIdentity = Normalize(currentRouteIdentity);
            CurrentRouteOperationId = Normalize(currentRouteOperationId);
            CurrentTransitionId = Normalize(currentTransitionId);
            CurrentRouteSequence = currentRouteSequence < 0 ? 0 : currentRouteSequence;
            HasPreviousRoute = hasPreviousRoute;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousRouteOperationId = Normalize(previousRouteOperationId);
            PreviousRouteSequence = previousRouteSequence < 0 ? 0 : previousRouteSequence;
            PreviousSaveActivityOnExit = previousSaveActivityOnExit;
            PreviousRouteContributorScopePolicy = previousRouteContributorScopePolicy;
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            PreviousActivitySaveKey = Normalize(previousActivitySaveKey);
            ShouldSave = shouldSave;
            SkipKind = skipKind;
            SkipDetail = Normalize(skipDetail);
        }

        public string CurrentRouteIdentity { get; }
        public string CurrentRouteOperationId { get; }
        public string CurrentTransitionId { get; }
        public int CurrentRouteSequence { get; }
        public bool HasPreviousRoute { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousRouteOperationId { get; }
        public int PreviousRouteSequence { get; }
        public bool PreviousSaveActivityOnExit { get; }
        public RouteActivitySaveContributorScopePolicy PreviousRouteContributorScopePolicy { get; }
        public string PreviousActivityIdentity { get; }
        public string PreviousActivitySaveKey { get; }
        public bool ShouldSave { get; }
        public RouteActivitySaveSkipKind SkipKind { get; }
        public string SkipDetail { get; }

        public bool IsSkipped => !ShouldSave;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(CurrentRouteIdentity) &&
            !string.IsNullOrWhiteSpace(CurrentRouteOperationId) &&
            !string.IsNullOrWhiteSpace(CurrentTransitionId) &&
            CurrentRouteSequence > 0 &&
            (ShouldSave || SkipKind != RouteActivitySaveSkipKind.None);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static class RouteActivitySavePlanResolver
    {
        public static RouteActivitySavePlan Resolve(
            SessionOperationalRouteCommand command,
            bool hasPreviousRoute,
            string previousRouteIdentity,
            string previousRouteOperationId,
            int previousRouteSequence,
            bool previousSaveActivityOnExit,
            RouteActivitySaveContributorScopePolicy previousRouteContributorScopePolicy,
            string previousActivityIdentity,
            string previousActivitySaveKey)
        {
            if (!command.IsValid)
            {
                throw new ArgumentException("SessionOperationalRouteCommand invalido para RouteActivitySavePlan.", nameof(command));
            }

            var loadOnEnter = ResolveLoadOnEnter(command);
            var saveOnExit = ResolveSaveOnExit(
                command,
                hasPreviousRoute,
                previousRouteIdentity,
                previousRouteOperationId,
                previousRouteSequence,
                previousSaveActivityOnExit,
                previousRouteContributorScopePolicy,
                previousActivityIdentity,
                previousActivitySaveKey);

            return new RouteActivitySavePlan(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.ActivitySavePolicy,
                loadOnEnter,
                saveOnExit);
        }

        private static RouteActivitySaveLoadPlan ResolveLoadOnEnter(SessionOperationalRouteCommand command)
        {
            bool shouldLoad = command.ActivitySavePolicy.LoadActivitySaveOnEnter;
            return new RouteActivitySaveLoadPlan(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.HandoffSessionStateId,
                command.ActivitySavePolicy.LoadActivitySaveOnEnter,
                shouldLoad,
                shouldLoad ? RouteActivitySaveSkipKind.None : RouteActivitySaveSkipKind.DisabledByRoute,
                shouldLoad ? string.Empty : "load-on-enter desabilitado na politica da rota atual.");
        }

        private static RouteActivitySaveOnExitPlan ResolveSaveOnExit(
            SessionOperationalRouteCommand command,
            bool hasPreviousRoute,
            string previousRouteIdentity,
            string previousRouteOperationId,
            int previousRouteSequence,
            bool previousSaveActivityOnExit,
            RouteActivitySaveContributorScopePolicy previousRouteContributorScopePolicy,
            string previousActivityIdentity,
            string previousActivitySaveKey)
        {
            if (!hasPreviousRoute)
            {
                return CreateSkippedSavePlan(
                    command,
                    hasPreviousRoute,
                    previousRouteIdentity,
                    previousRouteOperationId,
                    previousRouteSequence,
                    previousSaveActivityOnExit,
                    previousRouteContributorScopePolicy,
                    previousActivityIdentity,
                    previousActivitySaveKey,
                    RouteActivitySaveSkipKind.NoPreviousRoute,
                    "previous completed route snapshot ausente.");
            }

            if (!previousSaveActivityOnExit)
            {
                return CreateSkippedSavePlan(
                    command,
                    hasPreviousRoute,
                    previousRouteIdentity,
                    previousRouteOperationId,
                    previousRouteSequence,
                    previousSaveActivityOnExit,
                    previousRouteContributorScopePolicy,
                    previousActivityIdentity,
                    previousActivitySaveKey,
                    RouteActivitySaveSkipKind.DisabledByPreviousRoute,
                    "save-on-exit desabilitado na politica da rota anterior.");
            }

            if (string.IsNullOrWhiteSpace(previousActivityIdentity))
            {
                return CreateSkippedSavePlan(
                    command,
                    hasPreviousRoute,
                    previousRouteIdentity,
                    previousRouteOperationId,
                    previousRouteSequence,
                    previousSaveActivityOnExit,
                    previousRouteContributorScopePolicy,
                    previousActivityIdentity,
                    previousActivitySaveKey,
                    RouteActivitySaveSkipKind.NoSessionActivity,
                    "activity identity da rota anterior ausente.");
            }

            if (string.IsNullOrWhiteSpace(previousActivitySaveKey))
            {
                return CreateSkippedSavePlan(
                    command,
                    hasPreviousRoute,
                    previousRouteIdentity,
                    previousRouteOperationId,
                    previousRouteSequence,
                    previousSaveActivityOnExit,
                    previousRouteContributorScopePolicy,
                    previousActivityIdentity,
                    previousActivitySaveKey,
                    RouteActivitySaveSkipKind.Unknown,
                    "activity save key da rota anterior ausente.");
            }

            return new RouteActivitySaveOnExitPlan(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                hasPreviousRoute,
                previousRouteIdentity,
                previousRouteOperationId,
                previousRouteSequence,
                previousSaveActivityOnExit,
                previousRouteContributorScopePolicy,
                previousActivityIdentity,
                previousActivitySaveKey,
                shouldSave: true,
                RouteActivitySaveSkipKind.None,
                string.Empty);
        }

        private static RouteActivitySaveOnExitPlan CreateSkippedSavePlan(
            SessionOperationalRouteCommand command,
            bool hasPreviousRoute,
            string previousRouteIdentity,
            string previousRouteOperationId,
            int previousRouteSequence,
            bool previousSaveActivityOnExit,
            RouteActivitySaveContributorScopePolicy previousRouteContributorScopePolicy,
            string previousActivityIdentity,
            string previousActivitySaveKey,
            RouteActivitySaveSkipKind skipKind,
            string skipDetail)
        {
            return new RouteActivitySaveOnExitPlan(
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                hasPreviousRoute,
                previousRouteIdentity,
                previousRouteOperationId,
                previousRouteSequence,
                previousSaveActivityOnExit,
                previousRouteContributorScopePolicy,
                previousActivityIdentity,
                previousActivitySaveKey,
                shouldSave: false,
                skipKind,
                skipDetail);
        }
    }
}
