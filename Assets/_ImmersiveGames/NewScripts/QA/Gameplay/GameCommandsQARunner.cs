#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gameplay;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Gameplay
{
    public sealed class GameCommandsQARunner : MonoBehaviour
    {
        private const string RunnerName = "[QA] GameCommandsQARunner";
        private const string ReportsRelativePath = "_ImmersiveGames/NewScripts/Docs/Reports";
        private const string ReportFileName = "GameCommands-QA-LastRun.md";
        private const string LogFileName = "GameCommands-QA-LastRun.log";

        private readonly List<string> _logs = new(4096);
        private readonly StringBuilder _raw = new(1024 * 64);
        private readonly List<StepResult> _steps = new();

        private bool _gameplayObserved;
        private bool _failed;
        private IGameCommands _commands;

        private EventBinding<GameRunStartedEvent> _runStartedBinding;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject(RunnerName);
            DontDestroyOnLoad(go);
            go.AddComponent<GameCommandsQARunner>();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;

            _runStartedBinding = new EventBinding<GameRunStartedEvent>(_ => _gameplayObserved = true);
            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;

            if (_runStartedBinding != null)
            {
                EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            }
        }

        private void Start()
        {
            Debug.Log("[QA][GameCommands] Runner iniciado.");
            StartCoroutine(Run());
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            var line = condition ?? string.Empty;
            _logs.Add(line);
            _raw.AppendLine(line);

            if (line.IndexOf("ENTER: Playing", StringComparison.Ordinal) >= 0)
            {
                _gameplayObserved = true;
            }
        }

        private IEnumerator Run()
        {
            yield return WaitForCommands(20f);
            if (_commands == null)
            {
                Fail("Não foi possível resolver IGameCommands no DI global.");
                FinalizeRun();
                yield break;
            }

            yield return WaitForGameplay(60f);

            yield return RunPauseResume();
            yield return RunDefeatRestartVictoryExit();

            FinalizeRun();
        }

        private IEnumerator RunPauseResume()
        {
            _commands.RequestPause("qa");
            yield return WaitForEvidence("Pause", 12f,
                "Acquire token='state.pause'",
                "ENTER: Paused",
                "GamePauseCommandEvent");

            _commands.RequestResume("qa");
            yield return WaitForEvidence("Resume", 12f,
                "Release token='state.pause'",
                "ENTER: Playing",
                "GameResumeRequestedEvent");
        }

        private IEnumerator RunDefeatRestartVictoryExit()
        {
            _commands.RequestDefeat("qa");
            yield return WaitForEvidence("Defeat -> PostGame", 20f,
                "Outcome=Defeat",
                "Acquire token='state.postgame'",
                "GameRunEndedEvent");

            _commands.RequestRestart("qa");
            yield return WaitForEvidence("Restart -> Gameplay", 40f,
                "GameResetRequestedEvent recebido -> RequestGameplayAsync",
                "Profile='gameplay'",
                "ENTER: Playing");

            yield return WaitForGameplay(40f);

            _commands.RequestVictory("qa");
            yield return WaitForEvidence("Victory -> PostGame", 20f,
                "Outcome=Victory",
                "Acquire token='state.postgame'",
                "GameRunEndedEvent");

            _commands.RequestExitToMenu("qa");
            yield return WaitForEvidence("ExitToMenu", 40f,
                "ExitToMenu recebido -> RequestMenuAsync",
                "Profile='startup'",
                "Active='MenuScene'");
        }

        private IEnumerator WaitForCommands(float timeout)
        {
            var deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var commands) && commands != null)
                {
                    _commands = commands;
                    AddStep("Resolve IGameCommands", true, "DI global pronto.");
                    yield break;
                }

                yield return null;
            }

            AddStep("Resolve IGameCommands", false, "Timeout aguardando DI global.");
        }

        private IEnumerator WaitForGameplay(float timeout)
        {
            if (_gameplayObserved)
            {
                AddStep("Wait Gameplay", true, "Sinal de Playing já observado.");
                yield break;
            }

            var startIndex = _logs.Count;
            var deadline = Time.realtimeSinceStartup + timeout;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (_gameplayObserved || HasAnyLogSince(startIndex, "ENTER: Playing", "GameRunStartedEvent"))
                {
                    _gameplayObserved = true;
                    AddStep("Wait Gameplay", true, "Playing observado via log/evento.");
                    yield break;
                }

                yield return null;
            }

            AddStep("Wait Gameplay", false, "Timeout sem evidência de Playing.");
        }

        private IEnumerator WaitForEvidence(string stepLabel, float timeout, params string[] tokens)
        {
            var startIndex = _logs.Count;
            var deadline = Time.realtimeSinceStartup + timeout;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (HasAnyLogSince(startIndex, tokens))
                {
                    AddStep(stepLabel, true, $"Evidência via log: {FirstMatchSince(startIndex, tokens)}");
                    yield break;
                }

                yield return null;
            }

            AddStep(stepLabel, false, $"Timeout sem evidência. Tokens={string.Join(", ", tokens)}");
        }

        private bool HasAnyLogSince(int startIndex, params string[] tokens)
        {
            for (var i = startIndex; i < _logs.Count; i++)
            {
                var line = _logs[i];
                for (var t = 0; t < tokens.Length; t++)
                {
                    if (line.IndexOf(tokens[t], StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private string FirstMatchSince(int startIndex, params string[] tokens)
        {
            for (var i = startIndex; i < _logs.Count; i++)
            {
                var line = _logs[i];
                for (var t = 0; t < tokens.Length; t++)
                {
                    if (line.IndexOf(tokens[t], StringComparison.Ordinal) >= 0)
                    {
                        return tokens[t];
                    }
                }
            }

            return "<none>";
        }

        private void AddStep(string label, bool success, string details)
        {
            _steps.Add(new StepResult(label, success, details));
            if (!success)
            {
                _failed = true;
                Debug.LogWarning($"[QA][GameCommands] FAIL: {label} -> {details}");
            }
            else
            {
                Debug.Log($"[QA][GameCommands] PASS: {label} -> {details}");
            }
        }

        private void Fail(string reason)
        {
            _failed = true;
            AddStep("Runner", false, reason);
        }

        private void FinalizeRun()
        {
            var reportPath = ResolveReportPath(ReportFileName);
            var logPath = ResolveReportPath(LogFileName);

            WriteReport(reportPath);
            WriteLog(logPath);

            var result = _failed ? "FAIL" : "PASS";
            Debug.Log($"[QA][GameCommands] Finalizado. Result={result} Report='{reportPath}' Log='{logPath}'.");
        }

        private void WriteReport(string path)
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("# GameCommands QA - Última execução");
            sb.AppendLine();
            sb.AppendLine($"- Resultado: **{(_failed ? "FAIL" : "PASS")}**");
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("## Etapas");
            foreach (var step in _steps)
            {
                var status = step.Success ? "PASS" : "FAIL";
                sb.AppendLine($"- [{status}] {step.Label} — {step.Details}");
            }

            sb.AppendLine();
            sb.AppendLine("## Janela final de logs");
            sb.AppendLine("```");
            foreach (var line in GetLogTail(120))
            {
                sb.AppendLine(line);
            }
            sb.AppendLine("```");

            WriteFileSafely(path, sb.ToString());
        }

        private void WriteLog(string path)
        {
            WriteFileSafely(path, _raw.ToString());
        }

        private void WriteFileSafely(string path, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, content);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QA][GameCommands] Falha ao escrever arquivo '{path}'. ex={ex.Message}");
            }
        }

        private string ResolveReportPath(string fileName)
        {
            var root = Application.dataPath;
            return Path.Combine(root, ReportsRelativePath, fileName);
        }

        private IEnumerable<string> GetLogTail(int maxLines)
        {
            var start = Mathf.Max(0, _logs.Count - maxLines);
            for (var i = start; i < _logs.Count; i++)
            {
                yield return _logs[i];
            }
        }

        private readonly struct StepResult
        {
            public StepResult(string label, bool success, string details)
            {
                Label = label;
                Success = success;
                Details = details;
            }

            public string Label { get; }
            public bool Success { get; }
            public string Details { get; }
        }
    }
}
#endif
