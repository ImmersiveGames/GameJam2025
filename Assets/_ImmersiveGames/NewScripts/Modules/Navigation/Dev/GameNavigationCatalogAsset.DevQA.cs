#if UNITY_EDITOR || DEVELOPMENT_BUILD

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    public partial class GameNavigationCatalogAsset
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
