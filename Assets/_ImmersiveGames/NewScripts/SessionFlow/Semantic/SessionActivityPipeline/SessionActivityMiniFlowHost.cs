using System;
using System.Text;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.InputModes;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SessionActivityPipeline;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SimulationGate;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/SessionFlow/Session Activity Sandbox Host")]
    public sealed class SessionActivityMiniFlowHost : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private bool autoStart;
        [SerializeField] private string sessionId = "SessionActivitySandboxSession";

        private SessionActivityMiniCatalog _catalog;
        private SessionActivityPipeline _pipeline;

        public SessionActivityRuntimeState State => _pipeline != null ? _pipeline.State : null;
        public SessionActivityMiniCatalog Catalog => _catalog;
        public SessionActivityPipeline Pipeline => _pipeline;
        public SimulationGateState GateState => _pipeline != null ? _pipeline.GateState : null;

        private void Awake()
        {
            _catalog = new SessionActivityMiniCatalog();
            _pipeline = new SessionActivityPipeline(
                _catalog,
                sessionId,
                new SessionActivityPauseOverlayAdapter(),
                new SessionActivityInputModeAdapter());
            RegisterGlobal(_catalog);
            RegisterGlobal(_pipeline);
            RegisterGlobal<ISessionActivityEntryHandoffReceiver>(_pipeline);
            Debug.Log(BuildHostBanner());
        }

        private void Start()
        {
            if (autoStart)
            {
                DebugDirectStart();
            }
        }

        [Obsolete("Use DebugDirectStart for explicit QA-only direct start.")]
        public void StartDemo()
        {
            DebugDirectStart();
        }

        public void DebugDirectStart()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.DebugDirectStart(QaSource("DebugDirectStart"), QaReason("DebugDirectStart"));
            LogResult("DebugDirectStart", result);
        }

        public void CompleteCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteCurrentActivity(QaSource("CompleteCurrentActivity"), QaReason("CompleteCurrentActivity"));
            LogResult("CompleteCurrentActivity", result);
        }

        public void ContinueToNextActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ContinueToNextActivity(QaSource("ContinueToNextActivity"), QaReason("ContinueToNextActivity"));
            LogResult("ContinueToNextActivity", result);
        }

        public void GoToNextActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToNextActivity(QaSource("GoToNextActivity"), QaReason("GoToNextActivity"));
            LogResult("GoToNextActivity", result);
        }

        public void GoToPreviousActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToPreviousActivity(QaSource("GoToPreviousActivity"), QaReason("GoToPreviousActivity"));
            LogResult("GoToPreviousActivity", result);
        }

        public void RestartCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.RestartCurrentActivity(QaSource("RestartCurrentActivity"), QaReason("RestartCurrentActivity"));
            LogResult("RestartCurrentActivity", result);
        }

        public void GoToActivity01()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToActivity01(QaSource("GoToActivity01"), QaReason("GoToActivity01"));
            LogResult("GoToActivity01", result);
        }

        public void GoToActivity02()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToActivity02(QaSource("GoToActivity02"), QaReason("GoToActivity02"));
            LogResult("GoToActivity02", result);
        }

        public void RequestPause()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.PauseRequested(QaSource("RequestPause"), QaReason("RequestPause"));
            LogResult("RequestPause", result);
        }

        public void RequestResume()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ResumeRequested(QaSource("RequestResume"), QaReason("RequestResume"));
            LogResult("RequestResume", result);
        }

        [Obsolete("Use RequestPause instead.")]
        public void PauseSimulation()
        {
            RequestPause();
        }

        [Obsolete("Use RequestResume instead.")]
        public void ResumeSimulation()
        {
            RequestResume();
        }

        public SessionActivityCommandResult ExecuteCommand(SessionActivityCommand command, string actionLabel)
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.Execute(command);
            LogResult(actionLabel, result);
            return result;
        }

        public string DumpState()
        {
            EnsurePipeline();
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] DumpState");
            builder.AppendLine($"sessionId='{sessionId}' autoStart='{autoStart}'");
            builder.AppendLine($"pipelineId='{State.PipelineId}' sessionStateId='{State.SessionId}'");
            builder.AppendLine($"entrySequence='{State.CurrentEntrySequence}'");
            builder.AppendLine($"simulationState='{State.CurrentSimulationState}'");
            builder.AppendLine($"gateState='{GateState}'");
            builder.AppendLine($"catalog='{_catalog.Summary}'");
            builder.AppendLine($"started='{State.HasStarted}' completed='{State.HasCompleted}' stage='{State.CurrentStage}'");
            builder.AppendLine($"definition='{State.CurrentDefinition}'");
            builder.AppendLine($"identity='{State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{State.CurrentHandoff}'");
            builder.AppendLine("facts:");
            for (int index = 0; index < State.Facts.Count; index++)
            {
                builder.AppendLine($"- {State.Facts[index]}");
            }

            builder.AppendLine("snapshots:");
            for (int index = 0; index < State.Snapshots.Count; index++)
            {
                builder.AppendLine($"- {State.Snapshots[index]}");
            }

            builder.AppendLine("trace:");
            for (int index = 0; index < State.Trace.Count; index++)
            {
                builder.AppendLine($"- {State.Trace[index]}");
            }

            string dump = builder.ToString().TrimEnd();
            Debug.Log(dump);
            return dump;
        }

        private void EnsurePipeline()
        {
            if (_pipeline != null)
            {
                return;
            }

            throw new InvalidOperationException("SessionActivityMiniFlowHost pipeline is not initialized.");
        }

        private void LogResult(string action, SessionActivityCommandResult result)
        {
            if (!result.IsValid)
            {
                throw new InvalidOperationException($"Invalid result returned by action '{action}'.");
            }

            string outcome = result.Kind.ToString();
            Debug.Log($"[OBS][SessionActivityPipeline][Host] action='{action}' outcome='{outcome}' reason='{result.Reason}' entrySequence='{State.CurrentEntrySequence}' simulationState='{State.CurrentSimulationState}' gateState='{GateState}'");

            for (int index = 0; index < result.Facts.Count; index++)
            {
                Debug.Log(result.Facts[index].ToString());
            }

            for (int index = 0; index < State.Snapshots.Count; index++)
            {
                Debug.Log(State.Snapshots[index].ToString());
            }

            Debug.Log(BuildTraceDump());
        }

        private string BuildTraceDump()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][Host] Trace");
            for (int index = 0; index < State.Trace.Count; index++)
            {
                builder.AppendLine(State.Trace[index]);
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildHostBanner()
        {
            return $"[OBS][SessionActivityPipeline][Host] initialized sessionId='{sessionId}' autoStart='{autoStart}' entrySequence='{State.CurrentEntrySequence}' simulationState='{State.CurrentSimulationState}' gateState='{GateState}' catalog='{_catalog.Summary}'";
        }

        private static string QaSource(string action)
        {
            return $"SessionActivityMiniFlowHost/QA/{action}";
        }

        private static string QaReason(string action)
        {
            return $"SessionActivityMiniFlowHost/QA/{action}";
        }

        private static void RegisterGlobal<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (DependencyManager.Provider.TryGetGlobal<T>(out var existing) && existing != null)
            {
                if (!ReferenceEquals(existing, instance))
                {
                    throw new InvalidOperationException($"Global dependency '{typeof(T).Name}' is already registered with a different instance.");
                }

                return;
            }

            DependencyManager.Provider.RegisterGlobal(instance);
        }
    }
}
