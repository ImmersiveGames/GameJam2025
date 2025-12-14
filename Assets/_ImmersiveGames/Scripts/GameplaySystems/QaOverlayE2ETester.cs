using System;
using System.Text;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.UISystems.TerminalOverlay;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.QA
{
    /// <summary>
    /// QA E2E tester para validar:
    /// - EventSystem único e ativo
    /// - QA_GoToGameplayFromAnywhere (fluxo real)
    /// - Force GameOver / Force Victory e visibilidade do TerminalOverlay
    /// - ReturnToMenu via EventBus (GameReturnToMenuRequestedEvent)
    ///
    /// IMPORTANTE:
    /// Existem DOIS tipos de reset no projeto:
    /// 1) Reset MACRO (GameResetRequestedEvent) -> MenuContext/GameManager -> pode envolver fade/scene flow.
    /// 2) Reset IN-PLACE (IResetOrchestrator) -> reset local dos atores (Cleanup/Restore/Rebind) sem recarregar cena.
    ///
    /// Para validar o sistema novo, use RESET IN-PLACE.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class QaOverlayE2ETester : MonoBehaviour
    {
        [Header("Auto Run")]
        [SerializeField] private bool autoRunOnStart = false;
        [SerializeField] private float autoRunDelaySeconds = 2.0f;

        [Header("Expected Scene Names (optional asserts)")]
        [SerializeField] private bool strictSceneAsserts = false;
        [SerializeField] private string expectedMenuSceneName = "MenuScene";
        [SerializeField] private string expectedGameplaySceneName = "GameplayScene";

        [Header("Timeouts")]
        [SerializeField] private float defaultStepTimeoutSeconds = 6.0f;
        [SerializeField] private float overlayAppearTimeoutSeconds = 2.0f;
        [SerializeField] private float afterTransitionSettleSeconds = 0.15f;

        [Header("Reset Settings")]
        [Tooltip("Cena onde o IResetOrchestrator foi registrado no DI (scene-scoped).")]
        [SerializeField] private string gameplaySceneNameForReset = "GameplayScene";

        [Header("References (optional; will auto-resolve if null)")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TerminalOverlayController terminalOverlay;

        [Header("UI (OnGUI)")]
        [SerializeField] private bool showOnGuiPanel = true;
        [SerializeField] private KeyCode togglePanelKey = KeyCode.F8;

        private bool _isRunning;
        private bool _panelVisible = true;

        private int _passCount;
        private int _failCount;

        private string _currentStep = "-";
        private string _lastError = "-";
        private readonly StringBuilder _logBuffer = new StringBuilder(4096);

        private GUIStyle _titleStyle;
        private GUIStyle _boxStyle;
        private Vector2 _scroll;

        private void Awake()
        {
            BuildGuiStyles();
            AppendLine("[QA] QaOverlayE2ETester pronto.");
        }

        private async void Start()
        {
            if (!autoRunOnStart)
                return;

            await DelayUnscaled(autoRunDelaySeconds);
            _ = RunFullSuiteAsync();
        }

        private void Update()
        {
            if (Input.GetKeyDown(togglePanelKey))
                _panelVisible = !_panelVisible;
        }

        #region Context Menu (Editor)

        [ContextMenu("QA/Reset IN-PLACE (IResetOrchestrator)")]
        private void Context_ResetInPlace()
        {
            _ = ResetInPlaceAsync();
        }

        [ContextMenu("QA/Reset MACRO (GameResetRequestedEvent)")]
        private void Context_ResetMacro()
        {
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
            DebugUtility.LogWarning<QaOverlayE2ETester>("[QA] Disparado Reset MACRO via GameResetRequestedEvent.");
        }

        #endregion

        #region Public buttons

        public void RunFullSuite()
        {
            _ = RunFullSuiteAsync();
        }

        public void RunSmoke()
        {
            _ = RunSmokeAsync();
        }

        public void Stop()
        {
            if (!_isRunning) return;
            AppendLine("[QA] Stop requested (soft). O runner atual irá finalizar no próximo await.");
            _isRunning = false;
        }

        #endregion

        #region Runner

        private async Task RunSmokeAsync()
        {
            if (!BeginRun("SMOKE"))
                return;

            try
            {
                await Step_CheckEventSystemSingleActiveAsync();
                await Step_GoToGameplayAsync();
                await Step_ForceGameOverAsync();
                await Step_ReturnToMenuAsync();
            }
            catch (Exception ex)
            {
                FailRun("SMOKE", ex);
            }
            finally
            {
                EndRun("SMOKE");
            }
        }

        private async Task RunFullSuiteAsync()
        {
            if (!BeginRun("FULL"))
                return;

            try
            {
                await Step_CheckEventSystemSingleActiveAsync();
                await Step_CheckEventSystemSingleActiveAsync(); // repetição intencional
                await Step_GoToGameplayAsync();
                await Step_ForceGameOverAsync();

                await Step_GoToGameplayAsync();
                await Step_ForceVictoryAsync();

                // IMPORTANTE: no FULL, não usamos reset macro (ele conflita com fluxo do MenuContext).
                await Step_ResetInPlaceAsync();

                await Step_CheckEventSystemSingleActiveAsync();
            }
            catch (Exception ex)
            {
                FailRun("FULL", ex);
            }
            finally
            {
                EndRun("FULL");
            }
        }

        private bool BeginRun(string mode)
        {
            if (_isRunning)
            {
                DebugUtility.LogWarning<QaOverlayE2ETester>("[QA] Runner já está em execução.");
                return false;
            }

            _isRunning = true;
            _passCount = 0;
            _failCount = 0;
            _currentStep = "-";
            _lastError = "-";
            _logBuffer.Length = 0;

            ResolveReferences();

            AppendLine($"[QA] ===== RUN START ({mode}) =====");
            DebugUtility.LogVerbose<QaOverlayE2ETester>($"[QA] RUN START ({mode})");

            return true;
        }

        private void EndRun(string mode)
        {
            AppendLine($"[QA] ===== RUN END ({mode}) | PASS={_passCount} FAIL={_failCount} =====");
            DebugUtility.LogVerbose<QaOverlayE2ETester>($"[QA] RUN END ({mode}) | PASS={_passCount} FAIL={_failCount}");
            _isRunning = false;
        }

        private void FailRun(string mode, Exception ex)
        {
            _failCount++;
            _lastError = ex.Message;
            AppendLine($"[QA] RUN FAIL ({mode}): {ex}");
            DebugUtility.LogError<QaOverlayE2ETester>($"[QA] RUN FAIL ({mode}): {ex}");
        }

        #endregion

        #region Steps

        private async Task Step_CheckEventSystemSingleActiveAsync()
        {
            await RunStep("Check EventSystem (single active)", async () =>
            {
                EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

                int activeAndEnabled = 0;
                for (int i = 0; i < systems.Length; i++)
                {
                    var es = systems[i];
                    if (es != null && es.isActiveAndEnabled)
                        activeAndEnabled++;
                }

                if (activeAndEnabled != 1)
                    throw new Exception($"EventSystem inválido: activeAndEnabled={activeAndEnabled}, totalEncontrados={systems.Length}");

                await Task.CompletedTask;
            });
        }

        private async Task Step_GoToGameplayAsync()
        {
            await RunStep("Go To Gameplay (QA flow)", async () =>
            {
                ResolveReferences();

                if (gameManager == null)
                    throw new Exception("GameManager não encontrado.");

                gameManager.QA_GoToGameplayFromAnywhere();

                await WaitUntilAsync(
                    predicate: () => GameManagerStateMachine.Instance != null &&
                                     GameManagerStateMachine.Instance.CurrentState is PlayingState,
                    timeoutSeconds: defaultStepTimeoutSeconds,
                    timeoutMessage: "FSM não entrou em PlayingState após QA_GoToGameplayFromAnywhere."
                );

                await DelayUnscaled(afterTransitionSettleSeconds);

                if (strictSceneAsserts && !string.IsNullOrWhiteSpace(expectedGameplaySceneName))
                {
                    string active = SceneManager.GetActiveScene().name;
                    if (!string.Equals(active, expectedGameplaySceneName, StringComparison.Ordinal))
                    {
                        DebugUtility.LogWarning<QaOverlayE2ETester>(
                            $"[QA] Cena ativa esperada='{expectedGameplaySceneName}', atual='{active}'.");
                    }
                }
            });
        }

        private async Task Step_ForceGameOverAsync()
        {
            await RunStep("Force GameOver -> Terminal Overlay visible", async () =>
            {
                ResolveReferences();

                if (gameManager == null)
                    throw new Exception("GameManager não encontrado.");

                if (terminalOverlay == null)
                    throw new Exception("TerminalOverlayController não encontrado.");

                await WaitUntilAsync(
                    predicate: () => GameManagerStateMachine.Instance != null &&
                                     GameManagerStateMachine.Instance.CurrentState is PlayingState,
                    timeoutSeconds: defaultStepTimeoutSeconds,
                    timeoutMessage: "FSM não está em PlayingState antes do GameOver."
                );

                gameManager.QA_ForceGameOverFromAnywhere("QA E2E");

                await WaitUntilAsync(
                    predicate: () => terminalOverlay != null && terminalOverlay.IsVisible,
                    timeoutSeconds: overlayAppearTimeoutSeconds,
                    timeoutMessage: "TerminalOverlay não ficou visível após GameOver."
                );

                if (Mathf.Abs(Time.timeScale - 1f) > 0.001f)
                    throw new Exception($"TimeScale esperado=1 durante GameOver, atual={Time.timeScale:0.###}");
            });
        }

        private async Task Step_ForceVictoryAsync()
        {
            await RunStep("Force Victory -> Terminal Overlay visible", async () =>
            {
                ResolveReferences();

                if (gameManager == null)
                    throw new Exception("GameManager não encontrado.");

                if (terminalOverlay == null)
                    throw new Exception("TerminalOverlayController não encontrado.");

                await WaitUntilAsync(
                    predicate: () => GameManagerStateMachine.Instance != null &&
                                     GameManagerStateMachine.Instance.CurrentState is PlayingState,
                    timeoutSeconds: defaultStepTimeoutSeconds,
                    timeoutMessage: "FSM não está em PlayingState antes da Victory."
                );

                gameManager.QA_ForceVictoryFromAnywhere("QA E2E");

                await WaitUntilAsync(
                    predicate: () => terminalOverlay != null && terminalOverlay.IsVisible,
                    timeoutSeconds: overlayAppearTimeoutSeconds,
                    timeoutMessage: "TerminalOverlay não ficou visível após Victory."
                );

                if (Mathf.Abs(Time.timeScale - 1f) > 0.001f)
                    throw new Exception($"TimeScale esperado=1 durante Victory, atual={Time.timeScale:0.###}");
            });
        }

        private async Task Step_ReturnToMenuAsync()
        {
            await RunStep("ReturnToMenu (EventBus) -> MenuScene loaded", async () =>
            {
                EventBus<GameReturnToMenuRequestedEvent>.Raise(new GameReturnToMenuRequestedEvent());

                await WaitUntilAsync(
                    predicate: () => GameManagerStateMachine.Instance != null &&
                                     GameManagerStateMachine.Instance.CurrentState is MenuState,
                    timeoutSeconds: defaultStepTimeoutSeconds,
                    timeoutMessage: "FSM não retornou para MenuState após GameReturnToMenuRequestedEvent."
                );

                await DelayUnscaled(afterTransitionSettleSeconds);

                if (strictSceneAsserts && !string.IsNullOrWhiteSpace(expectedMenuSceneName))
                {
                    var menuScene = SceneManager.GetSceneByName(expectedMenuSceneName);
                    if (!menuScene.isLoaded)
                        throw new Exception($"MenuScene esperada não está carregada: '{expectedMenuSceneName}'.");
                }

                ResolveReferences();
                if (terminalOverlay != null && terminalOverlay.IsVisible)
                {
                    DebugUtility.LogWarning<QaOverlayE2ETester>("[QA] Overlay ainda visível após retorno ao menu. Forçando Hide().");
                    terminalOverlay.Hide();
                    await Task.Yield();
                }
            });
        }

        private async Task Step_ResetInPlaceAsync()
        {
            await RunStep("Reset IN-PLACE (IResetOrchestrator)", async () =>
            {
                AppendLine("[QA] Reset IN-PLACE: garantindo Gameplay...");
                await Step_GoToGameplayAsync();

                bool ok = await ResetInPlaceAsync();
                if (!ok)
                    throw new Exception("Reset IN-PLACE falhou (ok=false).");
            });
        }

        #endregion

        #region Reset helpers

        private async Task<bool> ResetInPlaceAsync()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneNameForReset))
            {
                DebugUtility.LogError<QaOverlayE2ETester>("[QA] gameplaySceneNameForReset vazio. Configure no Inspector.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetForScene<IResetOrchestrator>(gameplaySceneNameForReset, out var orchestrator) ||
                orchestrator == null)
            {
                DebugUtility.LogError<QaOverlayE2ETester>(
                    $"[QA] IResetOrchestrator não encontrado no DI para a cena '{gameplaySceneNameForReset}'. " +
                    "Confirme que ResetOrchestratorBehaviour está na GameplayScene e registrou no Awake.");
                return false;
            }

            AppendLine("[QA] Chamando IResetOrchestrator.RequestResetAsync...");
            DebugUtility.LogVerbose<QaOverlayE2ETester>("[QA] Reset IN-PLACE => RequestResetAsync");

            bool ok = await orchestrator.RequestResetAsync(
                new ResetRequest(ResetScope.AllActorsInScene, reason: "QA Reset IN-PLACE"));

            // settle mínimo
            await DelayUnscaled(0.1f);

            if (Mathf.Abs(Time.timeScale - 1f) > 0.001f)
            {
                DebugUtility.LogWarning<QaOverlayE2ETester>(
                    $"[QA] TimeScale != 1 após reset IN-PLACE. Atual={Time.timeScale:0.###}");
            }

            return ok;
        }

        #endregion

        #region Step helpers

        private async Task RunStep(string name, Func<Task> action)
        {
            if (!_isRunning) return;

            _currentStep = name;
            DebugUtility.LogVerbose<QaOverlayE2ETester>($"[QA] Step start: {name}");
            AppendLine($"[QA] Step start: {name}");

            try
            {
                await action();

                _passCount++;
                DebugUtility.LogVerbose<QaOverlayE2ETester>($"[QA] Step PASS: {name}");
                AppendLine($"[QA] Step PASS: {name}");
            }
            catch (Exception ex)
            {
                _failCount++;
                _lastError = ex.Message;

                DebugUtility.LogError<QaOverlayE2ETester>($"[QA] Step FAIL: {name} | {ex}");
                AppendLine($"[QA] Step FAIL: {name} | {ex.Message}");

                throw;
            }
            finally
            {
                await Task.Yield();
            }
        }

        private async Task WaitUntilAsync(Func<bool> predicate, float timeoutSeconds, string timeoutMessage)
        {
            float elapsed = 0f;
            while (_isRunning && elapsed < timeoutSeconds)
            {
                if (predicate())
                    return;

                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            throw new Exception(timeoutMessage);
        }

        private static async Task DelayUnscaled(float seconds)
        {
            if (seconds <= 0f)
            {
                await Task.Yield();
                return;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                await Task.Yield();
            }
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
                gameManager = FindAnyObjectByType<GameManager>();

            if (terminalOverlay == null)
                terminalOverlay = FindAnyObjectByType<TerminalOverlayController>(FindObjectsInactive.Include);
        }

        private void AppendLine(string line)
        {
            _logBuffer.AppendLine(line);
        }

        #endregion

        #region OnGUI

        private void BuildGuiStyles()
        {
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft
            };
        }

        private void OnGUI()
        {
            if (!showOnGuiPanel || !_panelVisible)
                return;

            const int w = 520;
            const int h = 560;
            const int pad = 10;

            Rect rect = new Rect(pad, pad, w, h);
            GUILayout.BeginArea(rect, _boxStyle);

            GUILayout.Label("QA Overlay E2E Tester", _titleStyle);

            GUILayout.Space(4);
            GUILayout.Label($"Running: {_isRunning}");
            GUILayout.Label($"Step: {_currentStep}");
            GUILayout.Label($"PASS: {_passCount}  FAIL: {_failCount}");
            GUILayout.Label($"LastError: {_lastError}");

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUI.enabled = !_isRunning;
            if (GUILayout.Button("Run FULL", GUILayout.Height(28)))
                RunFullSuite();

            if (GUILayout.Button("Run SMOKE", GUILayout.Height(28)))
                RunSmoke();
            GUI.enabled = true;

            GUI.enabled = _isRunning;
            if (GUILayout.Button("Stop", GUILayout.Height(28)))
                Stop();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();

            GUI.enabled = !_isRunning;
            if (GUILayout.Button("Reset IN-PLACE", GUILayout.Height(26)))
                _ = ResetInPlaceAsync();

            if (GUILayout.Button("Reset MACRO", GUILayout.Height(26)))
                EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
            GUI.enabled = true;

            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Log", GUILayout.Height(22)))
                _logBuffer.Length = 0;

            GUILayout.Label($"Toggle: {togglePanelKey}");
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            GUILayout.TextArea(_logBuffer.ToString(), GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        #endregion
    }
}
