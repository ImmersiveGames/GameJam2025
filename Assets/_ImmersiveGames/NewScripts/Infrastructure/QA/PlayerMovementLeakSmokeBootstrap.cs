using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _ImmersiveGames.NewScripts.Gameplay.Player.Movement;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Smoke test em PlayMode para detectar vazamento de movimento/input do Player (Rigidbody)
    /// ao fechar/reabrir o gate e após reset do WorldLifecycle. Executa sem NUnit/TestRunner.
    /// </summary>
    public sealed class PlayerMovementLeakSmokeBootstrap : MonoBehaviour
    {
        private const string LogTag = "[PlayerMoveTest][Leak]";
        private const string ReportPath = "Assets/_ImmersiveGames/NewScripts/Docs/Reports/PlayerMovement-Leak.md";
        private const float TimeoutSeconds = 30f;
        private const float MovementTolerance = 0.05f;
        private const int InputFrames = 15;
        private const int DriftFrames = 10;
        private const string GateToken = "qa.player_move_leak";

        private readonly List<string> _taggedLogs = new();
        private int _capturedLogCount;
        private bool _hasFailMarker;
        private string _sceneName = string.Empty;
        private string _resetApiUsed = string.Empty;
        private bool? _testAResult;
        private bool? _testBResult;
        private bool? _testCResult;
        private string _testAFailure = string.Empty;
        private string _testBFailure = string.Empty;
        private string _testCFailure = string.Empty;
        private bool _timeout;
        private bool _inconclusive;
        private bool _autoSpawned;
        private float _initialSpeed;
        private float _speedAfterGate;
        private float _speedAfterReset;
        private float _speedAfterReopen;
        private float _driftAfterGate;
        private float _driftAfterReset;
        private float _driftAfterReopen;
        private bool _initialGateOpen;
        private string _discoveryPath = string.Empty;

        private static bool _created;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (_created)
            {
                return;
            }

            if (!Application.isPlaying && !Application.isBatchMode)
            {
                return;
            }

            var go = new GameObject(nameof(PlayerMovementLeakSmokeBootstrap))
            {
                hideFlags = HideFlags.DontSave
            };

            DontDestroyOnLoad(go);
            go.AddComponent<PlayerMovementLeakSmokeBootstrap>()._autoSpawned = true;
            _created = true;
        }

        private void Start()
        {
            StartCoroutine(RunSmoke());
        }

        private System.Collections.IEnumerator RunSmoke()
        {
            float startTime = Time.realtimeSinceStartup;
            _sceneName = gameObject.scene.name;

            Application.logMessageReceived += OnLogMessage;

            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} Runner iniciado (scene='{_sceneName}', autoSpawned={_autoSpawned}).");

            yield return null; // permite spawn/boot inicial concluir

            var provider = DependencyManager.Provider;
            if (!provider.TryGetGlobal<ISimulationGateService>(out var gateService) || gateService == null)
            {
                MarkInconclusive("ISimulationGateService ausente no escopo global.");
                FinalizeAndExit(startTime, "INCONCLUSIVE");
                yield break;
            }

            _initialGateOpen = gateService.IsOpen;

            var player = DiscoverPlayer(provider);
            if (!ValidatePlayer(player, "Player não encontrado (spawn serviço ou fallback).", startTime))
            {
                yield break;
            }

            _initialSpeed = HorizontalSpeed(player.Rigidbody);

            yield return RunTestA(player, gateService, startTime);
            if (StopEarly(startTime))
            {
                yield break;
            }

            yield return RunTestB(player, provider, startTime);
            if (StopEarly(startTime))
            {
                yield break;
            }

            player = DiscoverPlayer(provider);
            if (!ValidatePlayer(player, "Player não encontrado após reset.", startTime))
            {
                yield break;
            }

            yield return RunTestC(player, gateService, startTime);

            string result = ComputeFinalResult();
            FinalizeAndExit(startTime, result);
        }

        private bool StopEarly(float startTime)
        {
            if (HasTimedOut(startTime))
            {
                FinalizeAndExit(startTime, "FAIL");
                return true;
            }

            if (_inconclusive)
            {
                FinalizeAndExit(startTime, "INCONCLUSIVE");
                return true;
            }

            return false;
        }

        private System.Collections.IEnumerator RunTestA(PlayerContext player, ISimulationGateService gateService, float startTime)
        {
            if (HasTimedOut(startTime))
            {
                MarkTimeout();
                yield break;
            }

            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} Teste A iniciado - gate deve bloquear movimento.");

            yield return ApplyInputFrames(player.Controller, new Vector2(0f, 1f), InputFrames, startTime);
            Vector3 positionBeforeClose = player.Rigidbody.position;

            IDisposable gateHandle = null;
            try
            {
                gateHandle = gateService.Acquire(GateToken);
            }
            catch (Exception ex)
            {
                _testAResult = false;
                _testAFailure = $"Exception ao adquirir gate: {ex.Message}";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste A FAIL - {_testAFailure}");
                yield break;
            }

            yield return ApplyInputFrames(player.Controller, new Vector2(0f, 1f), InputFrames, startTime);

            _speedAfterGate = HorizontalSpeed(player.Rigidbody);
            _driftAfterGate = HorizontalDistance(positionBeforeClose, player.Rigidbody.position);

            gateHandle?.Dispose();

            bool pass = _speedAfterGate <= MovementTolerance && _driftAfterGate <= MovementTolerance;
            _testAResult = pass;
            if (!pass)
            {
                _testAFailure = $"Vel/Drift após gate: speed={_speedAfterGate:F3}, drift={_driftAfterGate:F3}";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste A FAIL - {_testAFailure}");
            }
            else
            {
                DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste A PASS - speed={_speedAfterGate:F3}, drift={_driftAfterGate:F3}");
            }
        }

        private System.Collections.IEnumerator RunTestB(PlayerContext currentPlayer, IDependencyProvider provider, float startTime)
        {
            if (HasTimedOut(startTime))
            {
                MarkTimeout();
                yield break;
            }

            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} Teste B iniciado - reset deve limpar física.");

            yield return ApplyInputFrames(currentPlayer.Controller, new Vector2(0f, 1f), InputFrames, startTime);
            Vector3 positionPreReset = currentPlayer.Rigidbody.position;
            float speedPreReset = HorizontalSpeed(currentPlayer.Rigidbody);

            bool resetTriggered = TryTriggerReset(out var resetCoroutine, out string apiLabel);
            _resetApiUsed = apiLabel;

            if (!resetTriggered)
            {
                _testBResult = null;
                _testBFailure = "Nenhuma API de reset disponível.";
                _inconclusive = true;
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste B INCONCLUSIVE - {_testBFailure}");
                yield break;
            }

            if (resetCoroutine != null)
            {
                yield return resetCoroutine;
            }

            PlayerContext newPlayerContext = default;
            bool playerFound = false;
            yield return WaitForRespawn(currentPlayer, provider, startTime, context =>
            {
                newPlayerContext = context;
                playerFound = true;
            });

            if (!playerFound || newPlayerContext.Rigidbody == null)
            {
                _testBResult = false;
                _testBFailure = "Player não reapareceu após reset.";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste B FAIL - {_testBFailure}");
                yield break;
            }

            newPlayerContext.Controller?.QA_ClearInputs();

            _speedAfterReset = HorizontalSpeed(newPlayerContext.Rigidbody);
            _driftAfterReset = 0f;

            yield return CaptureDrift(newPlayerContext.Rigidbody, DriftFrames, startTime, drift => _driftAfterReset = drift);

            bool pass = _speedAfterReset <= MovementTolerance && _driftAfterReset <= MovementTolerance;
            _testBResult = pass;
            if (!pass)
            {
                _testBFailure = $"Vel/Drift pós-reset: speed={_speedAfterReset:F3}, drift={_driftAfterReset:F3}";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste B FAIL - {_testBFailure}");
            }
            else
            {
                DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste B PASS - speed={_speedAfterReset:F3}, drift={_driftAfterReset:F3}, preResetSpeed={speedPreReset:F3}, preResetDelta={HorizontalDistance(positionPreReset, currentPlayer.Rigidbody.position):F3}");
            }
        }

        private System.Collections.IEnumerator RunTestC(PlayerContext player, ISimulationGateService gateService, float startTime)
        {
            if (HasTimedOut(startTime))
            {
                MarkTimeout();
                yield break;
            }

            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} Teste C iniciado - reabertura do gate não deve gerar input fantasma.");

            IDisposable gateHandle = null;
            try
            {
                gateHandle = gateService.Acquire(GateToken);
            }
            catch (Exception ex)
            {
                _testCResult = false;
                _testCFailure = $"Exception ao adquirir gate: {ex.Message}";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste C FAIL - {_testCFailure}");
                yield break;
            }

            yield return ApplyInputFrames(player.Controller, new Vector2(0f, 1f), InputFrames, startTime);

            gateHandle?.Dispose();
            player.Controller?.QA_ClearInputs();

            _speedAfterReopen = HorizontalSpeed(player.Rigidbody);
            _driftAfterReopen = 0f;

            yield return CaptureDrift(player.Rigidbody, DriftFrames, startTime, drift => _driftAfterReopen = drift);

            bool pass = _speedAfterReopen <= MovementTolerance && _driftAfterReopen <= MovementTolerance;
            _testCResult = pass;
            if (!pass)
            {
                _testCFailure = $"Vel/Drift após reabrir: speed={_speedAfterReopen:F3}, drift={_driftAfterReopen:F3}";
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste C FAIL - {_testCFailure}");
            }
            else
            {
                DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Teste C PASS - speed={_speedAfterReopen:F3}, drift={_driftAfterReopen:F3}");
            }
        }

        private System.Collections.IEnumerator ApplyInputFrames(NewPlayerMovementController controller, Vector2 input, int frames, float startTime)
        {
            if (controller == null)
            {
                MarkInconclusive("Controller de movimento não encontrado.");
                yield break;
            }

            for (int i = 0; i < frames; i++)
            {
                if (HasTimedOut(startTime))
                {
                    MarkTimeout();
                    yield break;
                }

                controller.QA_SetMoveInput(input);
                yield return new WaitForFixedUpdate();
            }
        }

        private System.Collections.IEnumerator CaptureDrift(Rigidbody rb, int frames, float startTime, Action<float> onDrift)
        {
            if (rb == null)
            {
                yield break;
            }

            Vector3 start = rb.position;
            for (int i = 0; i < frames; i++)
            {
                if (HasTimedOut(startTime))
                {
                    MarkTimeout();
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            onDrift?.Invoke(HorizontalDistance(start, rb.position));
        }

        private System.Collections.IEnumerator WaitForRespawn(PlayerContext oldPlayer, IDependencyProvider provider, float startTime, Action<PlayerContext> onFound)
        {
            float waitStart = Time.realtimeSinceStartup;
            while (!HasTimedOut(startTime) && Time.realtimeSinceStartup - waitStart < TimeoutSeconds)
            {
                var candidate = DiscoverPlayer(provider, suppressLog: true);
                if (candidate != null && !ReferenceEquals(candidate.GameObject, oldPlayer.GameObject) && candidate.Rigidbody != null)
                {
                    DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                        $"{LogTag} Player reapareceu após reset (path='{candidate.Path}').");
                    onFound?.Invoke(candidate);
                    yield break;
                }

                yield return null;
            }

            MarkTimeout();
        }

        private bool TryTriggerReset(out System.Collections.IEnumerator coroutine, out string apiLabel)
        {
            apiLabel = string.Empty;
            coroutine = null;

            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                var context = new SceneTransitionContext(new[] { scene }, Array.Empty<string>(), scene, false, "qa.player_move_leak_reset");
                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));
                apiLabel = "SceneTransitionScenesReadyEvent";

                DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Reset disparado via EventBus (SceneTransitionScenesReadyEvent). Context={context}");
                return true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Falha ao publicar SceneTransitionScenesReadyEvent: {ex.Message}");
            }

            var controller = UnityEngine.Object.FindFirstObjectByType<WorldLifecycleController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                return false;
            }

            apiLabel = "WorldLifecycleController.ResetWorldAsync";
            coroutine = AwaitResetTask(controller.ResetWorldAsync("PlayerMovementLeakSmoke"));
            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} Reset disparado via WorldLifecycleController.ResetWorldAsync.");
            return true;
        }

        private static System.Collections.IEnumerator AwaitResetTask(System.Threading.Tasks.Task task)
        {
            while (task != null && !task.IsCompleted)
            {
                yield return null;
            }
        }

        private PlayerContext DiscoverPlayer(IDependencyProvider provider, bool suppressLog = false)
        {
            if (TryFindSpawnedPlayer(provider, out var context))
            {
                _discoveryPath = context.Path;
                return context;
            }

            var actor = UnityEngine.Object.FindFirstObjectByType<PlayerActor>(FindObjectsInactive.Include);
            if (actor != null)
            {
                var rb = actor.GetComponentInChildren<Rigidbody>();
                var controller = actor.GetComponentInChildren<NewPlayerMovementController>();
                context = new PlayerContext(actor.gameObject, actor, controller, rb, "Fallback: PlayerActor na cena ativa");
                _discoveryPath = context.Path;
                return context;
            }

            var movement = UnityEngine.Object.FindFirstObjectByType<NewPlayerMovementController>(FindObjectsInactive.Include);
            if (movement != null)
            {
                var rb = movement.GetComponentInChildren<Rigidbody>();
                context = new PlayerContext(movement.gameObject, movement.GetComponent<PlayerActor>(), movement, rb, "Fallback: NewPlayerMovementController na cena ativa");
                _discoveryPath = context.Path;
                return context;
            }

            if (!suppressLog)
            {
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Player não encontrado no WorldRoot ou fallbacks.");
            }

            return null;
        }

        private bool ValidatePlayer(PlayerContext? player, string error, float startTime)
        {
            if (!player.HasValue || player.Value.Rigidbody == null || player.Value.Controller == null)
            {
                MarkInconclusive(error);
                FinalizeAndExit(startTime, "INCONCLUSIVE");
                return false;
            }

            return true;
        }

        private bool TryFindSpawnedPlayer(IDependencyProvider provider, out PlayerContext context)
        {
            context = default;
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (provider.TryGetForScene<IWorldSpawnContext>(sceneName, out var spawnContext) && spawnContext?.WorldRoot != null)
            {
                var actor = spawnContext.WorldRoot.GetComponentInChildren<PlayerActor>(true);
                var controller = spawnContext.WorldRoot.GetComponentInChildren<NewPlayerMovementController>(true);
                var rb = spawnContext.WorldRoot.GetComponentInChildren<Rigidbody>(true);

                var go = actor != null ? actor.gameObject : controller != null ? controller.gameObject : rb != null ? rb.gameObject : null;
                if (go != null)
                {
                    context = new PlayerContext(go, actor, controller, rb, $"WorldRoot:{BuildTransformPath(spawnContext.WorldRoot)}");
                    return true;
                }
            }

            return false;
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return;
            }

            if (condition.Contains("[PlayerMoveTest]", StringComparison.Ordinal))
            {
                _capturedLogCount++;
                _taggedLogs.Add(condition);

                if (condition.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    condition.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _hasFailMarker = true;
                }
            }
        }

        private void WriteReport(string result)
        {
            try
            {
                var dir = Path.GetDirectoryName(ReportPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var builder = new StringBuilder();
                builder.AppendLine("# Player Movement Leak Smoke Result");
                builder.AppendLine($"- Timestamp (UTC): {DateTime.UtcNow:O}");
                builder.AppendLine($"- Cena ativa: {_sceneName}");
                builder.AppendLine($"- Player encontrado via: {_discoveryPath}");
                builder.AppendLine($"- Reset API: {_resetApiUsed}");
                builder.AppendLine($"- Gate inicial aberto: {_initialGateOpen}");
                builder.AppendLine($"- Logs marcados capturados: {_capturedLogCount}");
                builder.AppendLine($"- Resultado final: {result}");
                builder.AppendLine();
                builder.AppendLine("## Métricas");
                builder.AppendLine($"- Teste A (Gate fecha): status={FormatStatus(_testAResult)} velInicial={_initialSpeed:F3} velApósGate={_speedAfterGate:F3} driftApósGate={_driftAfterGate:F3} detalhe={_testAFailure}");
                builder.AppendLine($"- Teste B (Reset limpa estado): status={FormatStatus(_testBResult)} velApósReset={_speedAfterReset:F3} driftApósReset={_driftAfterReset:F3} detalhe={_testBFailure}");
                builder.AppendLine($"- Teste C (Reabrir gate): status={FormatStatus(_testCResult)} velApósReabertura={_speedAfterReopen:F3} driftApósReabertura={_driftAfterReopen:F3} detalhe={_testCFailure}");
                builder.AppendLine();
                builder.AppendLine("## Logs (até 50 entradas)");

                if (_taggedLogs.Count == 0)
                {
                    builder.AppendLine("- (sem logs marcados por [PlayerMoveTest])");
                }
                else
                {
                    int count = 0;
                    foreach (var entry in _taggedLogs)
                    {
                        builder.AppendLine($"- {entry}");
                        count++;
                        if (count >= 50)
                        {
                            builder.AppendLine($"- (truncado; total={_taggedLogs.Count})");
                            break;
                        }
                    }
                }

                File.WriteAllText(ReportPath, builder.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                    $"{LogTag} Falha ao escrever relatório: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private string ComputeFinalResult()
        {
            if (_timeout)
            {
                return "FAIL";
            }

            if (_inconclusive || !_testAResult.HasValue || !_testBResult.HasValue || !_testCResult.HasValue)
            {
                return "INCONCLUSIVE";
            }

            if (_testAResult.Value && _testBResult.Value && _testCResult.Value && !_hasFailMarker)
            {
                return "PASS";
            }

            return "FAIL";
        }

        private void FinalizeAndExit(float startTime, string result)
        {
            Application.logMessageReceived -= OnLogMessage;
            WriteReport(result);

            int exit = result switch
            {
                "PASS" => 0,
                "INCONCLUSIVE" => 3,
                _ => 2
            };

            Environment.ExitCode = exit;

            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} RESULT={result} ExitCode={exit} Tempo={(Time.realtimeSinceStartup - startTime):F2}s");

            if (Application.isBatchMode)
            {
                Application.Quit(Environment.ExitCode);
            }
        }

        private void MarkInconclusive(string reason)
        {
            _inconclusive = true;
            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} INCONCLUSIVE - {reason}");
        }

        private void MarkTimeout()
        {
            _timeout = true;
            DebugUtility.LogError(typeof(PlayerMovementLeakSmokeBootstrap),
                $"{LogTag} FAIL - Timeout atingido ({TimeoutSeconds}s).");
        }

        private static string FormatStatus(bool? status)
        {
            if (!status.HasValue)
            {
                return "N/A";
            }
            return status.Value ? "PASS" : "FAIL";
        }

        private bool HasTimedOut(float startTime)
        {
            return Time.realtimeSinceStartup - startTime >= TimeoutSeconds;
        }

        private static float HorizontalSpeed(Rigidbody rb)
        {
            if (rb == null)
            {
                return 0f;
            }

            var v = rb.velocity;
            return new Vector2(v.x, v.z).magnitude;
        }

        private static float HorizontalDistance(Vector3 from, Vector3 to)
        {
            var delta = to - from;
            return new Vector2(delta.x, delta.z).magnitude;
        }

        private static string BuildTransformPath(Transform t)
        {
            if (t == null)
            {
                return "<null>";
            }

            var stack = new Stack<string>();
            var cursor = t;
            while (cursor != null)
            {
                stack.Push(cursor.name);
                cursor = cursor.parent;
            }

            return string.Join("/", stack);
        }

        private readonly struct PlayerContext
        {
            public PlayerContext(GameObject gameObject, PlayerActor actor, NewPlayerMovementController controller, Rigidbody rb, string path)
            {
                GameObject = gameObject;
                Actor = actor;
                Controller = controller;
                Rigidbody = rb;
                Path = path;
            }

            public GameObject GameObject { get; }
            public PlayerActor Actor { get; }
            public NewPlayerMovementController Controller { get; }
            public Rigidbody Rigidbody { get; }
            public string Path { get; }
        }
    }
#else
    /// <summary>
    /// Stub para builds sem UNITY_EDITOR/DEVELOPMENT_BUILD, garantindo compilação.
    /// </summary>
    public sealed class PlayerMovementLeakSmokeBootstrap : MonoBehaviour
    {
        private void Start()
        {
            DebugUtility.Log(typeof(PlayerMovementLeakSmokeBootstrap),
                "[PlayerMoveTest][Leak] INCONCLUSIVE - disponível apenas em Editor ou Development Build.");
            Environment.ExitCode = 3;
        }
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// Entry point para batchmode via -executeMethod.
    /// </summary>
    public static class PlayerMovementLeakSmokeBootstrapCI
    {
        public static void Run()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }
        }
    }
#endif
}
