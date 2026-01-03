using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.QA.Baseline
{
    public sealed class Baseline2SmokeRunner : MonoBehaviour
    {
        private const string RunKey = "NewScripts.Baseline2Smoke.RunRequested";

        private readonly List<string> _lines = new List<string>(8192);
        private readonly StringBuilder _raw = new StringBuilder(1024 * 256);

        private bool _failed;
        private string _failReason;

        private float _startRealtime;
        private string _activeSignatureLast;
        private string _profileLast;

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

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void Start()
        {
            _startRealtime = Time.realtimeSinceStartup;
            StartCoroutine(Run());
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            var line = condition ?? string.Empty;
            _lines.Add(line);
            _raw.AppendLine(line);

            TryExtractContextHints(line);
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

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(0.25f);

            var steps = new List<SmokeStep>
            {
                // A) Boot -> Menu (startup)
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

                // B) Menu -> Gameplay (gameplay)
                new SmokeStep("B1) Navigate to Gameplay (via production bridge)", () => NavigateToGameplayViaBridge(20f)),
                new SmokeStep("B2) Gameplay reset+spawn", () => ExpectInOrder(50f,
                    "Iniciando transição: Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'",
                    "Acquire token='flow.scene_transition'",
                    "SceneTransitionScenesReady recebido",
                    "Acquire token='WorldLifecycle.WorldReset'",
                    "World Reset Completed",
                    "Release token='WorldLifecycle.WorldReset'",
                    "Emitting WorldLifecycleResetCompletedEvent. profile='gameplay'",
                    "Release token='flow.scene_transition'. Active=0. IsOpen=True")),

                // C) Pause -> Resume
                new SmokeStep("C1) Pause/Resume", () => PauseResume(15f)),

                // D) Defeat -> PostGame -> Restart
                new SmokeStep("D1) Force Defeat (GameRunEndedEvent)", () => ForceOutcome("Defeat", "QA_ForcedDefeat", 15f)),
                new SmokeStep("D2) Restart (GameResetRequestedEvent)", () => PublishAndWait("GameResetRequestedEvent", 30f,
                    "GameResetRequestedEvent",
                    "Iniciando transição: Load=[GameplayScene, UIGlobalScene]",
                    "SceneTransitionScenesReady recebido",
                    "Emitting WorldLifecycleResetCompletedEvent. profile='gameplay'")),

                // E) Victory -> PostGame -> ExitToMenu
                new SmokeStep("E1) Force Victory (GameRunEndedEvent)", () => ForceOutcome("Victory", "QA_ForcedVictory", 15f)),
                new SmokeStep("E2) ExitToMenu (GameExitToMenuRequestedEvent)", () => PublishAndWait("GameExitToMenuRequestedEvent", 40f,
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
            EditorApplication.isPlaying = false;
#endif
        }

        // ---------------------------
        // Steps
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

        private IEnumerator PublishAndWait(string eventTypeName, float timeoutSeconds, params string[] evidenceAny)
        {
            if (!TryPublishEventByName(eventTypeName, out var publishError))
            {
                Fail($"Falha ao publicar evento '{eventTypeName}': {publishError}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds, evidenceAny);
        }

        private IEnumerator PauseResume(float timeoutSeconds)
        {
            if (!TryPublishEventByName("GamePauseCommandEvent", out var err1))
            {
                Fail($"Falha ao publicar GamePauseCommandEvent: {err1}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds,
                "Acquire token='state.pause'",
                "ENTER: Paused");

            if (_failed) yield break;

            if (!TryPublishEventByName("GameResumeRequestedEvent", out var err2))
            {
                Fail($"Falha ao publicar GameResumeRequestedEvent: {err2}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds,
                "Release token='state.pause'. Active=0. IsOpen=True",
                "ENTER: Playing");
        }

        /// <summary>
        /// Navegação Menu -> Gameplay sem reflection em DI/registry:
        /// usa o bridge de produção RestartNavigationBridge (GameResetRequestedEvent -> RequestGameplayAsync).
        /// </summary>
        private IEnumerator NavigateToGameplayViaBridge(float timeoutSeconds)
        {
            // Em Menu, "reset" é um gatilho válido para ir ao gameplay pela rota de produção.
            if (!TryPublishEventByName("GameResetRequestedEvent", out var err))
            {
                Fail($"Falha ao publicar GameResetRequestedEvent (para navegar ao gameplay): {err}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds,
                "Iniciando transição: Load=[GameplayScene, UIGlobalScene]",
                "Profile='gameplay'");
        }

        private IEnumerator ForceOutcome(string outcomeName, string reason, float timeoutSeconds)
        {
            if (!TryPublishGameRunEnded(outcomeName, reason, out var err))
            {
                Fail($"Falha ao publicar GameRunEndedEvent ({outcomeName}): {err}");
                yield break;
            }

            yield return WaitForAnyLog(timeoutSeconds,
                "GameRunEndedEvent",
                "Acquire token='state.postgame'",
                "state.postgame");
        }

        // ---------------------------
        // Validation/report
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
            md.AppendLine($"- Last signature seen: `{_activeSignatureLast ?? "<unknown>"}`");
            md.AppendLine($"- Last profile seen: `{_profileLast ?? "<unknown>"}`");
            md.AppendLine();

            if (_failed)
            {
                md.AppendLine("## Failure reason");
                md.AppendLine();
                md.AppendLine($"- {_failReason}");
                md.AppendLine();
            }

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
            AssetDatabase.Refresh();
#endif

            Debug.Log($"[Baseline2Smoke] Report written: {mdPath}");
            Debug.Log($"[Baseline2Smoke] Raw log written: {logPath}");
        }

        // ---------------------------
        // Reflection: EventBus publishing
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
            try
            {
                evt = Activator.CreateInstance(evtType);
            }
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
            try
            {
                evt = Activator.CreateInstance(evtType);
            }
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

        private bool TryPublish(object evt, out string error)
        {
            error = null;
            if (evt == null)
            {
                error = "Evento null.";
                return false;
            }

            var evtType = evt.GetType();

            foreach (var busType in FindTypesBySimpleName("EventBus"))
            {
                var methods = busType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var name in new[] { "Publish", "Raise", "Emit", "Fire", "Send" })
                {
                    foreach (var mi in methods.Where(m => m.Name == name))
                    {
                        try
                        {
                            if (mi.IsGenericMethodDefinition &&
                                mi.GetGenericArguments().Length == 1 &&
                                mi.GetParameters().Length == 1)
                            {
                                var closed = mi.MakeGenericMethod(evtType);
                                closed.Invoke(null, new[] { evt });
                                return true;
                            }

                            if (!mi.IsGenericMethodDefinition && mi.GetParameters().Length == 1)
                            {
                                var p = mi.GetParameters()[0].ParameterType;
                                if (p.IsAssignableFrom(evtType))
                                {
                                    mi.Invoke(null, new[] { evt });
                                    return true;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }

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

        private static IEnumerable<Type> FindTypesBySimpleName(string simpleName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t != null && t.Name == simpleName)
                        yield return t;
                }
            }
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
        // Searching utilities
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

        private void Fail(string reason)
        {
            if (_failed) return;
            _failed = true;
            _failReason = reason;
            Debug.LogError("[Baseline2Smoke] FAIL: " + reason);
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
