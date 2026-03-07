#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges
{
    public sealed partial class GameLoopSceneFlowCoordinator
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
