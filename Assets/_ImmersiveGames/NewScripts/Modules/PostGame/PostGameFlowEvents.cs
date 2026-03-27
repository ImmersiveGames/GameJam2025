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
}
