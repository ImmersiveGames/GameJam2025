using System;
using System.Text;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/SessionFlow/Session Activity Sandbox Debug Panel")]
    public sealed class SessionActivityMiniDebugPanel : MonoBehaviour
    {
        private const int PanelWidth = 1040;
        private const int PanelHeight = 980;
        private const int ButtonHeight = 52;
        private const int SectionSpacing = 8;
        private const int TextAreaHeight = 170;

        [Header("Refs")]
        [SerializeField] private SessionActivityMiniFlowHost host;

        [Header("Layout")]
        [SerializeField] private bool showOnGUI = true;

        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _dumpStyle;

        [ContextMenu("StartDemo")]
        public void StartDemo()
        {
            EnsureHost();
            host.StartDemo();
            DumpState();
        }

        [ContextMenu("CompleteCurrentActivity")]
        public void CompleteCurrentActivity()
        {
            EnsureHost();
            host.CompleteCurrentActivity();
            DumpState();
        }

        [ContextMenu("ContinueToNextActivity")]
        public void ContinueToNextActivity()
        {
            EnsureHost();
            host.ContinueToNextActivity();
            DumpState();
        }

        [ContextMenu("GoToNextActivity")]
        public void GoToNextActivity()
        {
            EnsureHost();
            host.GoToNextActivity();
            DumpState();
        }

        [ContextMenu("GoToPreviousActivity")]
        public void GoToPreviousActivity()
        {
            EnsureHost();
            host.GoToPreviousActivity();
            DumpState();
        }

        [ContextMenu("RestartCurrentActivity")]
        public void RestartCurrentActivity()
        {
            EnsureHost();
            host.RestartCurrentActivity();
            DumpState();
        }

        [ContextMenu("GoToActivity01")]
        public void GoToActivity01()
        {
            EnsureHost();
            host.GoToActivity01();
            DumpState();
        }

        [ContextMenu("GoToActivity02")]
        public void GoToActivity02()
        {
            EnsureHost();
            host.GoToActivity02();
            DumpState();
        }

        [ContextMenu("PauseSimulation")]
        public void PauseSimulation()
        {
            EnsureHost();
            host.PauseSimulation();
            DumpState();
        }

        [ContextMenu("ResumeSimulation")]
        public void ResumeSimulation()
        {
            EnsureHost();
            host.ResumeSimulation();
            DumpState();
        }

        [ContextMenu("SendStaleActivity01Command")]
        public void SendStaleActivity01Command()
        {
            EnsureHost();
            host.ExecuteCommand(BuildStaleActivity01Command(), "SendStaleActivity01Command");
            DumpState();
        }

        [ContextMenu("SendForeignSessionCommand")]
        public void SendForeignSessionCommand()
        {
            EnsureHost();
            host.ExecuteCommand(BuildForeignSessionCommand(), "SendForeignSessionCommand");
            DumpState();
        }

        [ContextMenu("SendForeignPipelineCommand")]
        public void SendForeignPipelineCommand()
        {
            EnsureHost();
            host.ExecuteCommand(BuildForeignPipelineCommand(), "SendForeignPipelineCommand");
            DumpState();
        }

        [ContextMenu("DumpState")]
        public void DumpState()
        {
            EnsureHost();
            Debug.Log(BuildDumpText());
        }

        private void OnGUI()
        {
            if (!showOnGUI)
            {
                return;
            }

            EnsureHost();
            EnsureStyles();

            GUILayout.BeginArea(new Rect(20, 20, PanelWidth, PanelHeight), _windowStyle);
            GUILayout.Label("Session Activity Sandbox", _titleStyle);
            GUILayout.Space(SectionSpacing);
            GUILayout.Label("Use the controls below to drive the minimum Session Activity cycle.", _labelStyle);
            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("StartDemo", _buttonStyle))
            {
                StartDemo();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("CompleteCurrentActivity", _buttonStyle))
            {
                CompleteCurrentActivity();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("ContinueToNextActivity", _buttonStyle))
            {
                ContinueToNextActivity();
            }

            GUILayout.Space(SectionSpacing);

            GUILayout.Label("Navigation", _labelStyle);

            if (GUILayout.Button("GoToNextActivity", _buttonStyle))
            {
                GoToNextActivity();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("GoToPreviousActivity", _buttonStyle))
            {
                GoToPreviousActivity();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("RestartCurrentActivity", _buttonStyle))
            {
                RestartCurrentActivity();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("GoToActivity01", _buttonStyle))
            {
                GoToActivity01();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("GoToActivity02", _buttonStyle))
            {
                GoToActivity02();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("PauseSimulation", _buttonStyle))
            {
                PauseSimulation();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("ResumeSimulation", _buttonStyle))
            {
                ResumeSimulation();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("DumpState", _buttonStyle))
            {
                DumpState();
            }

            GUILayout.Space(SectionSpacing);
            GUILayout.Label("Foreign/Stale QA", _labelStyle);

            if (GUILayout.Button("SendStaleActivity01Command", _buttonStyle))
            {
                SendStaleActivity01Command();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("SendForeignSessionCommand", _buttonStyle))
            {
                SendForeignSessionCommand();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("SendForeignPipelineCommand", _buttonStyle))
            {
                SendForeignPipelineCommand();
            }

            GUILayout.Space(SectionSpacing);
            GUILayout.Label("Current State", _labelStyle);

            string stateSummary = BuildStateSummary();
            GUILayout.TextArea(stateSummary, _dumpStyle, GUILayout.Height(TextAreaHeight));

            GUILayout.EndArea();
        }

        private void EnsureHost()
        {
            if (host != null)
            {
                return;
            }

            host = FindObjectOfType<SessionActivityMiniFlowHost>();
            if (host == null)
            {
                throw new InvalidOperationException("SessionActivityMiniDebugPanel requires SessionActivityMiniFlowHost in the same scene.");
            }
        }

        private string BuildDumpText()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][QA] DumpState");
            builder.AppendLine($"host='{host.name}'");
            builder.AppendLine($"pipelineId='{host.State.PipelineId}' sessionStateId='{host.State.SessionId}'");
            builder.AppendLine($"entrySequence='{host.State.CurrentEntrySequence}'");
            builder.AppendLine($"simulationState='{host.State.CurrentSimulationState}'");
            builder.AppendLine($"started='{host.State.HasStarted}' completed='{host.State.HasCompleted}' stage='{host.State.CurrentStage}' currentActivity='{host.State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"definition='{host.State.CurrentDefinition}'");
            builder.AppendLine($"identity='{host.State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{host.State.CurrentHandoff}'");
            builder.AppendLine("facts:");

            for (int index = 0; index < host.State.Facts.Count; index++)
            {
                builder.AppendLine($"- {host.State.Facts[index]}");
            }

            builder.AppendLine("snapshots:");
            for (int index = 0; index < host.State.Snapshots.Count; index++)
            {
                builder.AppendLine($"- {host.State.Snapshots[index]}");
            }

            builder.AppendLine("trace:");
            for (int index = 0; index < host.State.Trace.Count; index++)
            {
                builder.AppendLine($"- {host.State.Trace[index]}");
            }

            return builder.ToString().TrimEnd();
        }

        private string BuildStateSummary()
        {
            StringBuilder builder = new();
            builder.AppendLine($"pipelineId='{host.State.PipelineId}'");
            builder.AppendLine($"sessionStateId='{host.State.SessionId}'");
            builder.AppendLine($"entrySequence='{host.State.CurrentEntrySequence}'");
            builder.AppendLine($"simulationState='{host.State.CurrentSimulationState}'");
            builder.AppendLine($"started='{host.State.HasStarted}' completed='{host.State.HasCompleted}' stage='{host.State.CurrentStage}'");
            builder.AppendLine($"currentActivity='{host.State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"identity='{host.State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{host.State.CurrentHandoff}'");
            return builder.ToString().TrimEnd();
        }

        private SessionActivityCommand BuildStaleActivity01Command()
        {
            EnsureActiveIdentityOrFail("SendStaleActivity01Command");
            SessionActivityIdentity identity = new(
                host.State.PipelineId,
                host.State.SessionId,
                "activity_01",
                1,
                host.State.CurrentEntrySequence > 1 ? host.State.CurrentEntrySequence - 1 : 1,
                SessionActivityStage.GameplayRunning,
                "SessionActivityMiniDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityMiniDebugPanel",
                "Send stale activity_01 command.");
        }

        private SessionActivityCommand BuildForeignSessionCommand()
        {
            EnsureActiveIdentityOrFail("SendForeignSessionCommand");
            SessionActivityIdentity current = host.State.CurrentIdentity;
            SessionActivityIdentity identity = new(
                host.State.PipelineId,
                "ForeignSessionState",
                current.ActivityId,
                current.ActivityOrdinal,
                current.EntrySequence,
                current.Stage,
                "SessionActivityMiniDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityMiniDebugPanel",
                "Send foreign session command.");
        }

        private SessionActivityCommand BuildForeignPipelineCommand()
        {
            EnsureActiveIdentityOrFail("SendForeignPipelineCommand");
            SessionActivityIdentity current = host.State.CurrentIdentity;
            SessionActivityIdentity identity = new(
                "ForeignPipeline",
                host.State.SessionId,
                current.ActivityId,
                current.ActivityOrdinal,
                current.EntrySequence,
                current.Stage,
                "SessionActivityMiniDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityMiniDebugPanel",
                "Send foreign pipeline command.");
        }

        private void EnsureActiveIdentityOrFail(string action)
        {
            if (host.State == null || !host.State.CurrentIdentity.IsValid)
            {
                throw new InvalidOperationException($"Action '{action}' requires an active pipeline identity.");
            }
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
            {
                return;
            }

            GUIStyle baseWindow = new(GUI.skin.window)
            {
                padding = new RectOffset(18, 18, 18, 18),
                fontSize = 16,
                richText = false,
            };

            GUIStyle baseLabel = new(GUI.skin.label)
            {
                fontSize = 20,
                wordWrap = true,
                richText = false,
            };

            GUIStyle baseButton = new(GUI.skin.button)
            {
                fontSize = 22,
                fixedHeight = ButtonHeight,
                alignment = TextAnchor.MiddleCenter,
            };

            GUIStyle baseTextArea = new(GUI.skin.textArea)
            {
                fontSize = 18,
                wordWrap = true,
                richText = false,
                padding = new RectOffset(10, 10, 10, 10),
                alignment = TextAnchor.UpperLeft,
            };

            _windowStyle = baseWindow;
            _titleStyle = new GUIStyle(baseLabel)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _labelStyle = baseLabel;
            _buttonStyle = baseButton;
            _dumpStyle = baseTextArea;
        }
    }
}
