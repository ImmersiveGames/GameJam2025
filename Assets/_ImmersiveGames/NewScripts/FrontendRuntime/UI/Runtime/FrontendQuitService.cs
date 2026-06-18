using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEditor;
namespace _ImmersiveGames.NewScripts.FrontendRuntime.UI.Runtime
{
    public interface IFrontendQuitService
    {
        void Quit(string reason);
    }

    public sealed class FrontendQuitService : IFrontendQuitService
    {
        public void Quit(string reason)
        {
            string normalizedReason = reason.TrimToOrDefault("FrontendUI/Quit");

#if UNITY_EDITOR
            DebugUtility.Log(typeof(FrontendQuitService),
                $"Quit executado no Editor. Stopping Play Mode. reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            EditorApplication.isPlaying = false;
#else
            DebugUtility.Log(typeof(FrontendQuitService),
                $"Quit executado em build. Application.Quit() reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            Application.Quit();
#endif
        }
    }
}

