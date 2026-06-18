using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.UnityUtils;
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
            Reason = reason.TrimToEmpty();
        }

        public InputModeRequestKind PreviousMode { get; }
        public InputModeRequestKind CurrentMode { get; }
        public string Reason { get; }
    }
}
