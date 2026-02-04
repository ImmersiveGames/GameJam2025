#nullable enable
using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop.IntroStage;
using _ImmersiveGames.NewScripts.Runtime.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.QA.IntroStage
{
    /// <summary>
    /// QA helper para:
    /// - Disparar IntroStageController manualmente (sem depender do SceneFlow).
    /// - Forçar Complete/Skip da IntroStageController (o gatilho canônico que destrava o gameplay).
    /// - (Opcional) Auto-Complete com delay usando Coroutine (main thread), evitando Task/threads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IntroStageQATester : MonoBehaviour
    {
        [Header("Run IntroStageController QA")]
        [SerializeField] private string qaSignature = "qa.introstage";
        [SerializeField] private string qaReason = "QA/IntroStageOptional";

        [Header("Completion QA")]
        [SerializeField, Min(0f)] private float autoCompleteDelaySeconds = 0.5f;

        private Coroutine? _autoCompleteRoutine;

        [ContextMenu("QA/IntroStageController/Run Optional (TestCase: IntroStageOptional)")]
        private async void QA_RunIntroStageOptional()
        {
            var coordinator = ResolveCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStageController] IIntroStageCoordinator não encontrado no DI global.");
                return;
            }

            string? activeScene = SceneManager.GetActiveScene().name;
            var context = new IntroStageContext(
                contextSignature: qaSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeScene,
                reason: qaReason);

            try
            {
                DebugUtility.Log<IntroStageQATester>(
                    $"[QA][IntroStageController] RunIntroStageOptional solicitado. signature='{qaSignature}' scene='{activeScene}'.",
                    DebugUtility.Colors.Info);

                await coordinator.RunIntroStageAsync(context);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    $"[QA][IntroStageController] Falha ao executar IntroStageController QA. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        [ContextMenu("QA/IntroStageController/Complete Active (Force)")]
        private void QA_CompleteActiveIntroStage_Force()
        {
            CancelAutoCompleteIfRunning();

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStageController] IIntroStageControlService não encontrado no DI global; Complete ignorado.");
                return;
            }

            control.CompleteIntroStage("QA/IntroStageQATester/Complete");
            DebugUtility.Log<IntroStageQATester>(
                "[QA][IntroStageController] CompleteIntroStage solicitado (Force).",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStageController/Skip Active (Force)")]
        private void QA_SkipActiveIntroStage_Force()
        {
            CancelAutoCompleteIfRunning();

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStageController] IIntroStageControlService não encontrado no DI global; Skip ignorado.");
                return;
            }

            control.SkipIntroStage("QA/IntroStageQATester/Skip");
            DebugUtility.Log<IntroStageQATester>(
                "[QA][IntroStageController] SkipIntroStage solicitado (Force).",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStageController/Auto-Complete in 0.5s (Force)")]
        private void QA_AutoCompleteActiveIntroStage_Force()
        {
            CancelAutoCompleteIfRunning();
            _autoCompleteRoutine = StartCoroutine(AutoCompleteRoutine());
        }

        private IEnumerator AutoCompleteRoutine()
        {
            float delay = Mathf.Max(0f, autoCompleteDelaySeconds);
            DebugUtility.Log<IntroStageQATester>(
                $"[QA][IntroStageController] Auto-Complete agendado. delay='{delay:0.###}s'.",
                DebugUtility.Colors.Info);

            yield return new WaitForSecondsRealtime(delay);

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStageController] IIntroStageControlService indisponível; Auto-Complete abortado.");
                _autoCompleteRoutine = null;
                yield break;
            }

            control.CompleteIntroStage("QA/IntroStageQATester/AutoComplete");
            DebugUtility.Log<IntroStageQATester>(
                "[QA][IntroStageController] Auto-Complete executado (CompleteIntroStage).",
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

        private static IIntroStageCoordinator? ResolveCoordinator()
        {
            return DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator)
                ? coordinator
                : null;
        }

        private static IIntroStageControlService? ResolveControlService()
        {
            return DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var control)
                ? control
                : null;
        }
    }
}


