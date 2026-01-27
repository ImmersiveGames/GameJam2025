#nullable enable
using System;
using System.Collections;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// QA helper para:
    /// - Disparar IntroStage manualmente (sem depender do SceneFlow).
    /// - Forçar Complete/Skip da IntroStage (o gatilho canônico que destrava o gameplay).
    /// - (Opcional) Auto-Complete com delay usando Coroutine (main thread), evitando Task/threads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IntroStageQATester : MonoBehaviour
    {
        [Header("Run IntroStage QA")]
        [SerializeField] private string qaSignature = "qa.introstage";
        [SerializeField] private string qaReason = "QA/IntroStageOptional";

        [Header("Completion QA")]
        [SerializeField, Min(0f)] private float autoCompleteDelaySeconds = 0.5f;

        private Coroutine? _autoCompleteRoutine;

        [ContextMenu("QA/IntroStage/Run Optional (TestCase: IntroStageOptional)")]
        private async void QA_RunIntroStageOptional()
        {
            var coordinator = ResolveCoordinator();
            if (coordinator == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStage] IIntroStageCoordinator não encontrado no DI global.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene().name;
            var context = new IntroStageContext(
                contextSignature: qaSignature,
                profileId: SceneFlowProfileId.Gameplay,
                targetScene: activeScene,
                reason: qaReason);

            try
            {
                DebugUtility.Log<IntroStageQATester>(
                    $"[QA][IntroStage] RunIntroStageOptional solicitado. signature='{qaSignature}' scene='{activeScene}'.",
                    DebugUtility.Colors.Info);

                await coordinator.RunIntroStageAsync(context);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    $"[QA][IntroStage] Falha ao executar IntroStage QA. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        [ContextMenu("QA/IntroStage/Complete Active (Force)")]
        private void QA_CompleteActiveIntroStage_Force()
        {
            CancelAutoCompleteIfRunning();

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStage] IIntroStageControlService não encontrado no DI global; Complete ignorado.");
                return;
            }

            control.CompleteIntroStage("QA/IntroStageQATester/Complete");
            DebugUtility.Log<IntroStageQATester>(
                "[QA][IntroStage] CompleteIntroStage solicitado (Force).",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStage/Skip Active (Force)")]
        private void QA_SkipActiveIntroStage_Force()
        {
            CancelAutoCompleteIfRunning();

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStage] IIntroStageControlService não encontrado no DI global; Skip ignorado.");
                return;
            }

            control.SkipIntroStage("QA/IntroStageQATester/Skip");
            DebugUtility.Log<IntroStageQATester>(
                "[QA][IntroStage] SkipIntroStage solicitado (Force).",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStage/Auto-Complete in 0.5s (Force)")]
        private void QA_AutoCompleteActiveIntroStage_Force()
        {
            CancelAutoCompleteIfRunning();
            _autoCompleteRoutine = StartCoroutine(AutoCompleteRoutine());
        }

        private IEnumerator AutoCompleteRoutine()
        {
            var delay = Mathf.Max(0f, autoCompleteDelaySeconds);
            DebugUtility.Log<IntroStageQATester>(
                $"[QA][IntroStage] Auto-Complete agendado. delay='{delay:0.###}s'.",
                DebugUtility.Colors.Info);

            yield return new WaitForSecondsRealtime(delay);

            var control = ResolveControlService();
            if (control == null)
            {
                DebugUtility.LogWarning<IntroStageQATester>(
                    "[QA][IntroStage] IIntroStageControlService indisponível; Auto-Complete abortado.");
                _autoCompleteRoutine = null;
                yield break;
            }

            control.CompleteIntroStage("QA/IntroStageQATester/AutoComplete");
            DebugUtility.Log<IntroStageQATester>(
                "[QA][IntroStage] Auto-Complete executado (CompleteIntroStage).",
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
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQATester),
                    "[QA][IntroStage] DependencyManager.Provider indisponível; coordinator não resolvido.");
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IIntroStageCoordinator>(out var coordinator)
                ? coordinator
                : null;
        }

        private static IIntroStageControlService? ResolveControlService()
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageQATester),
                    "[QA][IntroStage] DependencyManager.Provider indisponível; control service não resolvido.");
                return null;
            }

            return DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var control)
                ? control
                : null;
        }
    }
}
