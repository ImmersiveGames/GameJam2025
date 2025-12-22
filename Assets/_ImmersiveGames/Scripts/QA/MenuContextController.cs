using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.GameplaySystems.Reset;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.TimerSystem;
using UnityEngine;
using GamePauseEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GamePauseEvent;
using GameResumeRequestedEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameResumeRequestedEvent;
using GameResetRequestedEvent = _ImmersiveGames.NewScripts.Gameplay.GameLoop.GameResetRequestedEvent;

namespace _ImmersiveGames.Scripts.QA
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class MenuContextController : MonoBehaviour
    {
        [Header("Referência (Opcional)")]
        [SerializeField] private GameManager gameManager;

        [Header("Force State Defaults")]
        [SerializeField] private string defaultGameOverReason = "QA ForceGameOver";
        [SerializeField] private string defaultVictoryReason = "QA ForceVictory";

        [Header("QA Gate Token (independente da FSM)")]
        [Tooltip("Token de QA para fechar/abrir o SimulationGate sem envolver FSM e sem mexer no Time.timeScale.")]
        [SerializeField] private string qaPauseGateToken = "qa.pause";

        [Header("Reset IN-PLACE (Orchestrator)")]
        [Tooltip("Nome da cena onde o ResetOrchestratorBehaviour está registrado no DI (scene-scoped).")]
        [SerializeField] private string gameplaySceneNameForOrchestrator = "GameplayScene";

        [Tooltip("Loga detalhes extras quando roda Reset IN-PLACE.")]
        [SerializeField] private bool logResetInPlaceVerbose = true;

        [Header("Full Reset (IN-PLACE) Behavior")]
        [Tooltip("Se true, quando ResetInPlace(AllActorsInScene) concluir com sucesso, também reseta o GameTimer para a duração configurada.")]
        [SerializeField] private bool resetTimerOnInPlaceAllActors = true;

        private ISimulationGateService _gateService;
        private bool _qaGateTokenHeld;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
                if (gameManager == null)
                {
                    DebugUtility.LogWarning<MenuContextController>(
                        "[MenuContext] GameManager não encontrado no Awake. " +
                        "Se os comandos forem usados antes do GameManager existir, serão ignorados.");
                }
            }

            TryResolveGateService();
            LogDiagnostics("[MenuContext] Awake");
        }

        private GameManager ResolveGameManager()
        {
            if (gameManager != null)
                return gameManager;

            gameManager = FindAnyObjectByType<GameManager>();
            return gameManager;
        }

        private bool TryResolveGateService()
        {
            if (_gateService != null)
                return true;

            var provider = DependencyManager.Provider;
            if (provider != null && provider.TryGetGlobal<ISimulationGateService>(out var gate))
            {
                _gateService = gate;
                return true;
            }

            return false;
        }

        private void LogDiagnostics(string header)
        {
            string fsmState = GameManagerStateMachine.Instance != null
                ? GameManagerStateMachine.Instance.CurrentState?.GetType().Name ?? "null"
                : "FSM.Instance=null";

            DebugUtility.LogVerbose<MenuContextController>(
                $"{header} | FSM={fsmState} | timeScale={Time.timeScale:0.###} | QA_GateHeld={_qaGateTokenHeld}");
        }

        // =========================
        // Context Menu (Inspector)
        // =========================

        [ContextMenu("QA/Go To Menu (From Anywhere)")]
        private void CM_GoToMenu() => GoToMenu();

        [ContextMenu("QA/Go To Gameplay (From Anywhere)")]
        private void CM_GoToGameplay() => GoToGameplay();

        [ContextMenu("QA/Pause (FSM Request)")]
        private void CM_RequestPause() => RequestPause();

        [ContextMenu("QA/Resume (FSM Request)")]
        private void CM_RequestResume() => RequestResume();

        [ContextMenu("QA/Soft Reset (FSM Request) [MACRO]")]
        private void CM_SoftReset() => SoftReset();

        [ContextMenu("QA/Force GameOver (Default Reason)")]
        private void CM_ForceGameOver() => ForceGameOver();

        [ContextMenu("QA/Force Victory (Default Reason)")]
        private void CM_ForceVictory() => ForceVictory();

        [ContextMenu("QA Gate/Acquire Pause Token (NO FSM, NO timeScale)")]
        private void CM_QA_GateAcquirePauseToken() => QA_GateAcquirePauseToken();

        [ContextMenu("QA Gate/Release Pause Token (NO FSM, NO timeScale)")]
        private void CM_QA_GateReleasePauseToken() => QA_GateReleasePauseToken();

        [ContextMenu("QA Gate/Toggle Pause Token (NO FSM, NO timeScale)")]
        private void CM_QA_GateTogglePauseToken() => QA_GateTogglePauseToken();

        // ---------
        // Reset IN-PLACE (sem GameResetRequestedEvent / sem MenuState / sem SceneTransition)
        // ---------

        [ContextMenu("QA/Reset IN-PLACE (AllActorsInScene)")]
        private void CM_ResetInPlace_AllActors() => _ = ResetInPlaceAsync(ResetScope.AllActorsInScene, "QA ContextMenu");

        [ContextMenu("QA/Reset IN-PLACE (PlayersOnly)")]
        private void CM_ResetInPlace_PlayersOnly() => _ = ResetInPlaceAsync(ResetScope.PlayersOnly, "QA ContextMenu");

        [ContextMenu("QA/Reset IN-PLACE (EaterOnly)")]
        private void CM_ResetInPlace_EaterOnly() => _ = ResetInPlaceAsync(ResetScope.EaterOnly, "QA ContextMenu");

        // =========================
        // Public API (UI/Buttons)
        // =========================

        #region Scene Flow (QA)

        public void GoToMenu()
        {
            LogDiagnostics("[MenuContext] GoToMenu (before)");

            var gm = ResolveGameManager();
            if (gm == null)
            {
                DebugUtility.LogWarning<MenuContextController>("[MenuContext] GoToMenu ignorado: GameManager null.");
                return;
            }

            DebugUtility.LogVerbose<MenuContextController>("[MenuContext] GoToMenu (QA_GoToMenuFromAnywhere).");
            gm.QA_GoToMenuFromAnywhere();

            LogDiagnostics("[MenuContext] GoToMenu (after request)");
        }

        public void GoToGameplay()
        {
            LogDiagnostics("[MenuContext] GoToGameplay (before)");

            var gm = ResolveGameManager();
            if (gm == null)
            {
                DebugUtility.LogWarning<MenuContextController>("[MenuContext] GoToGameplay ignorado: GameManager null.");
                return;
            }

            DebugUtility.LogVerbose<MenuContextController>("[MenuContext] GoToGameplay (QA_GoToGameplayFromAnywhere).");
            gm.QA_GoToGameplayFromAnywhere();

            LogDiagnostics("[MenuContext] GoToGameplay (after request)");
        }

        #endregion

        #region Pause via FSM (estado + timeScale)

        public void RequestPause()
        {
            LogDiagnostics("[MenuContext] RequestPause (before)");

            DebugUtility.LogVerbose<MenuContextController>(
                "[MenuContext] RequestPause (GamePauseRequestedEvent) => FSM deve entrar em Paused e ajustar timeScale conforme política do estado.");

            EventBus<GamePauseRequestedEvent>.Raise(new GamePauseRequestedEvent());

            LogDiagnostics("[MenuContext] RequestPause (after request)");
        }

        public void RequestResume()
        {
            LogDiagnostics("[MenuContext] RequestResume (before)");

            DebugUtility.LogVerbose<MenuContextController>(
                "[MenuContext] RequestResume (GameResumeRequestedEvent) => FSM deve sair de Paused e restaurar timeScale conforme política do estado.");

            EventBus<GameResumeRequestedEvent>.Raise(new GameResumeRequestedEvent());

            LogDiagnostics("[MenuContext] RequestResume (after request)");
        }

        #endregion

        #region Soft Reset (MACRO via FSM/EventBus)

        /// <summary>
        /// Reset MACRO: dispara GameResetRequestedEvent.
        /// Isso é esperado acionar MenuContext/GameManager/SceneTransition (fade/loading) dependendo do seu fluxo.
        /// Use isso para testar transição e fluxo "full reset".
        /// Para validar o sistema novo (ResetOrchestratorBehaviour), use Reset IN-PLACE.
        /// </summary>
        public void SoftReset()
        {
            LogDiagnostics("[MenuContext] SoftReset (before)");

            DebugUtility.LogVerbose<MenuContextController>("[MenuContext] SoftReset (GameResetRequestedEvent) [MACRO].");
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());

            LogDiagnostics("[MenuContext] SoftReset (after request)");
        }

        #endregion

        #region Reset IN-PLACE (Orchestrator)

        /// <summary>
        /// Reset IN-PLACE: chama o IResetOrchestrator diretamente (scene-scoped).
        /// NÃO dispara GameResetRequestedEvent e portanto NÃO deve entrar em MenuState/Fade/SceneTransition.
        /// </summary>
        public async Task<bool> ResetInPlaceAsync(ResetScope scope, string reason = "Manual")
        {
            LogDiagnostics($"[MenuContext] ResetInPlace (before) | Scope={scope}");

            if (string.IsNullOrWhiteSpace(gameplaySceneNameForOrchestrator))
            {
                DebugUtility.LogError<MenuContextController>(
                    "[MenuContext] gameplaySceneNameForOrchestrator vazio. Configure no Inspector.");
                return false;
            }

            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                DebugUtility.LogError<MenuContextController>(
                    "[MenuContext] DependencyManager.Provider null. Não é possível resolver IResetOrchestrator.");
                return false;
            }

            if (!provider.TryGetForScene<IResetOrchestrator>(gameplaySceneNameForOrchestrator, out var orchestrator) ||
                orchestrator == null)
            {
                DebugUtility.LogError<MenuContextController>(
                    $"[MenuContext] IResetOrchestrator não encontrado para a cena '{gameplaySceneNameForOrchestrator}'. " +
                    "Confirme que ResetOrchestratorBehaviour está na GameplayScene e registrou no Awake.");
                return false;
            }

            if (logResetInPlaceVerbose)
            {
                DebugUtility.LogVerbose<MenuContextController>(
                    $"[MenuContext] Reset IN-PLACE => RequestResetAsync | Scene='{gameplaySceneNameForOrchestrator}' | Scope={scope} | Reason='{reason}'");
            }

            bool ok = await orchestrator.RequestResetAsync(new ResetRequest(scope, $"MenuContext ResetInPlace: {reason}"));

            if (logResetInPlaceVerbose)
            {
                DebugUtility.LogVerbose<MenuContextController>(
                    $"[MenuContext] Reset IN-PLACE завершён | ok={ok} | Scope={scope}");
            }

            // Requisito: timer só reinicia quando reinicia a fase inteira (AllActorsInScene).
            if (ok && resetTimerOnInPlaceAllActors && scope == ResetScope.AllActorsInScene)
            {
                var timer = GameTimer.Instance;
                if (timer != null)
                {
                    timer.ResetToConfiguredDuration("MenuContext ResetInPlace: Full reset (AllActorsInScene)");
                    DebugUtility.LogVerbose<MenuContextController>(
                        "[MenuContext] Full Reset IN-PLACE: GameTimer resetado para duração configurada.");
                }
                else
                {
                    DebugUtility.LogWarning<MenuContextController>(
                        "[MenuContext] Full Reset IN-PLACE: GameTimer.Instance null; não foi possível resetar timer.");
                }
            }

            LogDiagnostics("[MenuContext] ResetInPlace (after)");
            return ok;
        }

        #endregion

        #region Force States

        public void ForceGameOver() => ForceGameOver(defaultGameOverReason);
        public void ForceVictory() => ForceVictory(defaultVictoryReason);

        public void ForceGameOver(string reason)
        {
            LogDiagnostics("[MenuContext] ForceGameOver (before)");

            var gm = ResolveGameManager();
            if (gm == null)
            {
                DebugUtility.LogWarning<MenuContextController>("[MenuContext] ForceGameOver ignorado: GameManager null.");
                return;
            }

            DebugUtility.LogVerbose<MenuContextController>($"[MenuContext] ForceGameOver ({reason}).");
            gm.QA_ForceGameOverFromAnywhere(reason);

            LogDiagnostics("[MenuContext] ForceGameOver (after request)");
        }

        public void ForceVictory(string reason)
        {
            LogDiagnostics("[MenuContext] ForceVictory (before)");

            var gm = ResolveGameManager();
            if (gm == null)
            {
                DebugUtility.LogWarning<MenuContextController>("[MenuContext] ForceVictory ignorado: GameManager null.");
                return;
            }

            DebugUtility.LogVerbose<MenuContextController>($"[MenuContext] ForceVictory ({reason}).");
            gm.QA_ForceVictoryFromAnywhere(reason);

            LogDiagnostics("[MenuContext] ForceVictory (after request)");
        }

        #endregion

        #region QA Gate-only (sem FSM, sem timeScale)

        public void QA_GateAcquirePauseToken()
        {
            LogDiagnostics("[MenuContext][QA Gate] Acquire (before)");

            if (!TryResolveGateService())
            {
                DebugUtility.LogWarning<MenuContextController>(
                    "[MenuContext][QA Gate] ISimulationGateService não encontrado no DI.");
                return;
            }

            if (_qaGateTokenHeld)
            {
                DebugUtility.LogVerbose<MenuContextController>(
                    $"[MenuContext][QA Gate] Token '{qaPauseGateToken}' já está ativo.");
                return;
            }

            _gateService.Acquire(qaPauseGateToken);
            _qaGateTokenHeld = true;

            DebugUtility.LogVerbose<MenuContextController>(
                $"[MenuContext][QA Gate] Acquire token '{qaPauseGateToken}' (Gate fecha; timeScale permanece {Time.timeScale:0.###}).");

            LogDiagnostics("[MenuContext][QA Gate] Acquire (after)");
        }

        public void QA_GateReleasePauseToken()
        {
            LogDiagnostics("[MenuContext][QA Gate] Release (before)");

            if (!TryResolveGateService())
            {
                DebugUtility.LogWarning<MenuContextController>(
                    "[MenuContext][QA Gate] ISimulationGateService não encontrado no DI.");
                return;
            }

            if (!_qaGateTokenHeld)
            {
                DebugUtility.LogVerbose<MenuContextController>(
                    $"[MenuContext][QA Gate] Token '{qaPauseGateToken}' não está ativo.");
                return;
            }

            _gateService.Release(qaPauseGateToken);
            _qaGateTokenHeld = false;

            DebugUtility.LogVerbose<MenuContextController>(
                $"[MenuContext][QA Gate] Release token '{qaPauseGateToken}' (Gate abre; timeScale permanece {Time.timeScale:0.###}).");

            LogDiagnostics("[MenuContext][QA Gate] Release (after)");
        }

        public void QA_GateTogglePauseToken()
        {
            if (_qaGateTokenHeld) QA_GateReleasePauseToken();
            else QA_GateAcquirePauseToken();
        }

        #endregion
    }
}
