#nullable enable
using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Gameplay.Scene;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// QA helper para:
    /// - Disparar Pregame manualmente (sem depender do SceneFlow).
    /// - Forçar Complete/Skip do Pregame (o gatilho canônico que destrava o gameplay).
    /// - (Opcional) Auto-Complete com delay usando Coroutine (main thread), evitando Task/threads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PregameQATester : MonoBehaviour
    {
        [Header("Run Pregame QA")]
        [SerializeField] private string qaSignature = "qa.pregame";
        [SerializeField] private string qaReason = "QA/PregameOptional";

        [Header("Completion QA")]
        [SerializeField, Min(0f)] private float autoCompleteDelaySeconds = 0.5f;

        private Coroutine? _autoCompleteRoutine;

        [ContextMenu("QA/Pregame/Run Optional (TestCase: PregameOptional)")]
        private async void QA_RunPregameOptional()
        {
            var coordinator = ResolveCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    "[QA][Pregame] IPregameCoordinator não encontrado no DI global.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene().name;
            var context = new PregameContext(
                contextSignature: qaSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeScene,
                reason: qaReason);

            try
            {
                DebugUtility.Log<PregameQATester>(
                    $"[QA][Pregame] RunPregameOptional solicitado. signature='{qaSignature}' scene='{activeScene}'.",
                    DebugUtility.Colors.Info);

                await coordinator.RunPregameAsync(context);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    $"[QA][Pregame] Falha ao executar pregame QA. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        [ContextMenu("QA/Pregame/Complete Active (Force)")]
        private void QA_CompleteActivePregame_Force()
        {
            CancelAutoCompleteIfRunning();

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    "[QA][Pregame] IPregameControlService não encontrado no DI global; Complete ignorado.");
                return;
            }

            control.CompletePregame("QA/PregameQATester/Complete");
            DebugUtility.Log<PregameQATester>(
                "[QA][Pregame] CompletePregame solicitado (Force).",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/Pregame/Skip Active (Force)")]
        private void QA_SkipActivePregame_Force()
        {
            CancelAutoCompleteIfRunning();

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    "[QA][Pregame] IPregameControlService não encontrado no DI global; Skip ignorado.");
                return;
            }

            control.SkipPregame("QA/PregameQATester/Skip");
            DebugUtility.Log<PregameQATester>(
                "[QA][Pregame] SkipPregame solicitado (Force).",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/Pregame/Auto-Complete in 0.5s (Force)")]
        private void QA_AutoCompleteActivePregame_Force()
        {
            CancelAutoCompleteIfRunning();
            _autoCompleteRoutine = StartCoroutine(AutoCompleteRoutine());
        }

        private IEnumerator AutoCompleteRoutine()
        {
            var delay = Mathf.Max(0f, autoCompleteDelaySeconds);
            DebugUtility.Log<PregameQATester>(
                $"[QA][Pregame] Auto-Complete agendado. delay='{delay:0.###}s'.",
                DebugUtility.Colors.Info);

            yield return new WaitForSecondsRealtime(delay);

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<PregameQATester>(
                    "[QA][Pregame] IPregameControlService indisponível; Auto-Complete abortado.");
                _autoCompleteRoutine = null;
                yield break;
            }

            control.CompletePregame("QA/PregameQATester/AutoComplete");
            DebugUtility.Log<PregameQATester>(
                "[QA][Pregame] Auto-Complete executado (CompletePregame).",
                DebugUtility.Colors.Info);

            _autoCompleteRoutine = null;
        }

        private void CancelAutoCompleteIfRunning()
        {
            if (_autoCompleteRoutine != null)
            {
                StopCoroutine(_autoCompleteRoutine);
                _autoCompleteRoutine = null;
            }
        }

        private static IPregameCoordinator? ResolveCoordinator()
        {
            return DependencyManager.Provider.TryGetGlobal<IPregameCoordinator>(out var coordinator)
                ? coordinator
                : null;
        }

        private static IPregameControlService? ResolveControlService()
        {
            return DependencyManager.Provider.TryGetGlobal<IPregameControlService>(out var control)
                ? control
                : null;
        }
    }
}
