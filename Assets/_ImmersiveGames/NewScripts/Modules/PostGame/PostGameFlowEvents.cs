using _ImmersiveGames.NewScripts.Core.Events;

namespace _ImmersiveGames.NewScripts.Modules.PostGame
{
    public readonly struct PostGameEnteredEvent : IEvent
    {
        public PostGameEnteredEvent(PostGameOwnershipContext context)
        {
            Context = context;
        }

        public PostGameOwnershipContext Context { get; }
    }

    public readonly struct PostGameExitedEvent : IEvent
    {
        public PostGameExitedEvent(PostGameOwnershipExitContext context)
        {
            Context = context;
        }

        public PostGameOwnershipExitContext Context { get; }
    }

    public readonly struct PostGameResultUpdatedEvent : IEvent
    {
        public PostGameResultUpdatedEvent(PostGameResult result, string reason)
        {
            Result = result;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public PostGameResult Result { get; }
        public string Reason { get; }
    }
}
