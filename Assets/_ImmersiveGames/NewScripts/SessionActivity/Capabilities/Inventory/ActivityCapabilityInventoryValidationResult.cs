using System;
using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public enum ActivityCapabilityInventoryValidationStatus
    {
        Unknown = 0,
        Passed = 1,
        PassedWithWarnings = 2,
        FailedPassive = 3,
    }

    public readonly struct ActivityCapabilityInventoryValidationResult
    {
        public ActivityCapabilityInventoryValidationResult(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationStatus status,
            IReadOnlyList<ActivityCapabilityInventoryValidationIssue> issues,
            string source,
            string reason)
        {
            Inventory = inventory;
            Status = status;
            Issues = issues ?? Array.Empty<ActivityCapabilityInventoryValidationIssue>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ActivityCapabilityInventory Inventory { get; }
        public ActivityCapabilityInventoryValidationStatus Status { get; }
        public IReadOnlyList<ActivityCapabilityInventoryValidationIssue> Issues { get; }
        public string Source { get; }
        public string Reason { get; }

        public int IssueCount => Issues.Count;
        public int ErrorCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < Issues.Count; index++)
                {
                    if (Issues[index].IsError)
                    {
                        count += 1;
                    }
                }

                return count;
            }
        }

        public int WarningCount => IssueCount - ErrorCount;
        public bool IsPassed => Status == ActivityCapabilityInventoryValidationStatus.Passed;
        public bool IsPassedWithWarnings => Status == ActivityCapabilityInventoryValidationStatus.PassedWithWarnings;
        public bool IsFailedPassive => Status == ActivityCapabilityInventoryValidationStatus.FailedPassive;
        public bool IsValid => Inventory.IsValid && Status != ActivityCapabilityInventoryValidationStatus.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
