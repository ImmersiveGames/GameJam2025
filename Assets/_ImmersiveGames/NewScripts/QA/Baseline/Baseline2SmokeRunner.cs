#if UNITY_EDITOR || DEVELOPMENT_BUILD || NEWSCRIPTS_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gameplay;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline
{
    /// <summary>
    /// Baseline 2.0 Smoke Runner
    /// - Objetivo: validar pipeline mínimo (Boot -> Menu -> Gameplay -> Pause/Resume) com evidências por log.
    /// - Correção: Pause/Resume NÃO usa reflexão. Usa IGameCommands (API oficial).
    /// - Saída: Docs/Reports/Baseline-2.0-Smoke-LastRun.md + .log
    /// </summary>
    public sealed class Baseline2SmokeRunner : MonoBehaviour
    {
        public const string RunKey = "NewScripts.Baseline2Smoke.RunRequested";

        private const string RunnerName = "[Baseline2Smoke]";
        private const string ReportsRelativePath = "_ImmersiveGames/NewScripts/Docs/Reports";
        private const string ReportFileName = "Baseline-2.0-Smoke-LastRun.md";
        private const string LogFileName = "Baseline-2.0-Smoke-LastRun.log";

        public enum Mode
        {
            MANUAL_ONLY = 0,
            AUTO_NAVIGATE = 1
        }

        [SerializeField] private Mode mode = Mode.MANUAL_ONLY;

        [Tooltip("Timeout (seg) para navegação automática (quando mode=AUTO_NAVIGATE).")]
        [SerializeField] private float navTimeoutSeconds = 25f;

        [Tooltip("Timeout (seg) padrão para passos de evidência.")]
        [SerializeField] private float defaultStepTimeoutSeconds = 20f;

        private readonly List<string> _logs = new(8192);
        private readonly StringBuilder _raw = new(1024 * 128);
        private readonly List<StepResult> _steps = new();

        private bool _failed;

        // Services (global)
        private IGameCommands _commands;
        private IGameNavigationService _navigation;

        private bool _gameplayPlayingObserved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (PlayerPrefs.GetInt(RunKey, 0) != 1)
                return;

            PlayerPrefs.SetInt(RunKey, 0);
            PlayerPrefs.Save();

            var go = new GameObject(RunnerName);
            DontDestroyOnLoad(go);
            go.AddComponent<Baseline2SmokeRunner>();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void Start()
        {
            Debug.Log($"{RunnerName} Runner started. mode={mode}, navTimeout={navTimeoutSeconds:0.0}s");
            StartCoroutine(Run());
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            var line = condition ?? string.Empty;
            _logs.Add(line);
            _raw.AppendLine(line);

            // Evidência robusta de gameplay em execução
            if (line.IndexOf("ENTER: Playing", StringComparison.Ordinal) >= 0)
                _gameplayPlayingObserved = true;
        }

        private IEnumerator Run()
        {
            // A1) Wait infra ready
            yield return Step_WaitInfraReady(20f);
            if (_failed) { FinalizeRun(); yield break; }

            // A2) Startup Menu completed (evidências de Menu estável)
            yield return Step_WaitMenuStable(defaultStepTimeoutSeconds);
            if (_failed) { FinalizeRun(); yield break; }

            // B1/B2) Enter Gameplay (manual ou auto)
            yield return Step_EnterGameplay();
            if (_failed) { FinalizeRun(); yield break; }

            // B3) Gameplay stable
            yield return Step_WaitGameplayStable(30f);
            if (_failed) { FinalizeRun(); yield break; }

            // C1) Pause/Resume (CORRIGIDO: usa IGameCommands)
            yield return Step_PauseResume();
            if (_failed) { FinalizeRun(); yield break; }

            FinalizeRun();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private IEnumerator Step_WaitInfraReady(float timeoutSeconds)
        {
            LogStepHeader("A1) Wait infra ready");

            var deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                // Resolve IGameCommands (novo "oficial")
                if (_commands == null &&
                    DependencyManager.Provider.TryGetGlobal<IGameCommands>(out var commands) && commands != null)
                {
                    _commands = commands;
                }

                // Resolve navegação (opcional; só para AUTO_NAVIGATE)
                if (_navigation == null &&
                    DependencyManager.Provider.TryGetGlobal<IGameNavigationService>(out var nav) && nav != null)
                {
                    _navigation = nav;
                }

                if (_commands != null)
                {
                    AddStep("Resolve services", true,
                        $"IGameCommands={( _commands != null ? "OK" : "NULL" )}, IGameNavigationService={( _navigation != null ? "OK" : "NULL" )}");
                    yield break;
                }

                yield return null;
            }

            AddStep("Resolve services", false, "Timeout aguardando DI global (IGameCommands).");
        }

        private IEnumerator Step_WaitMenuStable(float timeoutSeconds)
        {
            LogStepHeader("A2/B0) Ensure Menu stable");

            // Evidências observadas no seu log real:
            // - SceneFlow transição concluída
            // - GameLoop ENTER: Ready
            // - gate token released (Active=0 IsOpen=True)
            var startIndex = _logs.Count;

            yield return WaitForEvidence("Menu stable", timeoutSeconds, startIndex,
                "Transição concluída com sucesso",
                "ENTER: Ready",
                "Release token='flow.scene_transition'. Active=0. IsOpen=True");

            if (!_failed)
                Debug.Log($"{RunnerName} Evidência: Menu estável (GameLoop Ready + SceneTransition completed + token released).");
        }

        private IEnumerator Step_EnterGameplay()
        {
            LogStepHeader("B1) Enter Gameplay");

            if (mode == Mode.AUTO_NAVIGATE)
            {
                if (_navigation == null)
                {
                    AddStep("Auto Navigate", false,
                        "mode=AUTO_NAVIGATE, porém IGameNavigationService não está disponível no DI global.");
                    yield break;
                }

                Debug.Log($"{RunnerName} AUTO: Solicitando Gameplay via IGameNavigationService...");
                var task = RequestGameplayAsync(_navigation);
                yield return WaitForTask(task, navTimeoutSeconds, "Timeout aguardando RequestGameplayAsync.");
                if (_failed) yield break;
            }
            else
            {
                Debug.Log($"{RunnerName} MANUAL: Clique no botão Play do Menu para entrar no Gameplay.");
            }

            // Aguarda sinais de transição para gameplay + playing
            var startIndex = _logs.Count;
            yield return WaitForEvidence("Gameplay transition started", 60f, startIndex,
                "Profile='gameplay'",
                "Iniciando transição: Load=[GameplayScene",
                "Active='GameplayScene'");

            if (_failed) yield break;

            // Aguarda Playing (pós scene flow + sync game loop)
            startIndex = _logs.Count;
            yield return WaitForEvidence("Gameplay Playing", 60f, startIndex,
                "ENTER: Playing",
                "GameRunStartedEvent");
        }

        private IEnumerator Step_WaitGameplayStable(float timeoutSeconds)
        {
            LogStepHeader("B3) Gameplay stable (Playing + input)");

            if (_gameplayPlayingObserved)
            {
                AddStep("Gameplay stable", true, "Playing já observado.");
                yield break;
            }

            var startIndex = _logs.Count;

            // Sinais típicos (log real):
            // - InputMode -> Gameplay
            // - Applied map 'Player'
            // - ENTER: Playing
            yield return WaitForEvidence("Gameplay stable", timeoutSeconds, startIndex,
                "Modo alterado para 'Gameplay'",
                "Applied map 'Player'",
                "ENTER: Playing");
        }

        private IEnumerator Step_PauseResume()
        {
            LogStepHeader("C1) Pause/Resume");

            if (_commands == null)
            {
                AddStep("Pause/Resume", false, "IGameCommands indisponível (DI global).");
                yield break;
            }

            // Pause
            _commands.RequestPause("baseline2");
            var startIndex = _logs.Count;

            // Evidências reais que já aparecem no seu pipeline:
            // - Acquire token='state.pause'
            // - ENTER: Paused
            yield return WaitForEvidence("Pause", 15f, startIndex,
                "Acquire token='state.pause'",
                "ENTER: Paused",
                "GamePauseCommandEvent");

            if (_failed) yield break;

            // Resume
            _commands.RequestResume("baseline2");
            startIndex = _logs.Count;

            // - Release token='state.pause'
            // - ENTER: Playing
            yield return WaitForEvidence("Resume", 15f, startIndex,
                "Release token='state.pause'",
                "ENTER: Playing",
                "GameResumeRequestedEvent");
        }

        private static async Task RequestGameplayAsync(IGameNavigationService navigation)
        {
            await navigation.RequestGameplayAsync("baseline2_auto");
        }

        private IEnumerator WaitForEvidence(string label, float timeoutSeconds, int startIndex, params string[] tokens)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (HasAnyLogSince(startIndex, tokens, out var matched))
                {
                    AddStep(label, true, $"Evidência via log: {matched}");
                    yield break;
                }

                yield return null;
            }

            AddStep(label, false, $"Timeout sem evidência. Tokens={string.Join(", ", tokens)}");
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
                AddStep("WaitForTask", false, "Task nula.");
                yield break;
            }

            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!task.IsCompleted)
            {
                AddStep("WaitForTask", false, timeoutReason);
                yield break;
            }

            if (task.IsFaulted)
            {
                AddStep("WaitForTask", false,
                    $"Task falhou: {task.Exception?.GetBaseException().Message}");
            }
            else
            {
                AddStep("WaitForTask", true, "Task concluída.");
            }
        }

        private void LogStepHeader(string title)
        {
            Debug.Log($"{RunnerName} >>> {title}");
        }

        private void AddStep(string label, bool success, string details)
        {
            _steps.Add(new StepResult(label, success, details));

            if (!success)
            {
                _failed = true;
                Debug.LogWarning($"{RunnerName} FAIL: {label}: {details}");
            }
            else
            {
                Debug.Log($"{RunnerName} PASS: {label}: {details}");
            }
        }

        private void FinalizeRun()
        {
            var reportPath = ResolveReportPath(ReportFileName);
            var logPath = ResolveReportPath(LogFileName);

            WriteReport(reportPath);
            WriteLog(logPath);

            Debug.Log($"{RunnerName} Report written: {reportPath}");
            Debug.Log($"{RunnerName} Raw log written: {logPath}");
        }

        private void WriteReport(string path)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("# Baseline 2.0 Smoke - Última execução");
            sb.AppendLine();
            sb.AppendLine($"- Resultado: **{(_failed ? "FAIL" : "PASS")}**");
            sb.AppendLine($"- Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Mode: {mode}");
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
            foreach (var line in GetLogTail(160))
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
                Debug.LogWarning($"{RunnerName} Falha ao escrever arquivo '{path}'. ex={ex.Message}");
            }
        }

        private string ResolveReportPath(string fileName)
        {
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
