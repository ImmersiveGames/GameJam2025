using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public readonly struct OperationalSceneCompositionCommand
    {
        public OperationalSceneCompositionCommand(
            SessionOperationalRouteCommand routeCommand,
            string activeSceneName,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            ActiveSceneName = Normalize(activeSceneName);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string ActiveSceneName { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(ActiveSceneName);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalSceneCompositionStageResult
    {
        public OperationalSceneCompositionStageResult(
            OperationalSceneCompositionResultKind kind,
            SessionOperationalRouteCompletedFact completionFact,
            string reason,
            string detail)
        {
            Kind = kind;
            CompletionFact = completionFact;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalSceneCompositionResultKind Kind { get; }
        public SessionOperationalRouteCompletedFact CompletionFact { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalSceneCompositionResultKind.Completed && CompletionFact.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalSceneCompositionStage
    {
        private readonly Func<IOperationalSceneCompositionPort> _sceneCompositionPortResolver;

        public OperationalSceneCompositionStage(Func<IOperationalSceneCompositionPort> sceneCompositionPortResolver)
        {
            _sceneCompositionPortResolver = sceneCompositionPortResolver ?? throw new ArgumentNullException(nameof(sceneCompositionPortResolver));
        }

        public async Task<OperationalSceneCompositionStageResult> ExecuteAsync(OperationalSceneCompositionCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalSceneCompositionCommand is invalid.");
            }

            var routeCommand = command.RouteCommand;
            string source = Normalize(command.Source);
            string reason = Normalize(command.Reason);

            DebugUtility.LogVerbose(typeof(OperationalSceneCompositionStage),
                $"OperationalSceneCompositionStarted routeIdentity='{routeCommand.RouteIdentity}' activeScene='{command.ActiveSceneName}' activeSceneKey='{routeCommand.ActiveSceneKey.name}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' completionHandoff='{routeCommand.CompletionHandoff}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            var sceneCompositionPort = ResolveSceneCompositionPortOrFail(routeCommand);
            var result = await sceneCompositionPort.ApplyAsync(
                new OperationalSceneCompositionRequest(
                    routeCommand,
                    source,
                    reason));

            if (!result.IsCompleted)
            {
                DebugUtility.LogWarning<OperationalSceneCompositionStage>(
                    $"OperationalSceneCompositionFailed routeIdentity='{routeCommand.RouteIdentity}' activeScene='{command.ActiveSceneName}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' resultKind='{result.Kind}' reason='{result.Reason}' detail='{result.Detail}' source='{source}' reasonDetail='{reason}'.");

                return new OperationalSceneCompositionStageResult(
                    result.Kind,
                    default,
                    result.Reason,
                    result.Detail);
            }

            DebugUtility.Log(typeof(OperationalSceneCompositionStage),
                $"OperationalSceneCompositionCompleted routeIdentity='{routeCommand.RouteIdentity}' activeScene='{command.ActiveSceneName}' activeSceneKey='{routeCommand.ActiveSceneKey.name}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' correlationId='{result.CompletionFact.CorrelationId}' resultReason='{result.Reason}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalSceneCompositionStageResult(
                OperationalSceneCompositionResultKind.Completed,
                result.CompletionFact,
                result.Reason,
                result.Detail);
        }

        private IOperationalSceneCompositionPort ResolveSceneCompositionPortOrFail(SessionOperationalRouteCommand routeCommand)
        {
            var sceneCompositionPort = _sceneCompositionPortResolver();
            if (sceneCompositionPort == null)
            {
                throw new InvalidOperationException($"[FATAL][SessionOperationalPipeline][SceneComposition] IOperationalSceneCompositionPort is required routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}'.");
            }

            return sceneCompositionPort;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
