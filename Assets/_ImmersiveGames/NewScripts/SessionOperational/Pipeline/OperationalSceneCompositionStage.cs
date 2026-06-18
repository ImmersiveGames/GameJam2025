using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    // Etapa 2 canonical model: local command kept (Pipeline step-specific data). The Result surface is now the canonical from Contracts.
    public readonly struct OperationalSceneCompositionCommand
    {
        public OperationalSceneCompositionCommand(
            SessionOperationalRouteCommand routeCommand,
            string activeSceneName,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            ActiveSceneName = activeSceneName.TrimToEmpty();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string ActiveSceneName { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(ActiveSceneName);
}

    // Etapa 2 (canonização Command/Result/Fact): removed local OperationalSceneCompositionStageResult wrapper.
    // Stage now returns the canonical OperationalSceneCompositionResult directly from Contracts.
    // This unifies the operation result model (IsCompleted/IsAccepted + factories on the contract type).
    // Command remains local (enriches data for this step) as it is Pipeline-internal; the port Request is built from it.

    public sealed class OperationalSceneCompositionStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly Func<IOperationalSceneCompositionPort> _sceneCompositionPortResolver;

        public OperationalSceneCompositionStage(OperationalFactRecorder factRecorder, Func<IOperationalSceneCompositionPort> sceneCompositionPortResolver)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _sceneCompositionPortResolver = sceneCompositionPortResolver ?? throw new ArgumentNullException(nameof(sceneCompositionPortResolver));
        }

        public async Task<OperationalSceneCompositionResult> ExecuteAsync(OperationalSceneCompositionCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalSceneCompositionCommand is invalid.");
            }

            var routeCommand = command.RouteCommand;
            string source = command.Source.TrimToEmpty();
            string reason = command.Reason.TrimToEmpty();

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.SceneComposition, source, reason, "scene_composition_started");

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
                _factRecorder.TryRecordOperationStage(SessionOperationalStage.SceneComposition, source, reason, "scene_composition_failed");
                DebugUtility.LogWarning<OperationalSceneCompositionStage>(
                    $"OperationalSceneCompositionFailed routeIdentity='{routeCommand.RouteIdentity}' activeScene='{command.ActiveSceneName}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' resultKind='{result.Kind}' reason='{result.Reason}' detail='{result.Detail}' source='{source}' reasonDetail='{reason}'.");

                // Etapa 2: forward the canonical result directly (no local wrapper)
                return result;
            }

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.SceneComposition, source, reason, "scene_composition_completed");
            DebugUtility.Log(typeof(OperationalSceneCompositionStage),
                $"OperationalSceneCompositionCompleted routeIdentity='{routeCommand.RouteIdentity}' activeScene='{command.ActiveSceneName}' activeSceneKey='{routeCommand.ActiveSceneKey.name}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' correlationId='{result.CompletionFact.CorrelationId}' resultReason='{result.Reason}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Success);

            // Etapa 2: forward the canonical result directly (no local wrapper)
            return result;
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
}
}
