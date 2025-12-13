using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

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

        [ContextMenu("QA/Soft Reset (FSM Request)")]
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

        #region Soft Reset (via FSM)

        public void SoftReset()
        {
            LogDiagnostics("[MenuContext] SoftReset (before)");

            DebugUtility.LogVerbose<MenuContextController>("[MenuContext] SoftReset (GameResetRequestedEvent).");
            EventBus<GameResetRequestedEvent>.Raise(new GameResetRequestedEvent());

            LogDiagnostics("[MenuContext] SoftReset (after request)");
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
