using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Frontend.UI.Runtime
{
    public interface IFrontendQuitService
    {
        void Quit(string reason);
    }

    public sealed class FrontendQuitService : IFrontendQuitService
    {
        public void Quit(string reason)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "FrontendUI/Quit" : reason.Trim();

#if UNITY_EDITOR
            DebugUtility.Log(typeof(FrontendQuitService),
                $"[OBS][Quit][Execute] Quit executado no Editor. Stopping Play Mode. reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            UnityEditor.EditorApplication.isPlaying = false;
#else
            DebugUtility.Log(typeof(FrontendQuitService),
                $"[OBS][Quit][Execute] Quit executado em build. Application.Quit() reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            Application.Quit();
#endif
        }
    }
}
