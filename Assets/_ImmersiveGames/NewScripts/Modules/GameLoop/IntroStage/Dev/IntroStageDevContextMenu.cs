#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev
{
    public sealed class IntroStageDevContextMenu : MonoBehaviour
    {
        private const int AwaitTimeoutMs = 2000;

        [ContextMenu("QA/IntroStageController/Dump - Status")]
        private void DumpStatus()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                return;
            }

            DebugUtility.Log(typeof(IntroStageDevContextMenu),
                $"[QA][IntroStageController] Status snapshot isActive={controlService.IsIntroStageActive.ToString().ToLowerInvariant()}.",
                DebugUtility.Colors.Info);
        }

        [ContextMenu("QA/IntroStageController/Complete (Force)")]
        private void CompleteIntroStage()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                return;
            }

            DebugUtility.Log(typeof(IntroStageDevContextMenu),
                "[QA][IntroStageController] CompleteIntroStage solicitado.",
                DebugUtility.Colors.Info);

            controlService.CompleteIntroStage("QA/IntroStageController/Complete");
        }

        [ContextMenu("QA/IntroStageController/Skip (Force)")]
        private void SkipIntroStage()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                return;
            }

            DebugUtility.Log(typeof(IntroStageDevContextMenu),
                "[QA][IntroStageController] SkipIntroStage solicitado.",
                DebugUtility.Colors.Info);

            controlService.SkipIntroStage("QA/IntroStageController/Skip");
        }

        [ContextMenu("QA/IntroStageController/Complete + Await Result (2s)")]
        private void CompleteIntroStageAwait()
        {
            _ = CompleteAwaitAsync(skip: false);
        }

        [ContextMenu("QA/IntroStageController/Skip + Await Result (2s)")]
        private void SkipIntroStageAwait()
        {
            _ = CompleteAwaitAsync(skip: true);
        }

        private async Task CompleteAwaitAsync(bool skip)
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                return;
            }

            DebugUtility.Log(typeof(IntroStageDevContextMenu),
                $"[QA][IntroStageController] {(skip ? "Skip" : "Complete")} solicitado + aguardando resultado (timeout={AwaitTimeoutMs}ms).",
                DebugUtility.Colors.Info);

            try
            {
                using var cts = new CancellationTokenSource(AwaitTimeoutMs);

                if (skip)
                {
                    controlService.SkipIntroStage("QA/IntroStageController/Skip");
                }
                else
                {
                    controlService.CompleteIntroStage("QA/IntroStageController/Complete");
                }

                var result = await controlService.WaitForCompletionAsync(cts.Token);

                // Evita depender de propriedades específicas do tipo.
                DebugUtility.Log(typeof(IntroStageDevContextMenu),
                    $"[QA][IntroStageController] Await result => '{result}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(IntroStageDevContextMenu),
                    $"[QA][IntroStageController] Await FAILED ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static IIntroStageControlService? ResolveControlService()
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogError(typeof(IntroStageDevContextMenu),
                    "[QA][IntroStageController] DependencyManager.Provider é null (infra global não inicializada?).");
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(IntroStageDevContextMenu),
                    "[QA][IntroStageController] IIntroStageControlService indisponível; ação ignorada.");
                return null;
            }

            return service;
        }
    }
}
