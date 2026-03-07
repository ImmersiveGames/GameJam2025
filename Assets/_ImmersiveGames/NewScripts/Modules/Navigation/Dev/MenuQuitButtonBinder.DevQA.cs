#if UNITY_EDITOR || DEVELOPMENT_BUILD
using _ImmersiveGames.NewScripts.Modules.Navigation.Bindings;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Bindings
{
    public sealed partial class MenuQuitButtonBinder
    {
        partial void DevStopPlayModeInEditor()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
#endif
