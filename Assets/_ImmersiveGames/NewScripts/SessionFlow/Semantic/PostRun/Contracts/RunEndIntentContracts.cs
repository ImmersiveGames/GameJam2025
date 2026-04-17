using _ImmersiveGames.NewScripts.Foundation.Core.Events;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Contracts
{
    public readonly struct RunEndIntent
    {
        public RunEndIntent(string signature, string sceneName, string profile, int frame, string reason, bool isGameplayScene)
        {
            Signature = string.IsNullOrWhiteSpace(signature) ? string.Empty : signature.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            Profile = string.IsNullOrWhiteSpace(profile) ? string.Empty : profile.Trim();
            Frame = frame < 0 ? 0 : frame;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            IsGameplayScene = isGameplayScene;
        }

        public string Signature { get; }
        public string SceneName { get; }
        public string Profile { get; }
        public int Frame { get; }
        public string Reason { get; }
        public bool IsGameplayScene { get; }
    }

    public readonly struct RunEndIntentAcceptedEvent : IEvent
    {
        public RunEndIntentAcceptedEvent(RunEndIntent intent)
        {
            Intent = intent;
        }

        public RunEndIntent Intent { get; }
    }
}

