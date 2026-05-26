using System;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityInventoryValidationIssue
    {
        public ActivityCapabilityInventoryValidationIssue(
            string code,
            bool isError,
            string ownerId,
            string capabilityId,
            string detail)
        {
            Code = Normalize(code);
            IsError = isError;
            OwnerId = Normalize(ownerId);
            CapabilityId = Normalize(capabilityId);
            Detail = Normalize(detail);
        }

        public string Code { get; }
        public bool IsError { get; }
        public bool IsWarning => !IsError;
        public string OwnerId { get; }
        public string CapabilityId { get; }
        public string Detail { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Code);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
