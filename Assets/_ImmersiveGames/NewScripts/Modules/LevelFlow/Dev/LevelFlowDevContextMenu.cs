#nullable enable
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
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

        [SerializeField] private string targetLevelId = "level.1";
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
