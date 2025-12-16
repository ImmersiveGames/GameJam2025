using System;
using System.Text;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Domain;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Application.Services;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Domain.Configs;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.Scripts.UISystems.TerminalOverlay;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.GameplaySystems
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
        [SerializeField] private KeyCode eaterResetSmokeKey = KeyCode.F9;

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

            if (Input.GetKeyDown(eaterResetSmokeKey))
                RunEaterResetSmoke();
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

        public void RunEaterResetSmoke()
        {
            _ = RunEaterResetSmokeAsync();
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

        private async Task RunEaterResetSmokeAsync()
        {
            if (!BeginRun("EATER_RESET_SMOKE"))
                return;

            try
            {
                await RunStep("Eater Reset Smoke", ExecuteEaterResetSmokeAsync);
            }
            catch (Exception ex)
            {
                FailRun("EATER_RESET_SMOKE", ex);
            }
            finally
            {
                EndRun("EATER_RESET_SMOKE");
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

        private async Task<bool> RequestResetForScopeAsync(ResetScope scope, string reason)
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

            AppendLine($"[QA] Chamando IResetOrchestrator.RequestResetAsync (Scope={scope}, Reason='{reason}')...");
            DebugUtility.LogVerbose<QaOverlayE2ETester>($"[QA] Reset Scope={scope} => RequestResetAsync");

            bool ok = await orchestrator.RequestResetAsync(new ResetRequest(scope, reason));

            await DelayUnscaled(0.1f);

            if (Mathf.Abs(Time.timeScale - 1f) > 0.001f)
            {
                DebugUtility.LogWarning<QaOverlayE2ETester>(
                    $"[QA] TimeScale != 1 após reset (scope={scope}). Atual={Time.timeScale:0.###}");
            }

            return ok;
        }

        private Task LogPoseSnapshotAsync(string stage, IActor actor)
        {
            if (actor?.Transform == null)
            {
                AppendLine($"{stage}: actor/transform indisponível.");
                return Task.CompletedTask;
            }

            Transform t = actor.Transform;

            AppendLine(
                $"{stage}: name={t.name} id={t.GetInstanceID()} scene={t.gameObject.scene.name} " +
                $"pos={t.position} rot={t.rotation.eulerAngles} frame={Time.frameCount}");

            return Task.CompletedTask;
        }

        private async Task ExecuteEaterResetSmokeAsync()
        {
            AppendLine("[QA] Eater Reset Smoke: garantindo Gameplay e serviços...");
            await EnsureGameplayReadyAsync();

            if (!TryResolveEaterServices(out var eaterDomain, out var eaterActor, out var eaterContext))
                throw new Exception("[QA] Não foi possível resolver Eater ou RuntimeAttributeContext.");

            RuntimeAttributeType trackedAttribute = PickTrackedAttribute(eaterContext);
            if (trackedAttribute == RuntimeAttributeType.None)
                throw new Exception("[QA] Nenhum atributo rastreável encontrado para o Eater.");

            if (!eaterContext.TryGetValue(trackedAttribute, out var attributeValue) || attributeValue == null)
                throw new Exception($"[QA] RuntimeAttributeValue ausente para {trackedAttribute}.");

            Vector3 initialPosition = default;
            Quaternion initialRotation = default;
            bool hasSpawnTransform = false;

            if (eaterDomain != null)
                hasSpawnTransform = eaterDomain.TryGetSpawnTransform(out initialPosition, out initialRotation);

            if (!hasSpawnTransform)
            {
                if (eaterActor.Transform != null)
                {
                    initialPosition = eaterActor.Transform.position;
                    initialRotation = eaterActor.Transform.rotation;
                    hasSpawnTransform = true;
                    AppendLine(
                        "[QA] SpawnTransform do domínio ausente; usando Transform atual do Eater como fallback para validação.");
                }
            }

            if (!hasSpawnTransform)
                throw new Exception("[QA] SpawnTransform do Eater indisponível para validação de reset.");

            float initialAttributeValue = attributeValue.GetCurrentValue();
            int callbacks = 0;

            void OnResourceChanged(RuntimeAttributeChangeContext change)
            {
                if (change.RuntimeAttributeType != trackedAttribute)
                    return;

                callbacks++;
                AppendLine(
                    $"[QA] ResourceChanged {trackedAttribute}: {change.PreviousValue:0.###} -> {change.NewValue:0.###} " +
                    $"(Δ={change.Delta:0.###}, Source={change.Source}, Linked={change.IsLinkedChange})");
            }

            eaterContext.ResourceChanged += OnResourceChanged;

            try
            {
                float firstDelta = ComputeDamageDelta(initialAttributeValue, attributeValue.GetMaxValue());
                AppendLine($"[QA] Mutando {trackedAttribute}: delta=-{firstDelta:0.###}");
                eaterContext.Modify(trackedAttribute, -firstDelta, RuntimeAttributeChangeSource.Manual);

                Vector3 offset = new Vector3(0.5f, 0f, 0f);
                eaterActor.Transform.position += offset;
                AppendLine($"[QA] Pose deslocada por {offset} (novo={eaterActor.Transform.position})");

                await ResetAndValidateAsync(
                    label: "1",
                    eaterActor,
                    eaterContext,
                    trackedAttribute,
                    initialPosition,
                    initialRotation,
                    initialAttributeValue);

                int callbacksBeforeSecondMutation = callbacks;
                float secondDelta = ComputeDamageDelta(initialAttributeValue, attributeValue.GetMaxValue());
                AppendLine($"[QA] Mutação pós-reset #2: delta=-{secondDelta:0.###}");
                eaterContext.Modify(trackedAttribute, -secondDelta, RuntimeAttributeChangeSource.Manual);

                int callbacksFromSecondMutation = callbacks - callbacksBeforeSecondMutation;
                AppendLine($"[QA] Callbacks recebidos após mutação #2: {callbacksFromSecondMutation} (total={callbacks})");
                if (callbacksFromSecondMutation != 1)
                    throw new Exception(
                        $"Esperado 1 callback ResourceChanged para a mutação #2, recebido {callbacksFromSecondMutation}.");

                await ResetAndValidateAsync(
                    label: "2",
                    eaterActor,
                    eaterContext,
                    trackedAttribute,
                    initialPosition,
                    initialRotation,
                    initialAttributeValue);
            }
            finally
            {
                eaterContext.ResourceChanged -= OnResourceChanged;
            }
        }

        private async Task ResetAndValidateAsync(
            string label,
            IActor eaterActor,
            RuntimeAttributeContext eaterContext,
            RuntimeAttributeType trackedAttribute,
            Vector3 initialPosition,
            Quaternion initialRotation,
            float initialAttributeValue)
        {
            await LogPoseSnapshotAsync(
                stage: $"[QA] Pose antes do RequestResetAsync #{label}",
                actor: eaterActor);

            bool ok = await RequestResetForScopeAsync(ResetScope.EaterOnly, $"QA Eater Reset Smoke #{label}");
            if (!ok)
                throw new Exception($"[QA] Reset scope EaterOnly falhou no passo {label}.");

            await LogPoseSnapshotAsync(
                stage: $"[QA] Pose imediatamente após RequestResetAsync #{label}",
                actor: eaterActor);

            if (!eaterContext.TryGetValue(trackedAttribute, out var value) || value == null)
                throw new Exception($"[QA] RuntimeAttributeValue inexistente após reset #{label} para {trackedAttribute}.");

            float restoredValue = value.GetCurrentValue();
            Vector3 poseAfterReset = eaterActor.Transform.position;
            Quaternion rotationAfterReset = eaterActor.Transform.rotation;

            bool attributeRestored = Mathf.Approximately(restoredValue, initialAttributeValue);
            bool poseRestored = Vector3.Distance(initialPosition, poseAfterReset) <= 0.05f &&
                                Quaternion.Angle(initialRotation, rotationAfterReset) <= 1f;

            AppendLine(
                $"[QA] Reset #{label}: atributo {trackedAttribute}={restoredValue:0.###} (esperado {initialAttributeValue:0.###}), " +
                $"poseRestaurada={poseRestored} pos={poseAfterReset} rot={rotationAfterReset.eulerAngles}");

            await Task.Yield();

            await LogPoseSnapshotAsync(
                stage: $"[QA] Pose no frame seguinte ao reset #{label}",
                actor: eaterActor);

            if (!attributeRestored)
                throw new Exception($"[QA] Reset #{label} não restaurou {trackedAttribute}. Valor={restoredValue:0.###}.");

            if (!poseRestored)
                throw new Exception($"[QA] Reset #{label} não restaurou pose do Eater.");
        }

        private async Task EnsureGameplayReadyAsync()
        {
            ResolveReferences();

            if (gameManager == null)
                throw new Exception("[QA] GameManager não encontrado para iniciar gameplay.");

            gameManager.QA_GoToGameplayFromAnywhere();

            await WaitUntilAsync(
                predicate: () => GameManagerStateMachine.Instance != null &&
                                 GameManagerStateMachine.Instance.CurrentState is PlayingState,
                timeoutSeconds: defaultStepTimeoutSeconds,
                timeoutMessage: "FSM não entrou em PlayingState durante Eater Reset Smoke.");

            await DelayUnscaled(afterTransitionSettleSeconds);
        }

        private bool TryResolveEaterServices(
            out IEaterDomain eaterDomain,
            out IActor eaterActor,
            out RuntimeAttributeContext eaterContext)
        {
            eaterDomain = null;
            eaterActor = null;
            eaterContext = null;

            if (string.IsNullOrWhiteSpace(gameplaySceneNameForReset))
            {
                AppendLine("[QA] gameplaySceneNameForReset vazio; não é possível resolver IEaterDomain.");
                return false;
            }

            if (!DependencyManager.Provider.TryGetForScene<IEaterDomain>(gameplaySceneNameForReset, out eaterDomain) ||
                eaterDomain == null ||
                eaterDomain.Eater == null)
            {
                AppendLine($"[QA] IEaterDomain ou Eater ausente para a cena '{gameplaySceneNameForReset}'.");
                return false;
            }

            eaterActor = eaterDomain.Eater;

            if (!DependencyManager.Provider.TryGetForObject<RuntimeAttributeContext>(eaterActor.ActorId, out eaterContext) ||
                eaterContext == null)
            {
                AppendLine($"[QA] RuntimeAttributeContext não encontrado para Eater '{eaterActor.ActorId}'.");
                eaterActor = null;
                return false;
            }

            AppendLine($"[QA] Eater resolvido: {eaterActor.ActorName} ({eaterActor.ActorId}).");
            return true;
        }

        private static RuntimeAttributeType PickTrackedAttribute(RuntimeAttributeContext eaterContext)
        {
            if (eaterContext == null)
                return RuntimeAttributeType.None;

            if (eaterContext.TryGetValue(RuntimeAttributeType.Health, out var health) && health != null)
                return RuntimeAttributeType.Health;

            foreach (var type in eaterContext.GetAllRegisteredTypes())
            {
                if (type != RuntimeAttributeType.None)
                    return type;
            }

            return RuntimeAttributeType.None;
        }

        private static float ComputeDamageDelta(float current, float max)
        {
            float safeMax = Mathf.Max(1f, max);
            float targetDelta = Mathf.Max(1f, safeMax * 0.1f);
            return Mathf.Clamp(targetDelta, 1f, Mathf.Max(1f, current));
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

            if (_titleStyle == null || _boxStyle == null)
                BuildGuiStyles();

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

            if (GUILayout.Button("Eater Reset Smoke", GUILayout.Height(28)))
                RunEaterResetSmoke();
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

            GUILayout.Label($"Toggle: {togglePanelKey} | EaterSmokeKey: {eaterResetSmokeKey}");
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
