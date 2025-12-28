#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopStateFlowQaTester : MonoBehaviour
    {
        private const string DefaultStartProfile = "startup";
        private const float DefaultTimeoutSeconds = 8f;

        private const string StateBoot = "Boot";
        private const string StateReady = "Ready";
        private const string StatePlaying = "Playing";
        private const string StatePaused = "Paused";

        private enum FlowMode
        {
            FrontendStartup = 0,
            Gameplay = 1
        }

        [Header("Runner")]
        [SerializeField] private string label = "GameLoopStateFlowQATester";
        [SerializeField] private bool runOnStart;
        [SerializeField] private int warmupFrames = 3;
        [SerializeField] private float timeoutSeconds = DefaultTimeoutSeconds;

        [Header("Mode")]
        [SerializeField] private FlowMode mode = FlowMode.FrontendStartup;

        [Header("Frontend (Menu) Expectations")]
        [SerializeField] private string expectedMenuSceneName = "MenuScene";
        [SerializeField] private string expectedUiGlobalSceneName = "UIGlobalScene";
        [SerializeField] private bool waitForMenuScenesLoaded = true;

        [Tooltip("Em produção, o Start já é solicitado pelo bootstrap. Deixe false para não disparar Start duas vezes.")]
        [SerializeField] private bool triggerStartRequestInTest = false;

        [Header("Scene Flow Filter")]
        [SerializeField] private string expectedStartProfile = DefaultStartProfile;

        private int _passes;
        private int _fails;

        private IGameLoopService _loop;
        private IStateDependentService _stateDependentService;
        private ISimulationGateService _gateService;

        private EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;
        private bool _seenScenesReady;
        private string _scenesReadyProfile = string.Empty;

        private bool _running;

        private void Awake()
        {
            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);
        }

        private void OnDestroy()
        {
            if (_onScenesReady != null)
            {
                EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);
                _onScenesReady = null;
            }
        }

        private void Start()
        {
            if (runOnStart)
            {
                Run();
            }
        }

        [ContextMenu("QA/GameLoop/State Flow/Run")]
        public void Run()
        {
            if (_running) return;
            StartCoroutine(RunFlow());
        }

        private IEnumerator RunFlow()
        {
            _running = true;
            _passes = _fails = 0;

            try
            {
                if (!TryResolveDependencies())
                {
                    Fail("Dependências críticas indisponíveis.");
                    yield break;
                }

                yield return WaitFrames(warmupFrames);

                if (mode == FlowMode.FrontendStartup)
                {
                    yield return RunFrontendStartupFlow();
                }
                else
                {
                    yield return RunGameplayFlow();
                }
            }
            finally
            {
                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    $"[QA] {label}: Completo. Passes={_passes} Fails={_fails}.",
                    _fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

                _running = false;
            }
        }

        private bool TryResolveDependencies()
        {
            var provider = DependencyManager.Provider;

            if (!provider.TryGetGlobal<IGameLoopService>(out _loop) || _loop == null)
            {
                return LogFail("IGameLoopService indisponível.");
            }

            if (!provider.TryGetGlobal(out _stateDependentService) || _stateDependentService == null)
            {
                return LogFail("IStateDependentService indisponível.");
            }

            if (!provider.TryGetGlobal(out _gateService) || _gateService == null)
            {
                return LogFail("ISimulationGateService indisponível.");
            }

            return true;

            bool LogFail(string msg)
            {
                DebugUtility.LogError(typeof(GameLoopStateFlowQaTester), $"[QA] {label}: {msg}");
                return false;
            }
        }

        // ----------------------------
        // Frontend (startup/menu)
        // ----------------------------

        private IEnumerator RunFrontendStartupFlow()
        {
            if (waitForMenuScenesLoaded)
            {
                yield return WaitForMenuScenes();
            }

            // Em Menu, esperamos Ready (ou Boot transitório, dependendo do tick).
            yield return WaitForStateNameOneOf("InitialState", StateReady, StateBoot);

            // Em Menu, Move deve estar bloqueado.
            ValidateMovePermission(expected: false, "Frontend/Initial");

            ResetScenesReadyTracking();

            if (triggerStartRequestInTest)
            {
                // Atenção: isto dispara Start DE NOVO. Use somente se você desabilitar o bootstrap de Start.
                DebugUtility.Log(typeof(GameLoopStateFlowQaTester),
                    "[QA] Disparando GameStartRequestedEvent pelo teste (triggerStartRequestInTest=true).",
                    DebugUtility.Colors.Warning);

                EventBus<GameStartRequestedEvent>.Raise(new GameStartRequestedEvent());

                yield return WaitForScenesReady();
                yield return WaitFrames(2);

                // Após startup no Menu, ainda é Ready.
                yield return WaitForStateNameOneOf("AfterStartRequest", StateReady, StateBoot);
                ValidateMovePermission(expected: false, "Frontend/AfterStartRequest");
            }

            // Pause gate: em Frontend, Move já está bloqueado.
            yield return ValidatePauseGate(expectMoveBeforeGate: false, expectMoveAfterRelease: false);

            // Reset: em Frontend, esperamos continuar bloqueando Move após reset.
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());
            yield return WaitForStateNameOneOf("PostReset", StateReady, StateBoot);

            // Se isso falhar, é bug real no StateDependentService (ele está liberando Move em Boot/Ready).
            ValidateMovePermission(expected: false, "Frontend/PostReset");
        }

        private IEnumerator WaitForMenuScenes()
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                var active = SceneManager.GetActiveScene().name;

                bool menuOk = string.IsNullOrWhiteSpace(expectedMenuSceneName) || SceneManager.GetSceneByName(expectedMenuSceneName).isLoaded;
                bool uiOk = string.IsNullOrWhiteSpace(expectedUiGlobalSceneName) || SceneManager.GetSceneByName(expectedUiGlobalSceneName).isLoaded;

                if (active == expectedMenuSceneName && menuOk && uiOk)
                {
                    Pass($"MenuScenesLoaded: active='{active}', menuOk={menuOk}, uiOk={uiOk}.");
                    yield break;
                }

                yield return null;
            }

            Fail($"MenuScenesLoaded: timeout. active='{SceneManager.GetActiveScene().name}'.");
        }

        // ----------------------------
        // Gameplay (futuro)
        // ----------------------------

        private IEnumerator RunGameplayFlow()
        {
            // Este fluxo só faz sentido quando a GameplayScene estiver pronta e o GameLoop realmente entrar em Playing.
            // Mantemos aqui para não quebrar, mas ele é intencionalmente conservador.
            yield return WaitForStateNameOneOf("InitialState", StateReady, StateBoot);

            // Tentar ir para Playing sem gameplay pronta tende a falhar.
            // Quando a gameplay estiver implementada, você pode:
            // 1) disparar navegação (IGameNavigationService) para Gameplay
            // 2) esperar ScenesReady/Completed do profile gameplay
            // 3) esperar StatePlaying
            Fail("Gameplay flow ainda não habilitado: requer GameplayScene pronta + navegação para gameplay.");
        }

        // ----------------------------
        // Shared helpers
        // ----------------------------

        private IEnumerator WaitForStateNameOneOf(string step, params string[] acceptedStates)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                string current = _loop.CurrentStateIdName ?? string.Empty;

                for (int i = 0; i < acceptedStates.Length; i++)
                {
                    if (current == acceptedStates[i])
                    {
                        Pass($"{step}: estado '{current}'.");
                        yield break;
                    }
                }

                yield return null;
            }

            Fail($"{step}: timeout. atual='{_loop.CurrentStateIdName}' aceitos=[{string.Join(", ", acceptedStates)}].");
        }

        private IEnumerator WaitForScenesReady()
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (_seenScenesReady)
                {
                    Pass($"ScenesReady (profile='{_scenesReadyProfile}').");
                    yield break;
                }

                yield return null;
            }

            Fail("Timeout ScenesReady.");
        }

        private void ResetScenesReadyTracking()
        {
            _seenScenesReady = false;
            _scenesReadyProfile = string.Empty;
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (_seenScenesReady) return;

            string profile = evt.Context.TransitionProfileName ?? string.Empty;
            if (profile != expectedStartProfile) return;

            _seenScenesReady = true;
            _scenesReadyProfile = profile;
        }

        private IEnumerator ValidatePauseGate(bool expectMoveBeforeGate, bool expectMoveAfterRelease)
        {
            bool before = _stateDependentService.CanExecuteAction(ActionType.Move);

            if (before == expectMoveBeforeGate)
                Pass($"Move {(before ? "liberado" : "bloqueado")} antes do gate (ok).");
            else
                Fail($"Move {(before ? "liberado" : "bloqueado")} antes do gate (inesperado). esperado={(expectMoveBeforeGate ? "liberado" : "bloqueado")}.");

            using (_gateService.Acquire(SimulationGateTokens.Pause))
            {
                yield return null;

                if (_stateDependentService.CanExecuteAction(ActionType.Move))
                    Fail("Gate Pause não bloqueou Move.");
                else
                    Pass("Gate Pause bloqueou Move.");
            }

            yield return null;

            bool after = _stateDependentService.CanExecuteAction(ActionType.Move);

            if (after == expectMoveAfterRelease)
                Pass($"Move {(after ? "liberado" : "bloqueado")} após release (ok).");
            else
                Fail($"Move {(after ? "liberado" : "bloqueado")} após release (inesperado). esperado={(expectMoveAfterRelease ? "liberado" : "bloqueado")}.");
        }

        private void ValidateMovePermission(bool expected, string contextInfo)
        {
            bool actual = _stateDependentService.CanExecuteAction(ActionType.Move);

            if (actual == expected)
                Pass($"Move {(expected ? "liberado" : "bloqueado")} em {contextInfo}.");
            else
                Fail($"Move {(expected ? "não liberado" : "liberado")} em {contextInfo}.");
        }

        private IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private void Pass(string msg)
        {
            _passes++;
            DebugUtility.Log(typeof(GameLoopStateFlowQaTester), $"[QA] PASS - {msg}", DebugUtility.Colors.Success);
        }

        private void Fail(string msg)
        {
            _fails++;
            DebugUtility.LogError(typeof(GameLoopStateFlowQaTester), $"[QA] FAIL - {msg}");
        }
    }
}

#endif
