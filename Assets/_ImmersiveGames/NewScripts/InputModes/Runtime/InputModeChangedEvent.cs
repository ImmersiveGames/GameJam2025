using _ImmersiveGames.NewScripts.Foundation.Core.Events;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    /// <summary>
    /// Hook oficial de observacao quando o modo de input muda de fato.
    /// </summary>
    public readonly struct InputModeChangedEvent : IEvent
    {
        public InputModeChangedEvent(InputModeRequestKind previousMode, InputModeRequestKind currentMode, string reason)
        {
            PreviousMode = previousMode;
            CurrentMode = currentMode;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        }

        public InputModeRequestKind PreviousMode { get; }
        public InputModeRequestKind CurrentMode { get; }
        public string Reason { get; }
    }
}

