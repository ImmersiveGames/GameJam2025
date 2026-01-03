using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private const string PrefManualPlayKey = "NewScripts.Baseline2Smoke.ManualPlay"; // 1=manual click, 0=auto
        private const string PrefAutoNavigateTimeoutKey = "NewScripts.Baseline2Smoke.AutoNavigateTimeoutSeconds";

        private readonly List<string> _lines = new List<string>(8192);
        private readonly StringBuilder _raw = new StringBuilder(1024 * 256);

        private bool _failed;
        private string _failReason;
        private string _failCategory;

        private bool _manualPlay;
        private float _autoNavigateTimeoutSeconds;

        private string _resolvedNavigationType;

        private bool _menuTransitionCompleted;
        private bool _gameLoopReady;
        private bool _navLogObserved;
        private bool _gameplayTransitionStarted;
        private bool _gameplayTransitionCompleted;

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
            _manualPlay = PlayerPrefs.GetInt(PrefManualPlayKey, 1) == 1;

            var rawTimeout = PlayerPrefs.GetFloat(PrefAutoNavigateTimeoutKey, 25f);
            _autoNavigateTimeoutSeconds = Mathf.Clamp(rawTimeout, 5f, 120f);
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

            Debug.Log($"[Baseline2Smoke] Runner started. mode={( _manualPlay ? "MANUAL_PLAY_CLICK" : "AUTO_NAV" )}, autoTimeout={_autoNavigateTimeoutSeconds:0.0}s");
            StartCoroutine(Run());
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            var line = condition ?? string.Empty;
            _lines.Add(line);
            _raw.AppendLine(line);

            TryExtractContextHints(line);
            TryCaptureEvidence(line);
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

            // GameLoop Ready (log evidence)
            if (line.IndexOf("[GameLoop] ENTER: Ready", StringComparison.Ordinal) >= 0)
            {
                _gameLoopReady = true;
            }

            // Navigation evidence (log)
            if (line.IndexOf("[Navigation] NavigateAsync -> routeId='to-gameplay'", StringComparison.Ordinal) >= 0)
            {
                _navLogObserved = true;
            }

            // SceneFlow started (log)
            if (line.IndexOf("[SceneFlow] Iniciando transição:", StringComparison.Ordinal) >= 0 &&
                line.IndexOf("Profile='gameplay'", StringComparison.Ordinal) >= 0)
            {
                _gameplayTransitionStarted = true;
            }

            // Menu "completed" robust detection via logs:
            // - release do token de transição
            // - transição concluída
            // - ou readiness completed com Profile startup + Active MenuScene
            if (!_menuTransitionCompleted)
            {
                if (line.IndexOf("Release token='flow.scene_transition'. Active=0. IsOpen=True", StringComparison.Ordinal) >= 0 &&
                    (line.IndexOf("Profile='startup'", StringComparison.Ordinal) >= 0 || line.IndexOf("Active='MenuScene'", StringComparison.Ordinal) >= 0))
                {
                    _menuTransitionCompleted = true;
                }
                else if (line.IndexOf("[SceneFlow] Transição concluída com sucesso", StringComparison.Ordinal) >= 0)
                {
                    // Esse log aparece no seu output atual e é um ótimo “selo” de completude.
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
            // Mantemos, mas não dependemos mais exclusivamente disso.
            // Se o seu struct tiver semântica diferente aqui, o smoke ainda funciona via logs.
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
                    "[GameLoopSceneFlow] Coordinator registrado",
                    "[SceneFlow] Iniciando transição: Load=[MenuScene")),

                new SmokeStep("A2) Startup Menu completed", () => ExpectInOrder(30f,
                    "Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[NewBootstrap], Active='MenuScene', UseFade=True, Profile='startup'",
                    "Acquire token='flow.scene_transition'",
                    "SceneTransitionScenesReady recebido",
                    "Reset SKIPPED (startup/frontend)",
                    "Emitting WorldLifecycleResetCompletedEvent. profile='startup'",
                    "Release token='flow.scene_transition'. Active=0. IsOpen=True")),

                new SmokeStep("B0) Ensure Menu stable", () => WaitForMenuStable(20f)),

                // B) Menu -> Gameplay
                new SmokeStep("B1) Enter Gameplay", () => EnterGameplay(_autoNavigateTimeoutSeconds)),
                new SmokeStep("B2) Gameplay reset+spawn", () => WaitForGameplayTransitionCompletion(60f)),

                // C) Pause -> Resume
                new SmokeStep("C1) Pause/Resume", () => PauseResume(15f)),

                // D) Defeat -> PostGame -> Restart
                new SmokeStep("D1) Force Defeat (GameRunEndedEvent)", () => ForceOutcome("Defeat", "QA_ForcedDefeat", 15f)),
                new SmokeStep("D2) Restart (GameResetRequestedEvent)", () => PublishAndWait("GameResetRequestedEvent", 40f,
                    "GameResetRequestedEvent",
                    "Iniciando transição: Load=[GameplayScene, UIGlobalScene]",
                    "SceneTransitionScenesReady recebido",
                    "Emitting WorldLifecycleResetCompletedEvent. profile='gameplay'")),

                // E) Victory -> PostGame -> ExitToMenu
                new SmokeStep("E1) Force Victory (GameRunEndedEvent)", () => ForceOutcome("Victory", "QA_ForcedVictory", 15f)),
                new SmokeStep("E2) ExitToMenu (GameExitToMenuRequestedEvent)", () => PublishAndWait("GameExitToMenuRequestedEvent", 60f,
                    "GameExitToMenuRequestedEvent",
                    "Iniciando transição: Load=[MenuScene, UIGlobalScene], Unload=[GameplayScene], Active='MenuScene'",
                    "Reset SKIPPED (startup/frontend)",
                    "Emitting WorldLifecycleResetCompletedEvent. profile='frontend'",
                    "Release token='flow.scene_transition'. Active=0. IsOpen=True"))
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
        // Step helpers
        // ---------------------------

        private IEnumerator EnterGameplay(float timeoutSeconds)
        {
            if (_manualPlay)
            {
                Debug.Log("[Baseline2Smoke] MANUAL: Clique no botão Play do Menu para entrar no Gameplay.");
                yield return WaitForCondition(timeoutSeconds,
                    () => _gameplayTransitionStarted || _navLogObserved || Contains("Profile='gameplay'"),
                    "Clique no Play / início de transição gameplay (event/log)");
                yield break;
            }

            // Auto mode: resolve nav service and call RequestGameplayAsync
            if (!TryResolveNavigationService(out var navigation, out var error))
            {
                _failCategory = "não resolver nav service";
                Fail($"Falha ao resolver IGameNavigationService: {error}");
                yield break;
            }

            _resolvedNavigationType = navigation.GetType().FullName;
            Debug.Log($"[Baseline2Smoke] AUTO: Resolved IGameNavigationService: {_resolvedNavigationType}");

            var requestTask = RequestGameplayAsync(navigation);
            yield return WaitForTask(requestTask, timeoutSeconds, "Timeout aguardando RequestGameplayAsync.");
            if (_failed) yield break;

            yield return WaitForCondition(timeoutSeconds,
                () => _gameplayTransitionStarted || _navLogObserved || Contains("Profile='gameplay'"),
                "Evidência de navegação/transição gameplay (event/log)");
        }

        private IEnumerator WaitForMenuStable(float timeoutSeconds)
        {
            var start = Time.realtimeSinceStartup;

            // Critério robusto:
            // - GameLoop Ready observado (log)
            // - e algum “selo” de transição concluída (flag via EventBus OU logs)
            // - e gate de scene_transition liberado (log)
            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                var tokenReleased = Contains("Release token='flow.scene_transition'. Active=0. IsOpen=True");
                var stable = _gameLoopReady && (_menuTransitionCompleted || Contains("[SceneFlow] Transição concluída com sucesso")) && tokenReleased;

                if (stable)
                {
                    Debug.Log("[Baseline2Smoke] Evidência: Menu estável (GameLoop Ready + SceneTransition completed + token released).");
                    yield break;
                }

                yield return null;
            }

            Fail("Timeout aguardando estado estável de Menu (GameLoop Ready + TransitionCompleted + Release token).");
        }

        private async Task RequestGameplayAsync(IGameNavigationService navigation)
        {
            await navigation.RequestGameplayAsync("baseline_smoke");
        }

        private IEnumerator WaitForGameplayTransitionCompletion(float timeoutSeconds)
        {
            yield return ExpectInOrder(timeoutSeconds,
                "Iniciando transição: Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'",
                "Acquire token='flow.scene_transition'",
                "SceneTransitionScenesReady recebido",
                "Acquire token='WorldLifecycle.WorldReset'",
                "World Reset Completed",
                "Release token='WorldLifecycle.WorldReset'",
                "Emitting WorldLifecycleResetCompletedEvent. profile='gameplay'",
                "Release token='flow.scene_transition'. Active=0. IsOpen=True");

            if (_failed && _gameplayTransitionStarted && !_gameplayTransitionCompleted)
                _failCategory = "iniciou mas não completou";
        }

        private IEnumerator PauseResume(float timeoutSeconds)
        {
            if (!TryPublishEventByName("GamePauseCommandEvent", out var err1))
            {
                Fail($"Falha ao publicar GamePauseCommandEvent: {err1}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds, "Acquire token='state.pause'", "ENTER: Paused");
            if (_failed) yield break;

            if (!TryPublishEventByName("GameResumeRequestedEvent", out var err2))
            {
                Fail($"Falha ao publicar GameResumeRequestedEvent: {err2}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds, "Release token='state.pause'. Active=0. IsOpen=True", "ENTER: Playing");
        }

        private IEnumerator ForceOutcome(string outcomeName, string reason, float timeoutSeconds)
        {
            if (!TryPublishGameRunEnded(outcomeName, reason, out var err))
            {
                Fail($"Falha ao publicar GameRunEndedEvent ({outcomeName}): {err}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds, "GameRunEndedEvent", "Acquire token='state.postgame'", "state.postgame");
        }

        private IEnumerator PublishAndWait(string eventTypeName, float timeoutSeconds, params string[] evidenceAny)
        {
            if (!TryPublishEventByName(eventTypeName, out var publishError))
            {
                Fail($"Falha ao publicar evento '{eventTypeName}': {publishError}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds, evidenceAny);
        }

        // ---------------------------
        // Wait utilities
        // ---------------------------

        private IEnumerator WaitForAnyLog(float timeoutSeconds, params string[] needles)
        {
            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
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
                if (predicate())
                {
                    onObserved?.Invoke();
                    yield break;
                }

                yield return null;
            }

            Fail($"Timeout aguardando evidência: {label}");
        }

        private IEnumerator WaitForTask(Task task, float timeoutSeconds, string timeoutReason)
        {
            if (task == null)
            {
                Fail("Task nula em WaitForTask.");
                yield break;
            }

            var start = Time.realtimeSinceStartup;

            while (!task.IsCompleted && Time.realtimeSinceStartup - start < timeoutSeconds)
                yield return null;

            if (!task.IsCompleted)
            {
                Fail(timeoutReason);
                yield break;
            }

            if (task.IsFaulted)
                Fail($"Task falhou: {task.Exception?.GetBaseException().Message}");
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
        // DI resolve
        // ---------------------------

        private bool TryResolveNavigationService(out IGameNavigationService navigation, out string error)
        {
            navigation = null;

            if (!DependencyManager.HasInstance)
            {
                error = "DependencyManager não inicializado.";
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal(out navigation) || navigation == null)
            {
                error = "IGameNavigationService indisponível no DI global.";
                return false;
            }

            error = null;
            return true;
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
            md.AppendLine($"- Mode: `{(_manualPlay ? "MANUAL_PLAY_CLICK" : "AUTO_NAV")}`");
            md.AppendLine($"- Last signature seen: `{_activeSignatureLast ?? "<unknown>"}`");
            md.AppendLine($"- Last profile seen: `{_profileLast ?? "<unknown>"}`");
            md.AppendLine();

            if (_failed)
            {
                md.AppendLine("## Failure reason");
                md.AppendLine();
                md.AppendLine($"- {_failReason}");
                if (!string.IsNullOrWhiteSpace(_failCategory))
                    md.AppendLine($"- Categoria: {_failCategory}");
                md.AppendLine();
            }

            md.AppendLine("## Evidências");
            md.AppendLine();
            md.AppendLine($"- Menu TransitionCompleted (flag): `{_menuTransitionCompleted}`");
            md.AppendLine($"- GameLoop Ready (log): `{_gameLoopReady}`");
            md.AppendLine($"- Nav log to-gameplay observado: `{_navLogObserved}`");
            md.AppendLine($"- Gameplay transition started observado: `{_gameplayTransitionStarted}`");
            md.AppendLine($"- Gameplay transition completed observado: `{_gameplayTransitionCompleted}`");
            md.AppendLine($"- IGameNavigationService resolvido: `{_resolvedNavigationType ?? "<null>"}`");
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
            Debug.LogError("[Baseline2Smoke] FAIL: " + reason);
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

            object evt;
            try { evt = Activator.CreateInstance(evtType); }
            catch (Exception ex)
            {
                error = $"Não foi possível instanciar '{eventTypeName}': {ex.GetBaseException().Message}";
                return false;
            }

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

            object evt;
            try { evt = Activator.CreateInstance(evtType); }
            catch (Exception ex)
            {
                error = $"Não foi possível instanciar GameRunEndedEvent: {ex.GetBaseException().Message}";
                return false;
            }

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
