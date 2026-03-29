using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    /// <summary>
    /// Owner canônico da precedência contextual de BGM.
    /// </summary>
    public interface IAudioBgmContextService
    {
        void OnSceneTransitionStarted(SceneTransitionStartedEvent evt);

        void OnSceneTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt);

        void OnLevelSelected(LevelSelectedEvent evt);

        void OnLevelSwapLocalApplied(LevelSwapLocalAppliedEvent evt);
    }
}
