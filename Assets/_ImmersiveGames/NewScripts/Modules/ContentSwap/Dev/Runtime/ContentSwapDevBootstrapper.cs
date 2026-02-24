#nullable enable
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.Dev.Runtime
{
    public static class ContentSwapDevBootstrapper
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallAfterSceneLoad()
        {
            // Garante que a infra esteja em Play Mode e evita mexer em produção.
            if (!Application.isPlaying)
            {
                return;
            }

            ContentSwapDevInstaller.EnsureInstalled();
        }
#endif
    }
}
