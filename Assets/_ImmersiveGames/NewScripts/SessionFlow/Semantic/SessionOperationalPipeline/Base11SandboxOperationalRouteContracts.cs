using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public enum Base11SandboxOperationalRouteCompletionHandoffKind
    {
        Unknown = 0,
        SessionActivityEntry = 1,
    }

    public readonly struct Base11SandboxOperationalRoute
    {
        public Base11SandboxOperationalRoute(
            SceneRouteDefinitionAsset routeDefinitionAsset,
            Base11SandboxOperationalRouteCompletionHandoffKind completionHandoff,
            string source,
            string reason)
        {
            RouteDefinitionAsset = routeDefinitionAsset;
            CompletionHandoff = completionHandoff;
            Source = Normalize(source);
            Reason = Normalize(reason);

            if (routeDefinitionAsset == null)
            {
                RouteDefinition = default;
                RouteIdentity = SceneRouteId.None;
                return;
            }

            RouteDefinition = routeDefinitionAsset.ToDefinition();
            RouteIdentity = routeDefinitionAsset.RouteId;
        }

        public SceneRouteDefinitionAsset RouteDefinitionAsset { get; }
        public SceneRouteDefinition RouteDefinition { get; }
        public SceneRouteId RouteIdentity { get; }
        public Base11SandboxOperationalRouteCompletionHandoffKind CompletionHandoff { get; }
        public IReadOnlyList<string> ScenesToLoad => RouteDefinition.ScenesToLoad;
        public IReadOnlyList<string> ScenesToUnload => RouteDefinition.ScenesToUnload;
        public string ActiveScene => RouteDefinition.TargetActiveScene;
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RouteDefinitionAsset != null &&
            RouteIdentity.IsValid &&
            RouteDefinition.HasRouteProfile &&
            RouteDefinition.RouteProfile.ProfileId.IsValid &&
            RouteDefinition.HasSceneData &&
            CompletionHandoff == Base11SandboxOperationalRouteCompletionHandoffKind.SessionActivityEntry &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', activeScene='{ActiveScene}', completionHandoff='{CompletionHandoff}', source='{Source}', reason='{Reason}'"
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct Base11SandboxOperationalRouteCommand
    {
        public Base11SandboxOperationalRouteCommand(
            Base11SandboxOperationalRoute route,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            Route = route;
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public Base11SandboxOperationalRoute Route { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }
        public string RouteIdentity => Route.RouteIdentity.Value;
        public string RouteProfileId => Route.RouteDefinition.HasRouteProfile && Route.RouteDefinition.RouteProfile.ProfileId.IsValid ? Route.RouteDefinition.RouteProfile.ProfileId.Value : string.Empty;
        public string ActiveScene => Route.ActiveScene;
        public string CompletionHandoff => Route.CompletionHandoff.ToString();

        public bool IsValid =>
            Route.IsValid &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeProfileId='{RouteProfileId}', activeScene='{ActiveScene}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', completionHandoff='{CompletionHandoff}', source='{Source}', reason='{Reason}'"
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct Base11SandboxOperationalRouteCompletedFact
    {
        public Base11SandboxOperationalRouteCompletedFact(
            Base11SandboxOperationalRouteCommand command,
            string correlationId,
            string message)
        {
            Command = command;
            CorrelationId = Normalize(correlationId);
            Message = Normalize(message);
        }

        public Base11SandboxOperationalRouteCommand Command { get; }
        public string CorrelationId { get; }
        public string Message { get; }
        public string RouteIdentity => Command.RouteIdentity;
        public string RouteProfileId => Command.RouteProfileId;
        public string RouteOperationId => Command.RouteOperationId;
        public string TransitionId => Command.TransitionId;
        public int RouteSequence => Command.RouteSequence;
        public bool IsValid => Command.IsValid && !string.IsNullOrWhiteSpace(CorrelationId);

        public override string ToString()
        {
            return IsValid
                ? $"routeIdentity='{RouteIdentity}', routeProfileId='{RouteProfileId}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', correlationId='{CorrelationId}', message='{Message}'"
                : "<none>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IBase11SandboxOperationalRouteTransitionExecutor
    {
        Task<Base11SandboxOperationalRouteCompletedFact> ApplyOperationalRouteAsync(Base11SandboxOperationalRouteCommand command);
    }
}
