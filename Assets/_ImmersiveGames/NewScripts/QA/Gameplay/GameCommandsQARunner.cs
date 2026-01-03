#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gameplay;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Gameplay
{
    public sealed class GameCommandsQARunner : MonoBehaviour
    {
        public const string RunKey = "NewScripts.GameCommandsQA.RunRequested";

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
        private IGameNavigationService _navigation;

        private EventBinding<GameRunStartedEvent> _runStartedBinding;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (PlayerPrefs.GetInt(RunKey, 0) != 1)
                return;

            PlayerPrefs.SetInt(RunKey, 0);
            PlayerPrefs.Save();

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
                EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
        }

        private void Start()
        {
            Debug.Log("[QA][GameCommands] Runner iniciado (gated by PlayerPrefs RunKey).");
            StartCoroutine(Run());
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            var line = condition ?? string.Empty;
            _logs.Add(line);
            _raw.AppendLine(line);

            // Evidência robusta de gameplay "rodando"
            if (line.IndexOf("ENTER: Playing", StringComparison.Ordinal) >= 0)
                _gameplayObserved = true;
        }

        private IEnumerator Run()
        {
            yield return WaitForGlobalServices(20f);

            if (_commands == null)
            {
                Fail("Não foi possível resolver IGameCommands no DI global.");
                FinalizeRun();
                yield break;
            }

            // Garantir gameplay antes de executar comandos
            yield return EnsureGameplay(60f);
            if (!_gameplayObserved)
            {
                Fail("Não foi possível observar Gameplay (Playing) antes de executar comandos.");
                FinalizeRun();
                yield break;
            }

            yield return RunPauseResume();
            if (_failed) { FinalizeRun(); yield break; }

            yield return RunDefeatRestartVictoryExit();

            FinalizeRun();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator WaitForGlobalServices(float timeout)
        {
            var deadline = Time.realtimeSinceStartup + timeout;

            while (Time.realtimeSinceStartup < deadline)
            {
                // Não assume DependencyManager.HasInstance; Provider costuma materializar o provider.
                if (_commands == null &&
                    DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var commands) && commands != null)
                {
                    _commands = commands;
                    AddStep("Resolve IGameCommands", true, "DI global pronto.");
                }

                if (_navigation == null &&
                    DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var nav) && nav != null)
                {
                    _navigation = nav;
                    AddStep("Resolve IGameNavigationService", true, "DI global pronto.");
                }

                if (_commands != null)
                    yield break;

                yield return null;
            }

            if (_commands == null)
                AddStep("Resolve IGameCommands", false, "Timeout aguardando DI global.");
        }

        private IEnumerator EnsureGameplay(float timeout)
        {
            if (_gameplayObserved)
            {
                AddStep("Ensure Gameplay", true, "Playing já observado.");
                yield break;
            }

            // Se estiver no menu, tenta navegar automaticamente (se nav existir)
            if (_navigation != null)
            {
                AddStep("Ensure Gameplay", true, "Solicitando navegação para Gameplay via IGameNavigationService.");

                var task = RequestGameplayAsync(_navigation);
                yield return WaitForTask(task, 20f, "Timeout aguardando RequestGameplayAsync.");
            }
            else
            {
                AddStep("Ensure Gameplay", true, "IGameNavigationService indisponível; aguarde manualmente entrar em Gameplay.");
            }

            // Aguarda evidências de gameplay (logs reais já vistos)
            var startIndex = _logs.Count;
            yield return WaitForEvidence("Wait Playing", timeout,
                startIndex,
                "Profile='gameplay'",
                "ENTER: Playing",
                "GameRunStartedEvent");
        }

        private static async Task RequestGameplayAsync(IGameNavigationService navigation)
        {
            await navigation.RequestGameplayAsync("qa_gamecommands");
        }

        private IEnumerator RunPauseResume()
        {
            _commands.RequestPause("qa");
            yield return WaitForEvidence("Pause", 12f, _logs.Count,
                "Acquire token='state.pause'",
                "ENTER: Paused",
                "GamePauseCommandEvent");

            if (_failed) yield break;

            _commands.RequestResume("qa");
            yield return WaitForEvidence("Resume", 12f, _logs.Count,
                "Release token='state.pause'. Active=0. IsOpen=True",
                "ENTER: Playing",
                "GameResumeRequestedEvent");
        }

        private IEnumerator RunDefeatRestartVictoryExit()
        {
            _commands.RequestDefeat("qa");
            yield return WaitForEvidence("Defeat -> PostGame", 25f, _logs.Count,
                "Acquire token='state.postgame'",
                "GameRunEndedEvent",
                "[PostGame]");

            if (_failed) yield break;

            _commands.RequestRestart("qa");
            yield return WaitForEvidence("Restart -> Gameplay", 60f, _logs.Count,
                "Iniciando transição: Load=[GameplayScene",
                "Profile='gameplay'",
                "ENTER: Playing");

            if (_failed) yield break;

            // Garante que estabilizou em gameplay antes da próxima
            yield return EnsureGameplay(40f);
            if (_failed) yield break;

            _commands.RequestVictory("qa");
            yield return WaitForEvidence("Victory -> PostGame", 25f, _logs.Count,
                "Acquire token='state.postgame'",
                "GameRunEndedEvent",
                "[PostGame]");

            if (_failed) yield break;

            _commands.RequestExitToMenu("qa");
            yield return WaitForEvidence("ExitToMenu -> Menu", 80f, _logs.Count,
                "Iniciando transição: Load=[MenuScene",
                "Active='MenuScene'",
                "Profile='startup'",
                "ENTER: Ready");
        }

        private IEnumerator WaitForEvidence(string stepLabel, float timeout, int startIndex, params string[] tokens)
        {
            var deadline = Time.realtimeSinceStartup + timeout;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (HasAnyLogSince(startIndex, tokens, out var matched))
                {
                    AddStep(stepLabel, true, $"Evidência via log: {matched}");
                    yield break;
                }

                yield return null;
            }

            AddStep(stepLabel, false, $"Timeout sem evidência. Tokens={string.Join(", ", tokens)}");
        }

        private bool HasAnyLogSince(int startIndex, string[] tokens, out string matched)
        {
            matched = null;

            startIndex = Mathf.Clamp(startIndex, 0, _logs.Count);

            for (var i = startIndex; i < _logs.Count; i++)
            {
                var line = _logs[i];
                for (var t = 0; t < tokens.Length; t++)
                {
                    if (line.IndexOf(tokens[t], StringComparison.Ordinal) >= 0)
                    {
                        matched = tokens[t];
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerator WaitForTask(Task task, float timeoutSeconds, string timeoutReason)
        {
            if (task == null)
            {
                Fail("Task nula em WaitForTask.");
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!task.IsCompleted)
            {
                Fail(timeoutReason);
                yield break;
            }

            if (task.IsFaulted)
                Fail($"Task falhou: {task.Exception?.GetBaseException().Message}");
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
            if (_failed) return;
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
            foreach (var line in GetLogTail(140))
                sb.AppendLine(line);
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
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, content, Encoding.UTF8);

#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QA][GameCommands] Falha ao escrever arquivo '{path}'. ex={ex.Message}");
            }
        }

        private string ResolveReportPath(string fileName)
        {
            // Application.dataPath -> .../Project/Assets
            return Path.Combine(Application.dataPath, ReportsRelativePath, fileName);
        }

        private IEnumerable<string> GetLogTail(int maxLines)
        {
            var start = Mathf.Max(0, _logs.Count - maxLines);
            for (var i = start; i < _logs.Count; i++)
                yield return _logs[i];
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
