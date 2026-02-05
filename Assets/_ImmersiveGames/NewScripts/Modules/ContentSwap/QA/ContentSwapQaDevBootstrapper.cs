// Assets/_ImmersiveGames/NewScripts/QA/ContentSwap/ContentSwapQaDevBootstrapper.cs
// Boot DEV para instalar o QA de ContentSwap automaticamente ao entrar em Play Mode.
// Comentários PT; código EN.

#nullable enable
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap.QA
{
    public static class ContentSwapQaDevBootstrapper
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

            ContentSwapQaInstaller.EnsureInstalled();
        }
#endif
    }
}
