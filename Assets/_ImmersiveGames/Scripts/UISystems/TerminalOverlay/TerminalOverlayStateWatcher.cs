using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.StateMachineSystems.GameStates;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.Scripts.UISystems.TerminalOverlay
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class TerminalOverlayStateWatcher : MonoBehaviour
    {
        [SerializeField] private TerminalOverlayController overlay;

        private System.Type _lastStateType;

        private void Awake()
        {
            if (overlay == null)
                overlay = GetComponentInChildren<TerminalOverlayController>(true);

            SnapshotState("Awake");
        }

        private void Update()
        {
            var state = OldGameManagerStateMachine.Instance != null ? OldGameManagerStateMachine.Instance.CurrentState : null;
            var currentType = state?.GetType();

            if (currentType == _lastStateType)
                return;

            _lastStateType = currentType;
            ApplyOverlayForState(state);
        }

        private void ApplyOverlayForState(object state)
        {
            if (overlay == null)
                return;

            // Terminal states => mostra overlay.
            if (state is OldVictoryState)
            {
                overlay.ShowVictory();
                return;
            }

            if (state is OldGameOverState)
            {
                overlay.ShowGameOver();
                return;
            }

            // Qualquer outro estado => garante que o terminal overlay não fique “travado”.
            overlay.Hide();
        }

        private void SnapshotState(string label)
        {
            var state = OldGameManagerStateMachine.Instance != null ? OldGameManagerStateMachine.Instance.CurrentState : null;
            _lastStateType = state?.GetType();
            DebugUtility.LogVerbose<TerminalOverlayStateWatcher>(
                $"[TerminalOverlayWatcher] {label} | State='{_lastStateType?.Name ?? "null"}'");
        }
    }
}


