#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Dev
{
    public sealed class LevelFlowDevContextMenu : MonoBehaviour
    {
        private const string ColorInfo = "#A8DEED";
        private const string ColorWarn = "#FFC107";
        private const string ColorError = "#F44336";

        private const string ReasonNextInMacro = "QA/LevelFlow/SwapLocal/NextInMacro";
        private const string ReasonToTarget = "QA/LevelFlow/SwapLocal/ToTargetLevelId";
        private const string ReasonProofNoMacroTransition = "QA/LevelFlow/SwapLocal/ProofNoMacroTransition";
        private const string ReasonSwapToLevel1 = "QA/LevelFlow/SwapLocal/ToLevel1";
        private const string ReasonSwapToLevel2 = "QA/LevelFlow/SwapLocal/ToLevel2";
        private const string ReasonBaselineNTo1StepA = "QA/Baseline3/LevelSwap/N_to_1/StepA_level2";
        private const string ReasonBaselineNTo1StepB = "QA/Baseline3/LevelSwap/N_to_1/StepB_level1";
        private const string ReasonResetCurrent = "QA/LevelFlow/ResetCurrent";
        private const string ReasonQaNextLevel = "QA/LevelFlow/NextLevel";
        private const string ReasonStartGameplayNoLevelId = "QA/LevelFlow/StartGameplay_NoLevelId";

        [SerializeField] private string targetLevelId = "level.1";
        [SerializeField] private string proofSwapLevelId = "level.2";
        [SerializeField] private bool wrapToFirst = true;

        [ContextMenu("QA/LevelFlow/SwapLocal/NextInMacro")]
        private void Qa_SwapLocal_NextInMacro()
        {
            _ = SwapNextInMacroAsync();
        }

        [ContextMenu("QA/LevelFlow/SwapLocal/ToTargetLevelId")]
        private void Qa_SwapLocal_ToTargetLevelId()
        {
            _ = SwapToTargetLevelIdAsync();
        }

        [ContextMenu("QA/LevelFlow/SwapLocal/ProofNoMacroTransition->level.2")]
        private void Qa_SwapLocal_ProofNoMacroTransition()
        {
            _ = SwapToProofLevelAsync();
        }

        [ContextMenu("QA/Smoke/Baseline 3.0 Levels/D. Swap to L2")]
        private void Qa_Smoke_B3_D_SwapToL2()
        {
            _ = SwapToLevelAsync("level.2", "QA/Smoke/Baseline3/Levels/D/SwapToL2");
        }

        [ContextMenu("QA/Smoke/Baseline 3.0 Levels/E. LevelReset (stay on L2)")]
        private void Qa_Smoke_B3_E_LevelReset()
        {
            _ = ResetCurrentLevelWithReasonAsync("QA/Smoke/Baseline3/Levels/E/LevelReset");
        }

        [ContextMenu("QA/Smoke/Baseline 3.0 Levels/D+E. Swap L2 + LevelReset")]
        private void Qa_Smoke_B3_DE_SwapAndReset()
        {
            _ = RunSmokeSwapAndResetAsync();
        }

        [ContextMenu("QA/LevelFlow/SwapLocal_ToLevel1")]
        private void Qa_SwapLocal_ToLevel1()
        {
            _ = SwapToLevelAsync("level.1", ReasonSwapToLevel1);
        }

        private void Qa_SwapLocal_ToLevel2()
        {
            _ = SwapToLevelAsync("level.2", ReasonSwapToLevel2);
        }

        [ContextMenu("QA/LevelFlow/Run_Baseline3_LevelSwap_N_to_1")]
        private void Qa_Run_Baseline3_LevelSwap_N_to_1()
        {
            _ = RunBaseline3LevelSwapNTo1Async();
        }

        private void Qa_ResetCurrentLevel()
        {
            _ = ResetCurrentLevelAsync();
        }

        [ContextMenu("QA/LevelFlow/NextLevel")]
        private void Qa_NextLevel()
        {
            _ = NextLevelAsync();
        }

        [ContextMenu("QA/LevelFlow/StartGameplay_NoLevelId")]
        private void Qa_StartGameplay_NoLevelId()
        {
            _ = StartGameplayNoLevelIdAsync();
        }

        private async Task StartGameplayNoLevelIdAsync()
        {
            var navigation = ResolveGlobal<IGameNavigationService>("IGameNavigationService");
            if (navigation == null)
            {
                return;
            }

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[OBS][QA][LevelFlow] StartGameplay_NoLevelId reason='{ReasonStartGameplayNoLevelId}'.",
                ColorInfo);

            try
            {
                await navigation.NavigateAsync(GameNavigationIntentKind.Gameplay, ReasonStartGameplayNoLevelId);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] StartGameplay_NoLevelId success=False reason='{ReasonStartGameplayNoLevelId}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
        }

        private async Task NextLevelAsync()
        {
            var postLevelActions = ResolveGlobal<IPostLevelActionsService>("IPostLevelActionsService");
            if (postLevelActions == null)
            {
                return;
            }

            int transitionStartedDuringNextLevel = 0;
            var transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(_ => transitionStartedDuringNextLevel++);
            EventBus<SceneTransitionStartedEvent>.Register(transitionStartedBinding);

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[OBS][PostLevel] PostLevelActionNextLevelRequested reason='{ReasonQaNextLevel}'.",
                ColorInfo);

            try
            {
                await postLevelActions.NextLevelAsync(ReasonQaNextLevel, CancellationToken.None);
                bool noMacroTransition = transitionStartedDuringNextLevel == 0;
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] NextLevelCompleted reason='{ReasonQaNextLevel}' noMacroTransition='{noMacroTransition.ToString().ToLowerInvariant()}' transitionStartedCount='{transitionStartedDuringNextLevel}'.",
                    noMacroTransition ? ColorInfo : ColorWarn);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] NextLevelCompleted success=False reason='{ReasonQaNextLevel}' transitionStartedCount='{transitionStartedDuringNextLevel}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(transitionStartedBinding);
            }
        }

        private async Task RunBaseline3LevelSwapNTo1Async()
        {
            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                "[OBS][QA][LevelFlow] Baseline3RunRequested scenario='LevelSwap_N_to_1' steps='level.2->level.1'.",
                ColorInfo);

            await SwapToLevelAsync("level.2", ReasonBaselineNTo1StepA);
            await SwapToLevelAsync("level.1", ReasonBaselineNTo1StepB);

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                "[OBS][QA][LevelFlow] Baseline3RunCompleted scenario='LevelSwap_N_to_1' steps='level.2->level.1'.",
                ColorInfo);
        }

        private async Task ResetCurrentLevelAsync()
        {
            var levelFlow = ResolveGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            if (levelFlow == null)
            {
                return;
            }

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[OBS][QA][LevelFlow] LevelResetRequested reason='{ReasonResetCurrent}'.",
                ColorInfo);

            try
            {
                await levelFlow.ResetCurrentLevelAsync(ReasonResetCurrent, CancellationToken.None);
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] LevelResetCompleted reason='{ReasonResetCurrent}'.",
                    ColorInfo);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] LevelResetCompleted success=False reason='{ReasonResetCurrent}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
        }

        private async Task SwapNextInMacroAsync()
        {
            var levelFlow = ResolveGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            var restartContext = ResolveGlobal<IRestartContextService>("IRestartContextService");
            var macroCatalog = ResolveGlobal<ILevelMacroRouteCatalog>("ILevelMacroRouteCatalog");
            if (levelFlow == null || restartContext == null || macroCatalog == null)
            {
                return;
            }

            if (!restartContext.TryGetLastGameplayStartSnapshot(out var snapshot) || !snapshot.IsValid || !snapshot.HasLevelId)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    "[WARN][QA][LevelFlow] SwapLocal skipped reason='no_snapshot'.",
                    ColorWarn);
                return;
            }

            if (!macroCatalog.TryGetNextLevelInMacro(snapshot.LevelId, out var nextLevelId, wrapToFirst) || !nextLevelId.IsValid)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocal skipped reason='no_next_in_macro' currentLevelId='{snapshot.LevelId}' wrapToFirst={wrapToFirst}.",
                    ColorWarn);
                return;
            }

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[INFO][QA][LevelFlow] SwapLocalRequested mode='next_in_macro' fromLevelId='{snapshot.LevelId}' toLevelId='{nextLevelId}' reason='{ReasonNextInMacro}'.",
                ColorInfo);

            try
            {
                await levelFlow.SwapLevelLocalAsync(nextLevelId, ReasonNextInMacro, CancellationToken.None);
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[INFO][QA][LevelFlow] SwapLocalCompleted mode='next_in_macro' toLevelId='{nextLevelId}' reason='{ReasonNextInMacro}'.",
                    ColorInfo);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocalCompleted mode='next_in_macro' success=False toLevelId='{nextLevelId}' reason='{ReasonNextInMacro}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
        }

        private async Task SwapToTargetLevelIdAsync()
        {
            var levelFlow = ResolveGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            if (levelFlow == null)
            {
                return;
            }

            LevelId levelId = LevelId.FromName(targetLevelId);
            if (!levelId.IsValid)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocal skipped reason='invalid_target_level' targetLevelId='{targetLevelId ?? "<null>"}'.",
                    ColorWarn);
                return;
            }

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[INFO][QA][LevelFlow] SwapLocalRequested mode='target_level' toLevelId='{levelId}' reason='{ReasonToTarget}'.",
                ColorInfo);

            try
            {
                await levelFlow.SwapLevelLocalAsync(levelId, ReasonToTarget, CancellationToken.None);
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[INFO][QA][LevelFlow] SwapLocalCompleted mode='target_level' toLevelId='{levelId}' reason='{ReasonToTarget}'.",
                    ColorInfo);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocalCompleted mode='target_level' success=False toLevelId='{levelId}' reason='{ReasonToTarget}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
        }

        private async Task SwapToProofLevelAsync()
        {
            var swapLocalService = ResolveGlobal<ILevelSwapLocalService>("ILevelSwapLocalService");
            if (swapLocalService == null)
            {
                return;
            }

            LevelId levelId = LevelId.FromName(proofSwapLevelId);
            if (!levelId.IsValid)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocal skipped reason='invalid_proof_level' proofSwapLevelId='{proofSwapLevelId ?? "<null>"}'.",
                    ColorWarn);
                return;
            }

            int transitionStartedDuringSwap = 0;
            var transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(_ => transitionStartedDuringSwap++);

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[OBS][QA][LevelFlow] SwapLocalRequested mode='proof_no_macro_transition' toLevelId='{levelId}' reason='{ReasonProofNoMacroTransition}'.",
                ColorInfo);

            EventBus<SceneTransitionStartedEvent>.Register(transitionStartedBinding);

            try
            {
                await swapLocalService.SwapLocalAsync(levelId, ReasonProofNoMacroTransition, CancellationToken.None);

                bool noMacroTransition = transitionStartedDuringSwap == 0;
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] SwapLocalCompleted mode='proof_no_macro_transition' toLevelId='{levelId}' reason='{ReasonProofNoMacroTransition}' noMacroTransition='{noMacroTransition.ToString().ToLowerInvariant()}' transitionStartedCount='{transitionStartedDuringSwap}'.",
                    noMacroTransition ? ColorInfo : ColorWarn);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocalCompleted mode='proof_no_macro_transition' success=False toLevelId='{levelId}' reason='{ReasonProofNoMacroTransition}' transitionStartedCount='{transitionStartedDuringSwap}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
            finally
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(transitionStartedBinding);
            }
        }

        private async Task SwapToLevelAsync(string levelName, string reason)
        {
            var swapLocalService = ResolveGlobal<ILevelSwapLocalService>("ILevelSwapLocalService");
            if (swapLocalService == null)
            {
                return;
            }

            LevelId levelId = LevelId.FromName(levelName);
            if (!levelId.IsValid)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocal skipped reason='invalid_level' levelId='{levelName ?? "<null>"}'.",
                    ColorWarn);
                return;
            }

            DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                $"[OBS][QA][LevelFlow] SwapLocalRequested mode='direct' toLevelId='{levelId}' reason='{reason}'.",
                ColorInfo);

            try
            {
                await swapLocalService.SwapLocalAsync(levelId, reason, CancellationToken.None);
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[OBS][QA][LevelFlow] SwapLocalCompleted mode='direct' toLevelId='{levelId}' reason='{reason}'.",
                    ColorInfo);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] SwapLocalCompleted mode='direct' success=False toLevelId='{levelId}' reason='{reason}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
        }

        private async Task ResetCurrentLevelWithReasonAsync(string reason)
        {
            var levelFlow = ResolveGlobal<ILevelFlowRuntimeService>("ILevelFlowRuntimeService");
            if (levelFlow == null)
            {
                return;
            }

            try
            {
                await levelFlow.ResetCurrentLevelAsync(reason, CancellationToken.None);
            }
            catch (System.Exception ex)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] LevelResetCompleted success=False reason='{reason}' notes='{ex.GetType().Name}'.",
                    ColorWarn);
            }
        }

        private async Task RunSmokeSwapAndResetAsync()
        {
            await SwapToLevelAsync("level.2", "QA/Smoke/Baseline3/Levels/DE/D");
            await ResetCurrentLevelWithReasonAsync("QA/Smoke/Baseline3/Levels/DE/E");
        }

        private static T? ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    "[ERROR][QA][LevelFlow] DependencyManager.Provider is null.",
                    ColorError);
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.Log(typeof(LevelFlowDevContextMenu),
                    $"[WARN][QA][LevelFlow] Missing global service='{label}'.",
                    ColorWarn);
                return null;
            }

            return service;
        }
    }
}
