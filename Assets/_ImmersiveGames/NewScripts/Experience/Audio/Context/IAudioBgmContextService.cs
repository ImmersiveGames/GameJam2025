using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Context
{
    /// <summary>
    /// Owner canônico da precedência contextual de BGM.
    /// </summary>
    public interface IAudioBgmContextService
    {
        void OnSceneTransitionStarted(SceneTransitionStartedEvent evt);

        void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt);

        void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt);
    }
}
