using System;
using System.Text;
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
        public SimulationGateState GateState => _pipeline != null ? _pipeline.GateState : null;

        private void Awake()
        {
            _catalog = new SessionActivityMiniCatalog();
            _pipeline = new SessionActivityPipeline(_catalog, sessionId, new SessionActivityPauseOverlayAdapter());
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
            SessionActivityCommandResult result = _pipeline.DebugDirectStart("SessionActivityMiniFlowHost", "Start Activity 01");
            LogResult("DebugDirectStart", result);
        }

        public void CompleteCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.CompleteCurrentActivity("SessionActivityMiniFlowHost", "Complete current activity");
            LogResult("CompleteCurrentActivity", result);
        }

        public void ContinueToNextActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ContinueToNextActivity("SessionActivityMiniFlowHost", "Continue to next activity");
            LogResult("ContinueToNextActivity", result);
        }

        public void GoToNextActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToNextActivity("SessionActivityMiniFlowHost", "Go to next activity");
            LogResult("GoToNextActivity", result);
        }

        public void GoToPreviousActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToPreviousActivity("SessionActivityMiniFlowHost", "Go to previous activity");
            LogResult("GoToPreviousActivity", result);
        }

        public void RestartCurrentActivity()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.RestartCurrentActivity("SessionActivityMiniFlowHost", "Restart current activity");
            LogResult("RestartCurrentActivity", result);
        }

        public void GoToActivity01()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToActivity01("SessionActivityMiniFlowHost", "Go to activity 01");
            LogResult("GoToActivity01", result);
        }

        public void GoToActivity02()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.GoToActivity02("SessionActivityMiniFlowHost", "Go to activity 02");
            LogResult("GoToActivity02", result);
        }

        public void RequestPause()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.PauseRequested("SessionActivityMiniFlowHost", "Request pause");
            LogResult("RequestPause", result);
        }

        public void RequestResume()
        {
            EnsurePipeline();
            SessionActivityCommandResult result = _pipeline.ResumeRequested("SessionActivityMiniFlowHost", "Request resume");
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
    }
}
