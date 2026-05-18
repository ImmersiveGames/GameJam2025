using System;
using System.Text;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/NewScripts/SessionActivity/Session Activity Debug Panel")]
    public sealed class SessionActivityDebugPanel : MonoBehaviour
    {
        private const int PanelWidth = 1040;
        private const int PanelHeight = 980;
        private const int ButtonHeight = 52;
        private const int SectionSpacing = 8;
        private const int TextAreaHeight = 170;

        [Header("Refs")]
        [SerializeField] private SessionActivityHost host;

        [Header("Layout")]
        [SerializeField] private bool showOnGUI = true;
        [SerializeField] private bool showForeignStaleQa = false;

        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _dumpStyle;

        [ContextMenu("CompleteCurrentActivity")]
        public void CompleteCurrentActivity()
        {
            EnsureHost();
            host.CompleteCurrentActivity();
            DumpState();
        }

        [ContextMenu("CompleteActivationWindow")]
        public void CompleteActivationWindow()
        {
            EnsureHost();
            host.CompleteActivationWindow();
            DumpState();
        }

        [ContextMenu("CompleteDeactivationWindow")]
        public void CompleteDeactivationWindow()
        {
            EnsureHost();
            host.CompleteDeactivationWindow();
            DumpState();
        }

        [ContextMenu("ContinueToNextActivity")]
        public void ContinueToNextActivity()
        {
            EnsureHost();
            host.ContinueToNextActivity();
            DumpState();
        }

        [ContextMenu("SendStaleFirstActivityCommand")]
        public void SendStaleFirstActivityCommand()
        {
            EnsureHost();
            host.ExecuteCommand(BuildStaleFirstActivityCommand(), "SendStaleFirstActivityCommand");
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

        [ContextMenu("Trace")]
        public void Trace()
        {
            EnsureHost();
            host.DumpTrace();
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
            GUILayout.Label("Session Activity", _titleStyle);
            GUILayout.Space(SectionSpacing);
            GUILayout.Label("QA canonicamente restrito: use apenas o lifecycle local sem atalhos de navegacao.", _labelStyle);
            GUILayout.Space(SectionSpacing);

            GUILayout.Space(SectionSpacing);

            bool canCompleteActivationWindow = CanCompleteActivationWindow();
            bool canCompleteCurrentActivity = CanCompleteCurrentActivity();
            bool canCompleteDeactivationWindow = CanCompleteDeactivationWindow();
            bool canContinueToNextActivity = CanContinueToNextActivity();

            GUI.enabled = canCompleteActivationWindow;
            if (GUILayout.Button("CompleteActivationWindow", _buttonStyle))
            {
                CompleteActivationWindow();
            }
            GUI.enabled = true;

            GUILayout.Space(SectionSpacing);

            GUI.enabled = canCompleteCurrentActivity;
            if (GUILayout.Button("CompleteCurrentActivity", _buttonStyle))
            {
                CompleteCurrentActivity();
            }
            GUI.enabled = true;

            GUILayout.Space(SectionSpacing);

            GUI.enabled = canCompleteDeactivationWindow;
            if (GUILayout.Button("CompleteDeactivationWindow", _buttonStyle))
            {
                CompleteDeactivationWindow();
            }
            GUI.enabled = true;

            GUILayout.Space(SectionSpacing);

            GUI.enabled = canContinueToNextActivity;
            if (GUILayout.Button("ContinueToNextActivity (requires handoff after next-setup)", _buttonStyle))
            {
                ContinueToNextActivity();
            }
            GUI.enabled = true;

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("DumpState", _buttonStyle))
            {
                DumpState();
            }

            GUILayout.Space(SectionSpacing);

            if (GUILayout.Button("Trace", _buttonStyle))
            {
                Trace();
            }

            if (showForeignStaleQa)
            {
                GUILayout.Space(SectionSpacing);
                GUILayout.Label("Foreign/Stale QA", _labelStyle);

                if (GUILayout.Button("SendStaleFirstActivityCommand", _buttonStyle))
                {
                    SendStaleFirstActivityCommand();
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

            host = FindFirstObjectByType<SessionActivityHost>(FindObjectsInactive.Include);
            if (host == null)
            {
                throw new InvalidOperationException("SessionActivityDebugPanel requires SessionActivityHost in the same scene.");
            }
        }

        private string BuildDumpText()
        {
            StringBuilder builder = new();
            builder.AppendLine("[OBS][SessionActivityPipeline][QA] DumpState");
            builder.AppendLine($"host='{host.name}'");
            builder.AppendLine($"pipelineId='{host.State.PipelineId}' sessionStateId='{host.State.SessionId}'");
            builder.AppendLine($"entrySequence='{host.State.CurrentEntrySequence}'");
            builder.AppendLine($"executionState='{host.State.CurrentExecutionState}'");
            builder.AppendLine($"gateState='{host.GateState}'");
            builder.AppendLine($"started='{host.State.HasStarted}' completed='{host.State.HasCompleted}' stage='{host.State.CurrentStage}' currentActivity='{host.State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"definition='{host.State.CurrentDefinition}'");
            builder.AppendLine($"identity='{host.State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{host.State.CurrentHandoff}'");
            builder.AppendLine($"pendingOperation='{host.State.CurrentPendingOperation}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
            builder.AppendLine("qaLifecycleRail='ActivityRunning -> CompleteCurrentActivity -> ContinueToNextActivity; CompleteActivationWindow/CompleteDeactivationWindow apenas quando window stage=Ready'");
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
            builder.AppendLine($"executionState='{host.State.CurrentExecutionState}'");
            builder.AppendLine($"gateState='{host.GateState}'");
            builder.AppendLine($"started='{host.State.HasStarted}' completed='{host.State.HasCompleted}' stage='{host.State.CurrentStage}'");
            builder.AppendLine($"currentActivity='{host.State.CurrentDefinition.ActivityId}'");
            builder.AppendLine($"pendingHandoffTarget='{GetPendingHandoffTarget()}'");
            builder.AppendLine($"nextExpectedQaAction='{GetNextExpectedQaAction()}'");
            builder.AppendLine($"identity='{host.State.CurrentIdentity}'");
            builder.AppendLine($"handoff='{host.State.CurrentHandoff}'");
            builder.AppendLine($"pendingOperation='{host.State.CurrentPendingOperation}'");
            if (host.GateState != null)
            {
                builder.AppendLine($"gateSessionBlocked='{host.GateState.SessionBlocked}' gateActivityBlocked='{host.GateState.ActivityBlocked}'");
                builder.AppendLine($"gateLastFact='{host.GateState.LastFact}'");
                builder.AppendLine($"gateLastSnapshot='{host.GateState.LastSnapshot}'");
            }
            return builder.ToString().TrimEnd();
        }

        private bool CanCompleteActivationWindow()
        {
            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivationWindowReady;
        }

        private bool CanCompleteCurrentActivity()
        {
            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.ActivityRunning;
        }

        private bool CanCompleteDeactivationWindow()
        {
            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.DeactivationWindowReady;
        }

        private bool CanContinueToNextActivity()
        {
            return !host.State.CurrentPendingOperation.IsValid &&
                   host.State.CurrentHandoff.IsValid &&
                   host.State.CurrentStage == SessionActivityStage.NextActivitySetupCompleted;
        }

        private string GetPendingHandoffTarget()
        {
            return host.State.CurrentHandoff.IsValid
                ? host.State.CurrentHandoff.NextActivityId
                : "<none>";
        }

        private string GetNextExpectedQaAction()
        {
            SessionActivityStage stage = host.State.CurrentStage;
            bool hasPendingHandoff = host.State.CurrentHandoff.IsValid;

            if (stage == SessionActivityStage.ActivationWindowReady)
            {
                return "CompleteActivationWindow";
            }

            if (stage == SessionActivityStage.ActivityRunning)
            {
                return "CompleteCurrentActivity";
            }

            if (stage == SessionActivityStage.DeactivationWindowReady)
            {
                return "CompleteDeactivationWindow";
            }

            if (stage == SessionActivityStage.NextActivitySetupCompleted && hasPendingHandoff)
            {
                return "ContinueToNextActivity";
            }

            if (stage == SessionActivityStage.Completed || stage == SessionActivityStage.ClosedForRouteExit)
            {
                return "No local QA action";
            }

            return "No local QA action";
        }

        private SessionActivityCommand BuildStaleFirstActivityCommand()
        {
            EnsureActiveIdentityOrFail("SendStaleFirstActivityCommand");
            if (!host.Catalog.TryGetFirst(out SessionActivityDefinition firstDefinition) || !firstDefinition.IsValid)
            {
                throw new InvalidOperationException("Stale command requires a valid first activity in runtime catalog.");
            }

            SessionActivityIdentity identity = new(
                host.State.PipelineId,
                host.State.SessionId,
                firstDefinition.ActivityId,
                firstDefinition.ActivityOrdinal,
                host.State.CurrentEntrySequence > 1 ? host.State.CurrentEntrySequence - 1 : 1,
                SessionActivityStage.ActivityRunning,
                "SessionActivityDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityDebugPanel",
                "Send stale first activity command.");
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
                "SessionActivityDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityDebugPanel",
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
                "SessionActivityDebugPanel");

            return new SessionActivityCommand(
                SessionActivityCommandKind.CompleteCurrentActivity,
                identity,
                "SessionActivityDebugPanel",
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

