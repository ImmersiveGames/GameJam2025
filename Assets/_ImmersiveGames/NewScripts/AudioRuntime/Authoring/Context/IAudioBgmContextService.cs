using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
namespace ImmersiveGames.GameJam2025.Experience.Audio.Context
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

