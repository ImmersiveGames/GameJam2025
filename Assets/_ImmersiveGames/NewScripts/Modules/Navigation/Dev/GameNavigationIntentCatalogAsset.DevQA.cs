#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    public sealed partial class GameNavigationIntentCatalogAsset
    {
        static partial void DevStopPlayModeInEditor()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
#endif
