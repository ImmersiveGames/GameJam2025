using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Context
{
    /// <summary>
    /// Owner canônico da precedência contextual de BGM.
    /// </summary>
    public interface IAudioBgmContextService
    {
        void OnSceneTransitionStarted(SceneTransitionStartedEvent evt);

        void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt);
    }
}

