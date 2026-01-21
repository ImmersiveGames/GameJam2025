// Assets/_ImmersiveGames/NewScripts/QA/Phases/PhaseQaDevBootstrapper.cs
// Boot DEV para instalar o QA de Phases automaticamente ao entrar em Play Mode.
// Comentários PT; código EN.

#nullable enable
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Phases
{
    public static class PhaseQaDevBootstrapper
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

            PhaseQaInstaller.EnsureInstalled();
        }
#endif
    }
}
