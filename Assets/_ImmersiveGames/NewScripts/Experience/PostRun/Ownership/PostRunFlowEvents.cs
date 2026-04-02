using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Experience.PostRun.Result;
namespace _ImmersiveGames.NewScripts.Experience.PostRun.Ownership
{
    /// <summary>
    /// Eventos de bridge temporária do contexto visual de PostRunMenu.
    ///
    /// Mantém o runtime observável sem transformar o menu em um segundo rail público.
    /// </summary>
    public readonly struct PostRunEnteredEvent : IEvent
    {
        public PostRunEnteredEvent(PostRunOwnershipContext context)
        {
            Context = context;
        }

        public PostRunOwnershipContext Context { get; }
    }

    public readonly struct PostRunExitedEvent : IEvent
    {
        public PostRunExitedEvent(PostRunOwnershipExitContext context)
        {
            Context = context;
        }

        public PostRunOwnershipExitContext Context { get; }
    }

    public readonly struct PostRunResultUpdatedEvent : IEvent
    {
        public PostRunResultUpdatedEvent(PostRunResult result, string reason)
        {
            Result = result;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public PostRunResult Result { get; }
        public string Reason { get; }
    }
}

