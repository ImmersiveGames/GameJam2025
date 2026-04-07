using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.PostRun.Contracts;

namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    public interface IRunEndIntentOwnershipService
    {
        bool IsAccepted { get; }
        RunEndIntent CurrentIntent { get; }
        void AcceptRunEndIntent(RunEndIntent intent);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class RunEndIntentOwnershipService : IRunEndIntentOwnershipService
    {
        public bool IsAccepted { get; private set; }
        public RunEndIntent CurrentIntent { get; private set; }

        public void AcceptRunEndIntent(RunEndIntent intent)
        {
            if (IsAccepted)
            {
                return;
            }

            IsAccepted = true;
            CurrentIntent = intent;

            DebugUtility.Log<RunEndIntentOwnershipService>(
                $"[OBS][GameplaySessionFlow][RunEndIntent] RunEndIntentAccepted signature='{intent.Signature}' scene='{intent.SceneName}' frame={intent.Frame} reason='{intent.Reason}'.",
                DebugUtility.Colors.Info);

            EventBus<RunEndIntentAcceptedEvent>.Raise(new RunEndIntentAcceptedEvent(intent));
        }
    }
}
