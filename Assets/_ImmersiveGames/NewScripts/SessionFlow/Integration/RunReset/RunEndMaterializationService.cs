using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Result;
using UnityEngine;
using UnityEngine.SceneManagement;
using CanonicalRunEndIntent = _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts.RunEndIntent;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset
{
    public interface IRunEndPostMaterializationDispatchService
    {
        void DispatchRunResultStage();
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunEndMaterializationService : IRunEndMaterializationService
    {
        private readonly ISessionIntegrationContextService _sessionIntegrationService;
        private readonly IPostRunResultService _resultService;
        private readonly IRunEndIntentOwnershipService _runEndIntentOwner;
        private readonly IRunContinuationOwnershipService _continuationOwner;
        private readonly IRunEndPostMaterializationDispatchService _postMaterializationDispatchService;
        private readonly IGameplaySceneClassifier _gameplaySceneClassifier;

        public RunEndMaterializationService(
            ISessionIntegrationContextService sessionIntegrationService,
            IPostRunResultService resultService,
            IRunEndIntentOwnershipService runEndIntentOwner,
            IRunContinuationOwnershipService continuationOwner,
            IRunEndPostMaterializationDispatchService postMaterializationDispatchService,
            IGameplaySceneClassifier gameplaySceneClassifier)
        {
            _sessionIntegrationService = sessionIntegrationService ?? throw new ArgumentNullException(nameof(sessionIntegrationService));
            _resultService = resultService ?? throw new ArgumentNullException(nameof(resultService));
            _runEndIntentOwner = runEndIntentOwner ?? throw new ArgumentNullException(nameof(runEndIntentOwner));
            _continuationOwner = continuationOwner ?? throw new ArgumentNullException(nameof(continuationOwner));
            _postMaterializationDispatchService = postMaterializationDispatchService ?? throw new ArgumentNullException(nameof(postMaterializationDispatchService));
            _gameplaySceneClassifier = gameplaySceneClassifier;
        }

        public void MaterializeAndDispatch(GameRunEndedEvent evt, string source)
        {
            if (!TryMapToRunResult(evt?.Outcome ?? GameRunOutcome.Unknown, out RunResult runResult))
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    $"[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido com outcome nao terminal='{evt?.Outcome}' reason='{GameLoopReasonFormatter.Format(evt?.Reason)}'.");
                return;
            }

            string reason = GameLoopReasonFormatter.Format(evt?.Reason);
            string sceneName = SceneManager.GetActiveScene().name;
            bool isGameplayScene = IsGameplayScene();

            if (!_sessionIntegrationService.TryGetCurrent(out var sessionIntegration) || !sessionIntegration.HasCoreContext)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] GameRunEndedEvent recebido mas o contexto de Session Integration e invalido.");
                return;
            }

            GameplayPhaseRuntimeSnapshot phaseRuntime = sessionIntegration.PhaseRuntime;

            var intent = new CanonicalRunEndIntent(
                signature: phaseRuntime.PhaseRuntimeSignature,
                sceneName: sceneName,
                profile: string.Empty,
                frame: Time.frameCount,
                reason: reason,
                isGameplayScene: isGameplayScene);

            _resultService.TrySetRunOutcome(evt.Outcome, reason);
            _runEndIntentOwner.AcceptRunEndIntent(intent);
            _continuationOwner.AcceptTerminalFact(new RunContinuationTerminalFact(intent, runResult));

            string phaseEntrySignature = sessionIntegration.SessionContext.HasSessionSignature
                ? sessionIntegration.SessionContext.SessionSignature
                : phaseRuntime.PhaseRuntimeSignature;

            PhaseCompleted phaseCompleted = new PhaseCompleted(
                phaseRuntime,
                intent,
                evt.Outcome,
                source,
                phaseRuntime.HasPhaseDefinitionRef ? sessionIntegration.SessionContext.SelectionVersion : 0,
                phaseEntrySignature);

            if (!phaseCompleted.IsValid)
            {
                DebugUtility.LogError<GameRunEndedEventBridge>(
                    "[FATAL][GameplaySessionFlow] PhaseCompleted invalido montado a partir de GameRunEndedEvent.");
                return;
            }

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][PhaseCompleted] PhaseCompletedCanonical source='{phaseCompleted.Source}' phaseSignature='{phaseCompleted.PhaseSignature}' outcome='{phaseCompleted.RunOutcome}' entrySequence='{phaseCompleted.PhaseLocalEntrySequence}' entrySignature='{phaseCompleted.EntrySignature}' reason='{phaseCompleted.RunEndIntent.Reason}'.",
                DebugUtility.Colors.Info);

            EventBus<PhaseCompleted>.Raise(phaseCompleted);

            DebugUtility.Log<GameRunEndedEventBridge>(
                $"[OBS][GameplaySessionFlow][RunResultStage] RunResultStageDispatchRequested outcome='{evt?.Outcome}' result='{runResult}' reason='{reason}' scene='{sceneName}' frame={Time.frameCount} isGameplayScene='{isGameplayScene}' phaseSignature='{phaseRuntime.PhaseRuntimeSignature}'.",
                DebugUtility.Colors.Info);

            _postMaterializationDispatchService.DispatchRunResultStage();
        }

        private bool IsGameplayScene()
        {
            return _gameplaySceneClassifier != null && _gameplaySceneClassifier.IsGameplayScene();
        }

        private static bool TryMapToRunResult(GameRunOutcome outcome, out RunResult result)
        {
            result = outcome switch
            {
                GameRunOutcome.Victory => RunResult.Victory,
                GameRunOutcome.Defeat => RunResult.Defeat,
                _ => RunResult.Unknown,
            };

            return result != RunResult.Unknown;
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunEndPostMaterializationDispatchService : IRunEndPostMaterializationDispatchService
    {
        private readonly IRunResultStageOwnershipService _runResultStageOwner;
        private readonly IRunContinuationOwnershipService _continuationOwner;

        public RunEndPostMaterializationDispatchService(
            IRunResultStageOwnershipService runResultStageOwner,
            IRunContinuationOwnershipService continuationOwner)
        {
            _runResultStageOwner = runResultStageOwner ?? throw new ArgumentNullException(nameof(runResultStageOwner));
            _continuationOwner = continuationOwner ?? throw new ArgumentNullException(nameof(continuationOwner));
        }

        public void DispatchRunResultStage()
        {
            _runResultStageOwner.EnterRunResultStage(_continuationOwner.CurrentContext);
        }
    }
}
