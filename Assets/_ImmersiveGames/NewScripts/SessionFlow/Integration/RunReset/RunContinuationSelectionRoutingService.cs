using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunContinuationSelectionRoutingService : IRunContinuationSelectionRoutingService
    {
        private const string RoutingOperation = "RunContinuationSelectionRouting";
        private const string RoutingSource = nameof(RunContinuationSelectionResolvedEvent);
        private const string RoutingTarget = "RunContinuationOperational";

        private readonly IRunContinuationOperationalHandoffService _handoffService;

        public RunContinuationSelectionRoutingService(IRunContinuationOperationalHandoffService handoffService)
        {
            _handoffService = handoffService ?? throw new ArgumentNullException(nameof(handoffService));
        }

        public void RouteSelection(RunContinuationSelection selection)
        {
            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] translated continuation='{selection.SelectedContinuation}' reason='{selection.Reason}' nextState='{selection.NextState}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_dispatch target='RunContinuationOperational' continuation='{selection.SelectedContinuation}' reason='{selection.Reason}' nextState='{selection.NextState}'.",
                DebugUtility.Colors.Info);

            _ = ObserveRouteSelectionAsync(selection, RoutingSource);
        }

        private async Task ObserveRouteSelectionAsync(
            RunContinuationSelection selection,
            string source)
        {
            try
            {
                DebugUtility.Log<RunContinuationSelectionRoutingService>(
                    BuildRunContinuationRoutingLogMessage(selection, "started", source, null),
                    DebugUtility.Colors.Info);

                await DispatchRunContinuationHandoffAsync(_handoffService, selection);

                DebugUtility.Log<RunContinuationSelectionRoutingService>(
                    BuildRunContinuationRoutingLogMessage(selection, "completed", source, null),
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<RunContinuationSelectionRoutingService>(
                    BuildRunContinuationRoutingLogMessage(selection, "failed", source, ex));

                HardFailFastH1.Trigger(typeof(RunContinuationSelectionRoutingService),
                    $"[FATAL][H1][GameplaySessionFlow] RunContinuation selection routing failed. operation='{RoutingOperation}' status='failed' continuation='{selection.SelectedContinuation}' semantic='{ResolveRoutingSemantic(selection.SelectedContinuation)}' legacy='false' source='{AsText(source)}' target='{RoutingTarget}' reason='{AsText(selection.Reason)}' nextState='{AsText(selection.NextState)}' exceptionType='{ex.GetType().Name}' exceptionMessage='{AsText(ex.Message)}'.",
                    ex);
            }
        }

        private static async Task DispatchRunContinuationHandoffAsync(
            IRunContinuationOperationalHandoffService handoffService,
            RunContinuationSelection selection)
        {
            await handoffService.DispatchAsync(selection);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][Seam] handoff_accepted target='RunContinuationOperational' continuation='{selection.SelectedContinuation}' reason='{selection.Reason}' nextState='{selection.NextState}'.",
                DebugUtility.Colors.Success);
        }

        private static string BuildRunContinuationRoutingLogMessage(
            RunContinuationSelection selection,
            string status,
            string source,
            Exception exception)
        {
            string exceptionFields = exception == null
                ? string.Empty
                : $" exceptionType='{exception.GetType().Name}' exceptionMessage='{AsText(exception.Message)}'";

            return $"[OBS][GameplaySessionFlow][Seam] run_continuation_routing_{status} operation='{RoutingOperation}' status='{status}' continuation='{selection.SelectedContinuation}' semantic='{ResolveRoutingSemantic(selection.SelectedContinuation)}' legacy='false' source='{AsText(source)}' target='{RoutingTarget}' reason='{AsText(selection.Reason)}' nextState='{AsText(selection.NextState)}'{exceptionFields}.";
        }

        private static string ResolveRoutingSemantic(RunContinuationKind continuation)
        {
            return continuation switch
            {
                RunContinuationKind.RestartCurrentPhase => "CurrentPhaseRestart",
                RunContinuationKind.RestartFromFirstPhase => "FirstPhaseRunRestart",
                _ => "<none>",
            };
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
