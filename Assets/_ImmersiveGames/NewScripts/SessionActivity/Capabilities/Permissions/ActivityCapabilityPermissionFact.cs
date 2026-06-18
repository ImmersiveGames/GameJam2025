using System;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionFact
    {
        public ActivityCapabilityPermissionFact(
            ActivityCapabilityPermissionCommand command,
            PermissionOutcomeKind outcomeKind,
            string outcomeCode,
            string message)
        {
            Command = command;
            OutcomeKind = outcomeKind;
            OutcomeCode = outcomeCode.TrimToEmpty();
            Message = message.TrimToEmpty();
        }

        public ActivityCapabilityPermissionCommand Command { get; }
        public PermissionOutcomeKind OutcomeKind { get; }
        public string OutcomeCode { get; }
        public string Outcome => OutcomeCode;
        public string Message { get; }
        public bool IsValid => Command.IsValid && OutcomeKind != PermissionOutcomeKind.Unknown && !string.IsNullOrWhiteSpace(OutcomeCode);
}
}
