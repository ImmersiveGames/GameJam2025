using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteMaterializationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteMaterializationResult
    {
        public OperationalRouteMaterializationResult(
            OperationalRouteMaterializationResultKind kind,
            SessionOperationalRouteCompletedFact completionFact,
            bool loadingCompleted,
            bool loadingHidden)
        {
            Kind = kind;
            CompletionFact = completionFact;
            LoadingCompleted = loadingCompleted;
            LoadingHidden = loadingHidden;
        }

        public OperationalRouteMaterializationResultKind Kind { get; }
        public SessionOperationalRouteCompletedFact CompletionFact { get; }
        public bool LoadingCompleted { get; }
        public bool LoadingHidden { get; }
        public bool IsCompleted => Kind == OperationalRouteMaterializationResultKind.Completed && CompletionFact.IsValid;
    }

    public sealed class OperationalRouteMaterializationCommand
    {
        public string RouteIdentity { get; set; }
        public string RouteOperationId { get; set; }
        public string TransitionId { get; set; }
        public int RouteSequence { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public OperationalSceneCompositionStage SceneCompositionStage { get; set; }
        public OperationalSceneCompositionCommand SceneCompositionCommand { get; set; }
        public OperationalRouteCameraPresentationStage RouteCameraPresentationStage { get; set; }
        public OperationalRouteCameraPresentationCommand RouteCameraPresentationCommand { get; set; }
        public OperationalLoadingStage LoadingStage { get; set; }
        public OperationalLoadingCommand LoadingCommand { get; set; }
        public OperationalRouteActivitySaveLoadOnEnterStage RouteActivitySaveLoadOnEnterStage { get; set; }
        public OperationalRouteActivitySaveLoadOnEnterCommand RouteActivitySaveLoadOnEnterCommand { get; set; }
        public OperationalInputPreparationStage InputPreparationStage { get; set; }
        public OperationalInputPreparationCommand InputPreparationCommand { get; set; }
        public OperationalPlayerPreparationStage PlayerPreparationStage { get; set; }
        public OperationalPlayerPreparationCommand PlayerPreparationCommand { get; set; }
        public OperationalConsumerPresentationPreparationStage ConsumerPresentationPreparationStage { get; set; }
        public OperationalConsumerPresentationPreparationCommand ConsumerPresentationPreparationCommand { get; set; }
        public OperationalConsumerEntryAndReadinessStage ConsumerEntryAndReadinessStage { get; set; }
        public OperationalConsumerEntryAndReadinessCommand ConsumerEntryAndReadinessCommand { get; set; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            SceneCompositionStage != null &&
            SceneCompositionCommand.IsValid &&
            RouteCameraPresentationStage != null &&
            RouteCameraPresentationCommand.IsValid &&
            LoadingStage != null &&
            LoadingCommand.IsValid &&
            RouteActivitySaveLoadOnEnterStage != null &&
            RouteActivitySaveLoadOnEnterCommand.IsValid &&
            InputPreparationStage != null &&
            InputPreparationCommand.IsValid &&
            PlayerPreparationStage != null &&
            PlayerPreparationCommand.IsValid &&
            ConsumerPresentationPreparationStage != null &&
            ConsumerPresentationPreparationCommand.IsValid &&
            ConsumerEntryAndReadinessStage != null &&
            ConsumerEntryAndReadinessCommand.IsValid;
    }

    public readonly struct OperationalRouteMaterializationLoadingState
    {
        public OperationalRouteMaterializationLoadingState(bool loadingCompleted, bool loadingHidden)
        {
            LoadingCompleted = loadingCompleted;
            LoadingHidden = loadingHidden;
        }

        public bool LoadingCompleted { get; }
        public bool LoadingHidden { get; }
    }

    public sealed class OperationalRouteMaterializationStage
    {
        public async Task<OperationalRouteMaterializationResult> ExecuteAsync(OperationalRouteMaterializationCommand command)
        {
            if (command == null || !command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteMaterializationCommand is invalid.");
            }

            LogMaterializationStarted(command);

            SessionOperationalRouteCompletedFact adapterFact = await ExecuteRouteCompositionAsync(command);
            ExecuteRouteRuntimePreparation(command);
            PlayerPreparationResult playerPreparationResult = ExecuteConsumerPresentationPreparation(command);
            OperationalRouteMaterializationLoadingState loadingState = await ExecuteLoadingAndConsumerReadinessAsync(command, playerPreparationResult);

            LogMaterializationCompleted(command);

            return new OperationalRouteMaterializationResult(
                OperationalRouteMaterializationResultKind.Completed,
                adapterFact,
                loadingState.LoadingCompleted,
                loadingState.LoadingHidden);
        }

        private static async Task<SessionOperationalRouteCompletedFact> ExecuteRouteCompositionAsync(OperationalRouteMaterializationCommand command)
        {
            OperationalSceneCompositionStageResult sceneCompositionResult = await command.SceneCompositionStage.ExecuteAsync(command.SceneCompositionCommand);
            if (!sceneCompositionResult.IsCompleted)
            {
                throw new InvalidOperationException($"Operational scene composition did not complete. reason='{sceneCompositionResult.Reason}' detail='{sceneCompositionResult.Detail}'.");
            }

            SessionOperationalRouteCompletedFact adapterFact = sceneCompositionResult.CompletionFact;

            command.RouteCameraPresentationStage.Execute(command.RouteCameraPresentationCommand);
            OperationalLoadingResult sceneCompositionLoadingResult = await command.LoadingStage.ExecuteSceneCompositionCompletedAsync(command.LoadingCommand);
            if (!sceneCompositionLoadingResult.IsAccepted)
            {
                throw new InvalidOperationException($"Operational loading scene composition progress did not complete. reason='{sceneCompositionLoadingResult.Reason}' detail='{sceneCompositionLoadingResult.Detail}'.");
            }
            OperationalRouteActivitySaveLoadOnEnterResult loadOnEnterResult =
                command.RouteActivitySaveLoadOnEnterStage.Execute(command.RouteActivitySaveLoadOnEnterCommand);
            if (!loadOnEnterResult.IsCompleted)
            {
                throw new InvalidOperationException($"Operational RouteActivitySave load-on-enter did not complete. reason='{loadOnEnterResult.Reason}' detail='{loadOnEnterResult.Detail}'.");
            }

            return adapterFact;
        }

        private static void ExecuteRouteRuntimePreparation(OperationalRouteMaterializationCommand command)
        {
            OperationalInputPreparationResult inputPreparationResult = command.InputPreparationStage.Execute(command.InputPreparationCommand);
            if (!inputPreparationResult.IsCompleted)
            {
                throw new InvalidOperationException("Operational input preparation did not complete.");
            }
        }

        private static PlayerPreparationResult ExecuteConsumerPresentationPreparation(OperationalRouteMaterializationCommand command)
        {
            OperationalPlayerPreparationResult playerPreparationStageResult = command.PlayerPreparationStage.Execute(command.PlayerPreparationCommand);
            if (!playerPreparationStageResult.IsAccepted)
            {
                throw new InvalidOperationException("Operational player preparation did not complete.");
            }

            PlayerPreparationResult playerPreparationResult = playerPreparationStageResult.PlayerPreparationResult;
            if (playerPreparationStageResult.IsCompleted)
            {
                OperationalConsumerPresentationPreparationResult consumerPresentationResult =
                    command.ConsumerPresentationPreparationStage.Execute(command.ConsumerPresentationPreparationCommand);
                if (!consumerPresentationResult.IsAccepted)
                {
                    throw new InvalidOperationException("Operational consumer presentation preparation did not complete.");
                }
            }

            return playerPreparationResult;
        }

        private static async Task<OperationalRouteMaterializationLoadingState> ExecuteLoadingAndConsumerReadinessAsync(
            OperationalRouteMaterializationCommand command,
            PlayerPreparationResult playerPreparationResult)
        {
            OperationalRouteMaterializationLoadingState loadingState = await command.LoadingStage.ExecuteClosedWindowCompletionAsync(command.LoadingCommand);
            OperationalConsumerEntryAndReadinessResult consumerEntryAndReadinessResult =
                await command.ConsumerEntryAndReadinessStage.ExecuteAsync(
                    command.ConsumerEntryAndReadinessCommand,
                    playerPreparationResult);
            if (!consumerEntryAndReadinessResult.IsAccepted)
            {
                throw new InvalidOperationException("Operational consumer entry/readiness did not complete.");
            }

            return loadingState;
        }

        private static void LogMaterializationStarted(OperationalRouteMaterializationCommand command)
        {
            DebugUtility.Log(typeof(OperationalRouteMaterializationStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteMaterializationStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{Normalize(command.Source)}' reason='{Normalize(command.Reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogMaterializationCompleted(OperationalRouteMaterializationCommand command)
        {
            DebugUtility.Log(typeof(OperationalRouteMaterializationStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteMaterializationCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{Normalize(command.Source)}' reason='{Normalize(command.Reason)}'.",
                DebugUtility.Colors.Success);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
