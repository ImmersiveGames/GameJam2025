#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime
{
    public sealed partial class SceneTransitionService
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
