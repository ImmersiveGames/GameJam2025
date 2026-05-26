using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    [Serializable]
    public readonly struct ActivityCapabilityPermissionFact
    {
        public ActivityCapabilityPermissionFact(
            ActivityCapabilityPermissionCommand command,
            string outcome,
            string message)
        {
            Command = command;
            Outcome = Normalize(outcome);
            Message = Normalize(message);
        }

        public ActivityCapabilityPermissionCommand Command { get; }
        public string Outcome { get; }
        public string Message { get; }
        public bool IsValid => Command.IsValid && !string.IsNullOrWhiteSpace(Outcome);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
