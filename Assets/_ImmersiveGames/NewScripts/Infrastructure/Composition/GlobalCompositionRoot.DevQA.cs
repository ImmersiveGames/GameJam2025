using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Dev;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterIntroStageQaInstaller()
        {
            try
            {
                IntroStageDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][IntroStageController] Falha ao instalar IntroStageDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static void RegisterContentSwapQaInstaller()
        {
            try
            {
                ContentSwapDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][ContentSwap] Falha ao instalar ContentSwapDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

        private static void RegisterSceneFlowQaInstaller()
        {
            try
            {
                SceneFlowDevInstaller.EnsureInstalled();
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[QA][SceneFlow] Falha ao instalar SceneFlowDevContextMenu no bootstrap. ex='{ex.GetType().Name}: {ex.Message}'.");
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void RegisterIntroStageRuntimeDebugGui()
        {
            IntroStageRuntimeDebugGui.EnsureInstalled();
        }
#endif

        // --------------------------------------------------------------------
    }
}
