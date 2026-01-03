using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Navigation;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline
{
    public sealed class Baseline2SmokeRunner : MonoBehaviour
    {
        public const string RunKey = "NewScripts.Baseline2Smoke.RunRequested";

        // Preferences (Editor menu set these)
        private const string PrefManualPlayKey = "NewScripts.Baseline2Smoke.ManualPlay"; // kept for compatibility (ignored)
        private const string PrefAutoNavigateTimeoutKey = "NewScripts.Baseline2Smoke.AutoNavigateTimeoutSeconds";

        private readonly List<string> _lines = new List<string>(8192);
        private readonly StringBuilder _raw = new StringBuilder(1024 * 256);

        private bool _failed;
        private string _failReason;
        private string _failCategory;
        private string _failLogWindow;

        // NOTE: As requested, this runner is now ALWAYS manual for menu navigation steps.
        private bool _manualPlay = true;
        private float _autoNavigateTimeoutSeconds;

        private string _resolvedNavigationType;

        private bool _menuTransitionCompleted;
        private bool _gameLoopReady;
        private bool _gameLoopPlaying;
        private bool _navLogObserved;
        private bool _gameplayTransitionStarted;
        private bool _gameplayTransitionCompleted;

        private bool _moveUnblocked;
        private bool _worldResetCompletedObserved;
        private bool _actorSpawnedObserved;
        private bool _spawnRegistryTwoObserved;

        private float _startRealtime;
        private string _activeSignatureLast;
        private string _profileLast;

        private EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (PlayerPrefs.GetInt(RunKey, 0) != 1)
                return;

            PlayerPrefs.SetInt(RunKey, 0);
            PlayerPrefs.Save();

            var go = new GameObject("[QA] Baseline2SmokeRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<Baseline2SmokeRunner>();
        }

        private void Awake()
        {
            // Compatibility: read prefs but we no longer auto-navigate menus.
            // This prevents the flaky "auto open" behavior and makes runs deterministic under manual control.
            _manualPlay = true;

            var rawTimeout = PlayerPrefs.GetFloat(PrefAutoNavigateTimeoutKey, 25f);
            _autoNavigateTimeoutSeconds = Mathf.Clamp(rawTimeout, 5f, 120f);

            // Keep the old key around (ignored) so existing editor tooling doesn't break.
            _ = PlayerPrefs.GetInt(PrefManualPlayKey, 1);
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;

            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnSceneTransitionStarted);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;

            EventBus<SceneTransitionStartedEvent>.Unregister(_transitionStartedBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
        }

        private void Start()
        {
            _startRealtime = Time.realtimeSinceStartup;

            Debug.Log($"[Baseline2Smoke] Runner started. mode=MANUAL_ONLY, navTimeout={_autoNavigateTimeoutSeconds:0.0}s");
            StartCoroutine(Run());
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            var line = condition ?? string.Empty;
            _lines.Add(line);
            _raw.AppendLine(line);

            TryExtractContextHints(line);
            TryFailFastOnGlobalErrors(line, stackTrace, type);
            TryCaptureEvidence(line);
        }

        private void TryFailFastOnGlobalErrors(string line, string stackTrace, LogType type)
        {
            if (_failed)
                return;

            // Fail-fast for exceptions and obvious failures. This is intentionally broad to avoid ambiguous passes.
            if (type == LogType.Exception)
            {
                Fail("Unity log exception observed (LogType.Exception).");
                return;
            }

            if (line.IndexOf("NullReferenceException", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Fail("NullReferenceException observed in logs.");
                return;
            }

            if (line.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 &&
                line.IndexOf("[Baseline2Smoke]", StringComparison.Ordinal) < 0) // avoid double-trigger on our own messages
            {
                Fail("Exception-like text observed in logs.");
                return;
            }

            if (line.IndexOf("FAIL:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                line.IndexOf("[Baseline2Smoke]", StringComparison.Ordinal) < 0)
            {
                Fail("FAIL marker observed in logs.");
                return;
            }

            // Specific regression guard: startup/frontend must NOT trigger hard reset.
            if (line.IndexOf("Disparando hard reset após ScenesReady", StringComparison.Ordinal) >= 0 &&
                (line.IndexOf("profile='startup'", StringComparison.Ordinal) >= 0 ||
                 line.IndexOf("Profile='startup'", StringComparison.Ordinal) >= 0 ||
                 line.IndexOf("profile='frontend'", StringComparison.Ordinal) >= 0 ||
                 line.IndexOf("Profile='frontend'", StringComparison.Ordinal) >= 0))
            {
                _failCategory = "startup/frontend hard reset";
                Fail("Hard reset observed for startup/frontend profile (should be SKIP).");
            }
        }

        private void TryExtractContextHints(string line)
        {
            const string sigKey = "signature='";
            var sigIdx = line.IndexOf(sigKey, StringComparison.Ordinal);
            if (sigIdx >= 0)
            {
                var start = sigIdx + sigKey.Length;
                var end = line.IndexOf("'", start, StringComparison.Ordinal);
                if (end > start)
                    _activeSignatureLast = line.Substring(start, end - start);
            }

            const string profKey = "Profile='";
            var pIdx = line.IndexOf(profKey, StringComparison.Ordinal);
            if (pIdx >= 0)
            {
                var start = pIdx + profKey.Length;
                var end = line.IndexOf("'", start, StringComparison.Ordinal);
                if (end > start)
                    _profileLast = line.Substring(start, end - start);
            }
        }

        private void TryCaptureEvidence(string line)
        {
            // Gameplay completion (Readiness log)
            if (line.IndexOf("[Readiness] SceneTransitionCompleted", StringComparison.Ordinal) >= 0 &&
                line.IndexOf("Profile='gameplay'", StringComparison.Ordinal) >= 0)
            {
                _gameplayTransitionCompleted = true;
            }

            // GameLoop state evidence
            if (line.IndexOf("[GameLoop] ENTER: Ready", StringComparison.Ordinal) >= 0)
                _gameLoopReady = true;

            if (line.IndexOf("[GameLoop] ENTER: Playing", StringComparison.Ordinal) >= 0)
                _gameLoopPlaying = true;

            // Navigation evidence (log)
            if (line.IndexOf("[Navigation] NavigateAsync -> routeId='to-gameplay'", StringComparison.Ordinal) >= 0)
                _navLogObserved = true;

            // SceneFlow started (log)
            if (line.IndexOf("[SceneFlow] Iniciando transição:", StringComparison.Ordinal) >= 0 &&
                line.IndexOf("Profile='gameplay'", StringComparison.Ordinal) >= 0)
            {
                _gameplayTransitionStarted = true;
            }

            // Movement unblocked evidence (StateDependent)
            if (line.IndexOf("Action 'Move' liberada", StringComparison.Ordinal) >= 0)
                _moveUnblocked = true;

            // Reset completion evidence
            if (line.IndexOf("World Reset Completed", StringComparison.Ordinal) >= 0)
                _worldResetCompletedObserved = true;

            // Spawn evidence (either Player or Eater)
            if (line.IndexOf("Actor spawned:", StringComparison.Ordinal) >= 0)
                _actorSpawnedObserved = true;

            // Spawn registry evidence for definition count (expected 2 in current baseline)
            if (line.IndexOf("Spawn services registered from definition: 2", StringComparison.Ordinal) >= 0)
                _spawnRegistryTwoObserved = true;

            // Menu "completed" robust detection via logs:
            if (!_menuTransitionCompleted)
            {
                if (line.IndexOf("Release token='flow.scene_transition'. Active=0. IsOpen=True", StringComparison.Ordinal) >= 0 &&
                    (line.IndexOf("Profile='startup'", StringComparison.Ordinal) >= 0 || line.IndexOf("Active='MenuScene'", StringComparison.Ordinal) >= 0))
                {
                    _menuTransitionCompleted = true;
                }
                else if (line.IndexOf("[SceneFlow] Transição concluída com sucesso", StringComparison.Ordinal) >= 0)
                {
                    _menuTransitionCompleted = true;
                }
                else if (line.IndexOf("[Readiness] SceneTransitionCompleted", StringComparison.Ordinal) >= 0 &&
                         line.IndexOf("Profile='startup'", StringComparison.Ordinal) >= 0 &&
                         line.IndexOf("Active='MenuScene'", StringComparison.Ordinal) >= 0)
                {
                    _menuTransitionCompleted = true;
                }
            }
        }

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (evt.Context.TransitionProfileId.IsGameplay)
                _gameplayTransitionStarted = true;
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            try
            {
                if (string.Equals(evt.Context.TargetActiveScene, "MenuScene", StringComparison.Ordinal))
                    _menuTransitionCompleted = true;
            }
            catch
            {
                // Intencional: não quebrar smoke por divergência de API do Context.
            }

            if (evt.Context.TransitionProfileId.IsGameplay)
                _gameplayTransitionCompleted = true;
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            var steps = new List<SmokeStep>
            {
                new SmokeStep("A1) Wait infra ready", () => WaitForAnyLog(12f,
                    "✅ NewScripts global infrastructure initialized",
                    "[GlobalBootstrap] [EventBus] EventBus inicializado",
                    "[SceneFlow] SceneTransitionService nativo registrado",
                    "[Navigation] GameNavigationService registrado no DI global",
                    "[GameLoopSceneFlow] Coordinator registrado")),

                new SmokeStep("A2) Startup Menu completed", () => ExpectInOrder(30f,
                    "Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'",
                    "Acquire token='flow.scene_transition'",
                    "SceneTransitionScenesReady recebido",
                    "Reset SKIPPED (startup/frontend)",
                    "Emitting WorldLifecycleResetCompletedEvent. profile='startup'",
                    "Release token='flow.scene_transition'. Active=0. IsOpen=True",
                    "[SceneFlow] Transição concluída com sucesso")),

                new SmokeStep("B0) Ensure Menu stable", () => WaitForMenuStable(6f)),

                new SmokeStep("B1) Enter Gameplay (MANUAL)", () => EnterGameplay(_autoNavigateTimeoutSeconds)),
                new SmokeStep("B2) Gameplay reset+spawn", () => WaitForGameplayResetAndSpawn(25f)),
                new SmokeStep("B3) Gameplay stable (Playing + input)", () => WaitForGameplayStable(10f)),

                new SmokeStep("C1) Pause/Resume", () => PauseResume(15f)),

                new SmokeStep("D1) Force Defeat (GameRunEndedEvent)", () => ForceOutcome("Defeat", "QA_ForcedDefeat", 15f)),
                new SmokeStep("D2) Restart (MANUAL navigation trigger)", () => RestartManual(60f)),

                new SmokeStep("E1) Force Victory (GameRunEndedEvent)", () => ForceOutcome("Victory", "QA_ForcedVictory", 15f)),
                new SmokeStep("E2) ExitToMenu (MANUAL navigation trigger)", () => ExitToMenuManual(60f))
            };

            foreach (var s in steps)
            {
                if (_failed) break;
                Debug.Log($"[Baseline2Smoke] >>> {s.Name}");
                yield return s.Run();
            }

            if (!_failed)
            {
                if (!CheckTokenBalanced("flow.scene_transition")) Fail("Token leak: flow.scene_transition (Acquire != Release).");
                if (!CheckTokenBalanced("WorldLifecycle.WorldReset")) Fail("Token leak: WorldLifecycle.WorldReset (Acquire != Release).");
                if (!CheckTokenBalanced("state.pause")) Fail("Token leak: state.pause (Acquire != Release).");
                if (!CheckTokenBalanced("state.postgame")) Fail("Token leak: state.postgame (Acquire != Release).");
            }

            WriteReport();

            yield return new WaitForSecondsRealtime(0.25f);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ---------------------------
        // Steps
        // ---------------------------

        private IEnumerator EnterGameplay(float timeoutSeconds)
        {
            Debug.Log("[Baseline2Smoke] MANUAL: Clique no botão Play do Menu para entrar no Gameplay.");
            yield return WaitForCondition(timeoutSeconds,
                () => _gameplayTransitionStarted || _navLogObserved || Contains("Profile='gameplay'"),
                "Clique no Play / início de transição gameplay (event/log)");
        }

        private IEnumerator RestartManual(float timeoutSeconds)
        {
            Debug.Log("[Baseline2Smoke] MANUAL: Acione Restart (UI/tecla/evento) para reiniciar a Gameplay.");

            yield return WaitForCondition(timeoutSeconds,
                () => Contains("Iniciando transição: Load=[GameplayScene, UIGlobalScene]") ||
                      Contains("Active='GameplayScene'") ||
                      Contains("Profile='gameplay'"),
                "Evidência de transição para Gameplay (Restart)");

            if (_failed) yield break;

            yield return WaitForAnyLog(timeoutSeconds,
                "Emitting WorldLifecycleResetCompletedEvent. profile='gameplay'",
                "World Reset Completed");

            if (_failed) yield break;

            yield return WaitForAnyLog(timeoutSeconds,
                "Release token='flow.scene_transition'. Active=0. IsOpen=True",
                "[SceneFlow] Transição concluída com sucesso");
        }

        private IEnumerator ExitToMenuManual(float timeoutSeconds)
        {
            Debug.Log("[Baseline2Smoke] MANUAL: Acione ExitToMenu (UI/tecla/evento) para voltar ao Menu.");

            yield return WaitForCondition(timeoutSeconds,
                () => Contains("Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene'") ||
                      Contains("Active='MenuScene'"),
                "Evidência de transição para Menu");

            if (_failed) yield break;

            yield return WaitForAnyLog(timeoutSeconds,
                "Reset SKIPPED (startup/frontend)",
                "Emitting WorldLifecycleResetCompletedEvent. profile='startup'",
                "Emitting WorldLifecycleResetCompletedEvent. profile='frontend'");

            if (_failed) yield break;

            yield return WaitForAnyLog(timeoutSeconds,
                "Release token='flow.scene_transition'. Active=0. IsOpen=True",
                "[SceneFlow] Transição concluída com sucesso");
        }

        private IEnumerator WaitForMenuStable(float timeoutSeconds)
        {
            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                var tokenReleased = Contains("Release token='flow.scene_transition'. Active=0. IsOpen=True");
                var transitionSeal = _menuTransitionCompleted || Contains("[SceneFlow] Transição concluída com sucesso");
                var stable = _gameLoopReady && transitionSeal && tokenReleased;

                if (stable)
                {
                    Debug.Log("[Baseline2Smoke] Evidência: Menu estável (GameLoop Ready + SceneTransition completed + token released).");
                    yield break;
                }

                yield return null;
            }

            _failCategory = "menu não estabilizou";
            Fail("Timeout aguardando estado estável de Menu (GameLoop Ready + TransitionCompleted + Release token).");
        }

        private IEnumerator WaitForGameplayResetAndSpawn(float timeoutSeconds)
        {
            yield return ExpectInOrder(timeoutSeconds,
                "Iniciando transição: Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'",
                "Acquire token='flow.scene_transition'",
                "WorldRoot ready: GameplayScene/WorldRoot",
                "WorldDefinition loaded: WorldDefinition",
                "Spawn services registered from definition: 2",
                "SceneTransitionScenesReady recebido",
                "Disparando hard reset após ScenesReady",
                "Acquire token='WorldLifecycle.WorldReset'",
                "Actor spawned:",
                "World Reset Completed",
                "Emitting WorldLifecycleResetCompletedEvent. profile='gameplay'",
                "Release token='flow.scene_transition'. Active=0. IsOpen=True");

            if (_failed)
            {
                if (_gameplayTransitionStarted && !_gameplayTransitionCompleted)
                    _failCategory = "iniciou mas não completou";

                yield break;
            }

            if (!_worldResetCompletedObserved)
            {
                _failCategory = "reset ausente";
                Fail("Contrato violado: 'World Reset Completed' não observado.");
                yield break;
            }

            if (!_spawnRegistryTwoObserved)
            {
                _failCategory = "worlddefinition/spawn registry";
                Fail("Contrato violado: 'Spawn services registered from definition: 2' não observado.");
                yield break;
            }

            if (!_actorSpawnedObserved)
            {
                _failCategory = "spawn não ocorreu";
                Fail("Contrato violado: nenhum 'Actor spawned:' observado durante B2.");
                yield break;
            }
        }

        private IEnumerator WaitForGameplayStable(float timeoutSeconds)
        {
            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                var tokenReleased = Contains("Release token='flow.scene_transition'. Active=0. IsOpen=True");
                var stable = _gameLoopPlaying && _moveUnblocked && tokenReleased;

                if (stable)
                    yield break;

                yield return null;
            }

            _failCategory = "gameplay não estabilizou";
            Fail("Timeout aguardando Gameplay estável (GameLoop Playing + Move liberada + Release flow.scene_transition).");
        }

        private IEnumerator PauseResume(float timeoutSeconds)
        {
            // FIX: GamePauseCommandEvent pode não ter construtor padrão.
            // Agora o publish tenta construtores com args e fallback editor para tipos sem ctor acessível.
            if (!TryPublishEventByName("GamePauseCommandEvent", out var err1))
            {
                _failCategory = "pause publish";
                Fail($"Falha ao publicar GamePauseCommandEvent: {err1}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds,
                "Acquire token='state.pause'",
                "[GameLoop] ENTER: Paused",
                "ENTER: Paused");

            if (_failed) yield break;

            if (!TryPublishEventByName("GameResumeRequestedEvent", out var err2))
            {
                _failCategory = "resume publish";
                Fail($"Falha ao publicar GameResumeRequestedEvent: {err2}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds,
                "Release token='state.pause'. Active=0. IsOpen=True",
                "[GameLoop] ENTER: Playing",
                "ENTER: Playing");
        }

        private IEnumerator ForceOutcome(string outcomeName, string reason, float timeoutSeconds)
        {
            if (!TryPublishGameRunEnded(outcomeName, reason, out var err))
            {
                _failCategory = "force outcome publish";
                Fail($"Falha ao publicar GameRunEndedEvent ({outcomeName}): {err}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds, "GameRunEndedEvent", "Acquire token='state.postgame'", "state.postgame");
        }

        // ---------------------------
        // Wait utilities
        // ---------------------------

        private IEnumerator WaitForAnyLog(float timeoutSeconds, params string[] needles)
        {
            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                if (_failed) yield break;

                if (needles.Any(Contains))
                    yield break;

                yield return null;
            }

            Fail($"Timeout aguardando qualquer evidência: {string.Join(" OR ", needles)}");
        }

        private IEnumerator WaitForCondition(float timeoutSeconds, Func<bool> predicate, string label, Action onObserved = null)
        {
            if (predicate == null)
            {
                Fail("Predicate nulo em WaitForCondition.");
                yield break;
            }

            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                if (_failed) yield break;

                if (predicate())
                {
                    onObserved?.Invoke();
                    yield break;
                }

                yield return null;
            }

            Fail($"Timeout aguardando evidência: {label}");
        }

        private IEnumerator ExpectInOrder(float timeoutSeconds, params string[] orderedNeedles)
        {
            var start = Time.realtimeSinceStartup;

            var searchFrom = 0;
            var cursor = 0;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                if (_failed) yield break;

                while (cursor < orderedNeedles.Length)
                {
                    var idx = IndexOfFrom(orderedNeedles[cursor], searchFrom);
                    if (idx < 0)
                        break;

                    searchFrom = idx + 1;
                    cursor++;
                }

                if (cursor >= orderedNeedles.Length)
                    yield break;

                yield return null;
            }

            var missing = orderedNeedles[Mathf.Clamp(cursor, 0, orderedNeedles.Length - 1)];
            Fail($"Timeout esperando sequência mínima em ordem. Faltou: '{missing}'");
        }

        // ---------------------------
        // Token validation/report
        // ---------------------------

        private bool CheckTokenBalanced(string token)
        {
            var acq = CountContains($"Acquire token='{token}'");
            var rel = CountContains($"Release token='{token}'");
            return acq == rel;
        }

        private void WriteReport()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var reportsDir = Path.Combine(projectRoot, "Assets/_ImmersiveGames/NewScripts/Docs/Reports");
            Directory.CreateDirectory(reportsDir);

            var mdPath = Path.Combine(reportsDir, "Baseline-2.0-Smoke-LastRun.md");
            var logPath = Path.Combine(reportsDir, "Baseline-2.0-Smoke-LastRun.log");

            File.WriteAllText(logPath, _raw.ToString(), Encoding.UTF8);

            var dur = Time.realtimeSinceStartup - _startRealtime;
            var status = _failed ? "FAIL" : "PASS";

            var md = new StringBuilder();
            md.AppendLine("# Baseline 2.0 — Smoke Run (Editor PlayMode)");
            md.AppendLine();
            md.AppendLine($"- Result: **{status}**");
            md.AppendLine($"- Duration: `{dur:0.00}s`");
            md.AppendLine($"- Mode: `MANUAL_ONLY`");
            md.AppendLine($"- Last signature seen: `{_activeSignatureLast ?? "<unknown>"}`");
            md.AppendLine($"- Last profile seen: `{_profileLast ?? "<unknown>"}`");
            md.AppendLine();

            if (_failed)
            {
                md.AppendLine("## Failure reason");
                md.AppendLine();
                md.AppendLine($"- {_failReason}");
                if (!string.IsNullOrWhiteSpace(_failCategory))
                    md.AppendLine($"- Categoria: `{_failCategory}`");
                md.AppendLine();

                md.AppendLine("## Failure log window (tail)");
                md.AppendLine();
                md.AppendLine("```");
                md.AppendLine(_failLogWindow ?? GetLastLinesJoined(80));
                md.AppendLine("```");
                md.AppendLine();
            }

            md.AppendLine("## Evidências");
            md.AppendLine();
            md.AppendLine($"- Menu TransitionCompleted (flag): `{_menuTransitionCompleted}`");
            md.AppendLine($"- GameLoop Ready (log): `{_gameLoopReady}`");
            md.AppendLine($"- GameLoop Playing (log): `{_gameLoopPlaying}`");
            md.AppendLine($"- Nav log to-gameplay observado: `{_navLogObserved}`");
            md.AppendLine($"- Gameplay transition started observado: `{_gameplayTransitionStarted}`");
            md.AppendLine($"- Gameplay transition completed observado: `{_gameplayTransitionCompleted}`");
            md.AppendLine($"- Reset completed observado: `{_worldResetCompletedObserved}`");
            md.AppendLine($"- Spawn registry (=2) observado: `{_spawnRegistryTwoObserved}`");
            md.AppendLine($"- Actor spawned observado: `{_actorSpawnedObserved}`");
            md.AppendLine($"- Move liberada observado: `{_moveUnblocked}`");
            md.AppendLine($"- IGameNavigationService resolvido: `{_resolvedNavigationType ?? "<ignored (manual)>"}`");
            md.AppendLine();

            md.AppendLine("## Token balance (Acquire vs Release)");
            md.AppendLine();
            md.AppendLine($"- flow.scene_transition: `{CountContains("Acquire token='flow.scene_transition'")}` vs `{CountContains("Release token='flow.scene_transition'")}`");
            md.AppendLine($"- WorldLifecycle.WorldReset: `{CountContains("Acquire token='WorldLifecycle.WorldReset'")}` vs `{CountContains("Release token='WorldLifecycle.WorldReset'")}`");
            md.AppendLine($"- state.pause: `{CountContains("Acquire token='state.pause'")}` vs `{CountContains("Release token='state.pause'")}`");
            md.AppendLine($"- state.postgame: `{CountContains("Acquire token='state.postgame'")}` vs `{CountContains("Release token='state.postgame'")}`");
            md.AppendLine();

            md.AppendLine("## Artifacts");
            md.AppendLine();
            md.AppendLine("- Raw log: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log`");
            md.AppendLine("- Report: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md`");
            md.AppendLine();

            File.WriteAllText(mdPath, md.ToString(), Encoding.UTF8);

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            Debug.Log($"[Baseline2Smoke] Report written: {mdPath}");
            Debug.Log($"[Baseline2Smoke] Raw log written: {logPath}");
        }

        private void Fail(string reason)
        {
            if (_failed) return;
            _failed = true;
            _failReason = reason;
            _failLogWindow = GetLastLinesJoined(80);
            Debug.LogError("[Baseline2Smoke] FAIL: " + reason);
        }

        private string GetLastLinesJoined(int maxLines)
        {
            if (_lines.Count == 0)
                return string.Empty;

            maxLines = Mathf.Clamp(maxLines, 10, 400);
            var start = Mathf.Max(0, _lines.Count - maxLines);

            var sb = new StringBuilder(maxLines * 128);
            for (int i = start; i < _lines.Count; i++)
                sb.AppendLine(_lines[i]);

            return sb.ToString();
        }

        // ---------------------------
        // Reflection publish (EventBus)
        // ---------------------------

        private bool TryPublishEventByName(string eventTypeName, out string error)
        {
            error = null;

            var evtType = FindTypeByName(eventTypeName);
            if (evtType == null)
            {
                error = $"Tipo de evento '{eventTypeName}' não encontrado.";
                return false;
            }

            if (!TryCreateEventInstance(evtType, eventTypeName, out var evt, out error))
                return false;

            return TryPublish(evt, out error);
        }

        private bool TryPublishGameRunEnded(string outcomeName, string reason, out string error)
        {
            error = null;

            var evtType = FindTypeByName("GameRunEndedEvent");
            if (evtType == null)
            {
                error = "Tipo 'GameRunEndedEvent' não encontrado.";
                return false;
            }

            if (!TryCreateEventInstance(evtType, "GameRunEndedEvent", out var evt, out error))
                return false;

            SetMember(evt, "Outcome", outcomeName);
            SetMember(evt, "Reason", reason);
            SetMember(evt, "reason", reason);

            return TryPublish(evt, out error);
        }

        private bool TryPublish(object evt, out string error)
        {
            error = null;
            if (evt == null)
            {
                error = "Evento null.";
                return false;
            }

            var evtType = evt.GetType();

            var genericBusDef = FindTypeByFullNameContains("EventBus`1");
            if (genericBusDef != null && genericBusDef.IsGenericTypeDefinition)
            {
                try
                {
                    var closedBus = genericBusDef.MakeGenericType(evtType);
                    var methods = closedBus.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                    foreach (var name in new[] { "Publish", "Raise", "Emit", "Fire", "Send" })
                    {
                        var mi = methods.FirstOrDefault(m =>
                            m.Name == name &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == evtType);

                        if (mi != null)
                        {
                            mi.Invoke(null, new[] { evt });
                            return true;
                        }
                    }
                }
                catch { }
            }

            error = $"Não foi possível localizar método de publish no EventBus para '{evtType.Name}'.";
            return false;
        }

        // ---------------------------
        // Event instantiation (robust)
        // ---------------------------

        private bool TryCreateEventInstance(Type evtType, string label, out object evt, out string error)
        {
            evt = null;
            error = null;

            if (evtType == null)
            {
                error = "evtType null.";
                return false;
            }

            // 1) Try default ctor
            try
            {
                evt = Activator.CreateInstance(evtType);
                if (evt != null) return true;
            }
            catch (Exception ex)
            {
                // Continue with fallback strategies.
                error = ex.GetBaseException().Message;
            }

            // 2) Try best-effort constructor invocation (public/nonpublic), smallest param count first.
            try
            {
                var ctors = evtType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .OrderBy(c => c.GetParameters().Length)
                    .ToArray();

                foreach (var ctor in ctors)
                {
                    var ps = ctor.GetParameters();
                    var args = new object[ps.Length];
                    var ok = true;

                    for (int i = 0; i < ps.Length; i++)
                    {
                        if (!TryBuildDefaultArg(ps[i], out args[i]))
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (!ok) continue;

                    try
                    {
                        evt = ctor.Invoke(args);
                        if (evt != null) return true;
                    }
                    catch
                    {
                        // try next ctor
                    }
                }
            }
            catch
            {
                // ignore and go to last resort
            }

#if UNITY_EDITOR
            // 3) Editor-only last resort: create uninitialized object (bypasses ctor).
            // This is acceptable for EventBus "command" events that are used as signals and don't require invariants.
            try
            {
                evt = FormatterServices.GetUninitializedObject(evtType);
                if (evt != null) return true;
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
            }
#endif

            error = $"Não foi possível instanciar '{label}' (sem default ctor e sem construtor compatível). Último erro: {error}";
            return false;
        }

        private bool TryBuildDefaultArg(ParameterInfo p, out object value)
        {
            value = null;
            if (p == null) return false;

            var t = p.ParameterType;

            // Optional parameter: use default if available
            if (p.HasDefaultValue)
            {
                value = p.DefaultValue;
                return true;
            }

            // Common useful defaults
            if (t == typeof(string))
            {
                // Provide a stable non-null reason where strings are required.
                value = "baseline_smoke";
                return true;
            }

            // Value types => default(T)
            if (t.IsValueType)
            {
                try
                {
                    value = Activator.CreateInstance(t);
                    return true;
                }
                catch
                {
                    value = null;
                    return false;
                }
            }

            // Reference types => null is acceptable as generic best-effort
            value = null;
            return true;
        }

        private bool SetMember(object target, string memberName, object value)
        {
            if (target == null) return false;
            var t = target.GetType();

            var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                try
                {
                    var converted = ConvertValue(value, p.PropertyType);
                    p.SetValue(target, converted);
                    return true;
                }
                catch { }
            }

            var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                try
                {
                    var converted = ConvertValue(value, f.FieldType);
                    f.SetValue(target, converted);
                    return true;
                }
                catch { }
            }

            return false;
        }

        private object ConvertValue(object value, Type dstType)
        {
            if (value == null) return null;

            if (dstType.IsInstanceOfType(value))
                return value;

            if (dstType.IsEnum && value is string s)
            {
                try { return Enum.Parse(dstType, s, true); }
                catch { return Enum.GetValues(dstType).GetValue(0); }
            }

            if (dstType == typeof(string))
                return value.ToString();

            try { return Convert.ChangeType(value, dstType); }
            catch { return value; }
        }

        private static Type FindTypeByName(string simpleName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t != null && t.Name == simpleName)
                        return t;
                }
            }

            return null;
        }

        private static Type FindTypeByFullNameContains(string pattern)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t != null && (t.FullName ?? string.Empty).Contains(pattern, StringComparison.Ordinal))
                        return t;
                }
            }

            return null;
        }

        // ---------------------------
        // Search utilities
        // ---------------------------

        private bool Contains(string needle)
        {
            if (string.IsNullOrEmpty(needle))
                return false;

            const int tail = 4000;
            var start = Mathf.Max(0, _lines.Count - tail);

            for (int i = _lines.Count - 1; i >= start; i--)
            {
                if (_lines[i].IndexOf(needle, StringComparison.Ordinal) >= 0)
                    return true;
            }

            return false;
        }

        private int IndexOfFrom(string needle, int fromLine)
        {
            if (string.IsNullOrEmpty(needle))
                return -1;

            fromLine = Mathf.Clamp(fromLine, 0, _lines.Count);

            for (int i = fromLine; i < _lines.Count; i++)
            {
                if (_lines[i].IndexOf(needle, StringComparison.Ordinal) >= 0)
                    return i;
            }

            return -1;
        }

        private int CountContains(string needle)
        {
            if (string.IsNullOrEmpty(needle))
                return 0;

            var c = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].IndexOf(needle, StringComparison.Ordinal) >= 0)
                    c++;
            }

            return c;
        }

        private readonly struct SmokeStep
        {
            public string Name { get; }
            private Func<IEnumerator> RunFunc { get; }

            public SmokeStep(string name, Func<IEnumerator> runFunc)
            {
                Name = name;
                RunFunc = runFunc;
            }

            public IEnumerator Run() => RunFunc();
        }
    }
}
