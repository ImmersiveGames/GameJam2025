using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class Base11SandboxOperationalRouteTransitionAdapter : ISessionOperationalRouteTransitionExecutor
    {
        private readonly SceneCompositionExecutor _sceneCompositionExecutor = new();

        public async Task<SessionOperationalRouteCompletedFact> ApplyOperationalRouteAsync(SessionOperationalRouteCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("SessionOperationalRouteCommand is invalid.");
            }

            string routeIdentity = command.RouteIdentity;
            string source = Normalize(command.Source);
            string reason = Normalize(command.Reason);

            DebugUtility.Log(typeof(Base11SandboxOperationalRouteTransitionAdapter),
                $"[OBS][SessionOperationalPipeline][Route] adapter='Base11SandboxOperationalRouteTransitionAdapter' action='ApplyOperationalRoute' routeIdentity='{routeIdentity}' activeScene='{command.ActiveScene}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' completionHandoff='{command.CompletionHandoff}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    reason,
                    command.RouteOperationId,
                    command.Route.ScenesToLoad,
                    command.Route.ScenesToUnload,
                    command.Route.ActiveScene));

            if (!compositionResult.Success)
            {
                throw new InvalidOperationException($"Sandbox scene composition failed. correlationId='{compositionResult.CorrelationId}' reason='{compositionResult.Reason}'.");
            }

            return new SessionOperationalRouteCompletedFact(
                command,
                compositionResult.CorrelationId,
                compositionResult.Reason);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
