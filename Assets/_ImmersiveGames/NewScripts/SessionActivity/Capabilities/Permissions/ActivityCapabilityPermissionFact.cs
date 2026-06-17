using System;

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
            OutcomeCode = Normalize(outcomeCode);
            Message = Normalize(message);
        }

        public ActivityCapabilityPermissionCommand Command { get; }
        public PermissionOutcomeKind OutcomeKind { get; }
        public string OutcomeCode { get; }
        public string Outcome => OutcomeCode;
        public string Message { get; }
        public bool IsValid => Command.IsValid && OutcomeKind != PermissionOutcomeKind.Unknown && !string.IsNullOrWhiteSpace(OutcomeCode);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
