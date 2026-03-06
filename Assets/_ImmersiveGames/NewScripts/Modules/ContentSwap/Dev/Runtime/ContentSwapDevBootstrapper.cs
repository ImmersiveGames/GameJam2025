#nullable enable
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Runtime
{
    public static class ContentSwapDevBootstrapper
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterSceneLoad()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // DQ-1.2: caminho paralelo desativado para manter um trilho canônico de instalação via GlobalCompositionRoot.DevQA.
            DebugUtility.LogVerbose(typeof(ContentSwapDevBootstrapper),
                "[OBS][LEGACY][DevQA] ContentSwapDevBootstrapper desativado; instalação canônica via GlobalCompositionRoot.DevQA.",
                DebugUtility.Colors.Info);
        }
#endif
    }
}
