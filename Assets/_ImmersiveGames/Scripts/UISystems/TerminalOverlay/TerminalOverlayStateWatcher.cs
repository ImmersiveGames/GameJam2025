using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems.TerminalOverlay
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class TerminalOverlayStateWatcher : MonoBehaviour
    {
        [SerializeField] private TerminalOverlayController overlay;

        private Type _lastStateType;

        private void Awake()
        {
            if (overlay == null)
            {
                overlay = GetComponentInChildren<TerminalOverlayController>(true);
            }

            SnapshotState("Awake");
        }

        private void Update()
        {
            object state = null;
            var currentType = state?.GetType();

            if (currentType == _lastStateType)
            {
                return;
            }

            _lastStateType = currentType;
            ApplyOverlayForState(state);
        }

        private void ApplyOverlayForState(object state)
        {
            if (overlay == null)
            {
                return;
            }


            // Qualquer outro estado => garante que o terminal overlay n�o fique �travado�.
            overlay.Hide();
        }

        private void SnapshotState(string label)
        {
            object state = null;
            _lastStateType = state?.GetType();
            DebugUtility.LogVerbose<TerminalOverlayStateWatcher>(
                $"[TerminalOverlayWatcher] {label} | State='{_lastStateType?.Name ?? "null"}'");
        }
    }
}
