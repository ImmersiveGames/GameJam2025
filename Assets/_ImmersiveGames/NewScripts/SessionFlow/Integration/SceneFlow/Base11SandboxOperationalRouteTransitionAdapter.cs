using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
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
            string activeSceneName = ResolveSceneName(command.ActiveSceneKey, nameof(command.ActiveSceneKey));

            DebugUtility.Log(typeof(Base11SandboxOperationalRouteTransitionAdapter),
                $"[OBS][SessionOperationalPipeline][Route] adapter='Base11SandboxOperationalRouteTransitionAdapter' action='ApplyOperationalRoute' routeIdentity='{routeIdentity}' activeScene='{activeSceneName}' activeSceneKey='{command.ActiveSceneKey.name}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' completionHandoff='{command.CompletionHandoff}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SceneCompositionResult compositionResult = await _sceneCompositionExecutor.ApplyAsync(
                new SceneCompositionRequest(
                    SceneCompositionScope.Local,
                    reason,
                    command.RouteOperationId,
                    ResolveSceneNames(command.ScenesToLoad, nameof(command.ScenesToLoad)),
                    ResolveSceneNames(command.ScenesToUnload, nameof(command.ScenesToUnload)),
                    activeSceneName));

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

        private static string ResolveSceneName(SceneKeyAsset sceneKey, string fieldName)
        {
            if (sceneKey == null)
            {
                HardFailFastH1.Trigger(typeof(Base11SandboxOperationalRouteTransitionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} is required.");
            }

            if (string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                HardFailFastH1.Trigger(typeof(Base11SandboxOperationalRouteTransitionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} requires a SceneKeyAsset with a non-empty SceneName. asset='{sceneKey.name}'.");
            }

            return sceneKey.SceneName.Trim();
        }

        private static IReadOnlyList<string> ResolveSceneNames(IReadOnlyList<SceneKeyAsset> sceneKeys, string fieldName)
        {
            if (sceneKeys == null)
            {
                HardFailFastH1.Trigger(typeof(Base11SandboxOperationalRouteTransitionAdapter),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} is required.");
            }

            var resolved = new List<string>(sceneKeys.Count);
            for (int i = 0; i < sceneKeys.Count; i++)
            {
                string sceneName = ResolveSceneName(sceneKeys[i], $"{fieldName}[{i}]");
                resolved.Add(sceneName);
            }

            return resolved;
        }
    }
}
