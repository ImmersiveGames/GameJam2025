#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Gameplay.PostGame
{
    public partial class PostGameOverlayController
    {
        private bool _qaGuardBusy;

        private bool BeginQaRiskCommand(string commandName, string reason)
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Comando '{commandName}' requer Play Mode.");
                return false;
            }

#if UNITY_EDITOR
            if (!EditorUtility.DisplayDialog(
                    "QA Command",
                    $"{commandName}: comando forçado com risco de double-trigger/reentrada.\n\nReason: {reason}\n\nDeseja continuar?",
                    "Continuar",
                    "Cancelar"))
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Comando cancelado pelo usuário.");
                return false;
            }
#else
            DebugUtility.LogWarning<PostGameOverlayController>(
                $"[WARN][{reason}] Build sem diálogo de confirmação; executando comando.");
#endif

            if (_qaGuardBusy)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Comando ignorado por reentrância (_qaGuardBusy=true).");
                return false;
            }

            _qaGuardBusy = true;
            DebugUtility.Log<PostGameOverlayController>(
                $"[QA][PostGame] Executando comando '{commandName}'. reason='{reason}'.");
            return true;
        }

        private void EndQaRiskCommand(string reason)
        {
            _qaGuardBusy = false;
            DebugUtility.LogVerbose<PostGameOverlayController>(
                $"[QA][PostGame] Guard liberado. reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/Gameplay/PostGame/Force Restart x2 (same frame)")]
        private void QaForceRestartDoubleSameFrame()
        {
            const string reason = "QA/Gameplay/PostGame/Force Restart x2 (same frame)";
            if (!BeginQaRiskCommand("Force Restart x2 (same frame)", reason))
            {
                return;
            }

            try
            {
                OnClickRestart();
                OnClickRestart();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Falha no comando: {ex}");
            }
            finally
            {
                EndQaRiskCommand(reason);
            }
        }

        [ContextMenu("QA/Gameplay/PostGame/Force ExitToMenu x2 (same frame)")]
        private void QaForceExitToMenuDoubleSameFrame()
        {
            const string reason = "QA/Gameplay/PostGame/Force ExitToMenu x2 (same frame)";
            if (!BeginQaRiskCommand("Force ExitToMenu x2 (same frame)", reason))
            {
                return;
            }

            try
            {
                OnClickExitToMenu();
                OnClickExitToMenu();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Falha no comando: {ex}");
            }
            finally
            {
                EndQaRiskCommand(reason);
            }
        }

        [ContextMenu("QA/Gameplay/PostGame/Force Restart x2 (delay 0.05s)")]
        private void QaForceRestartDoubleWithDelay()
        {
            const string reason = "QA/Gameplay/PostGame/Force Restart x2 (delay 0.05s)";
            if (!BeginQaRiskCommand("Force Restart x2 (delay 0.05s)", reason))
            {
                return;
            }

            try
            {
                StartCoroutine(QaCoroutineDoubleWithGuard(() => OnClickRestart(), 0.05f, reason));
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Falha no comando: {ex}");
                EndQaRiskCommand(reason);
            }
        }

        [ContextMenu("QA/Gameplay/PostGame/Force ExitToMenu x2 (delay 0.05s)")]
        private void QaForceExitToMenuDoubleWithDelay()
        {
            const string reason = "QA/Gameplay/PostGame/Force ExitToMenu x2 (delay 0.05s)";
            if (!BeginQaRiskCommand("Force ExitToMenu x2 (delay 0.05s)", reason))
            {
                return;
            }

            try
            {
                StartCoroutine(QaCoroutineDoubleWithGuard(() => OnClickExitToMenu(), 0.05f, reason));
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PostGameOverlayController>(
                    $"[WARN][{reason}] Falha no comando: {ex}");
                EndQaRiskCommand(reason);
            }
        }

        private System.Collections.IEnumerator QaCoroutineDoubleWithGuard(Action action, float delaySeconds, string reason)
        {
            try
            {
                action?.Invoke();
                yield return new WaitForSeconds(delaySeconds);
                action?.Invoke();
            }
            finally
            {
                EndQaRiskCommand(reason);
            }
        }
    }
}
#endif
