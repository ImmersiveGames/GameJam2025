#if UNITY_EDITOR || DEVELOPMENT_BUILD

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime
{
    public partial class SceneRouteResetPolicy
    {
        static partial void StopPlayModeInEditor()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }
    }
}
#endif
