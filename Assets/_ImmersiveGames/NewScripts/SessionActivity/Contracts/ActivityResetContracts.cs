using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityResetCompletionKind
    {
        Unknown = 0,
        Applied = 1,
        NoCommands = 2,
        SkippedOptional = 3,
        NoApplicableGroups = 4,
        InventoryInvalidOrStale = 5,
        Failed = 6,
    }

    public readonly struct ActivityResetCommand
    {
        public ActivityResetCommand(
            SessionActivityIdentity identity,
            SessionActivityDefinition definition,
            string source,
            string reason)
        {
            Identity = identity;
            Definition = definition;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityDefinition Definition { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            Definition.IsValid &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct ActivityResetContext
    {
        public ActivityResetContext(
            ActivityObjectContributorDiscoveryResult discoveryResult,
            ActivityCapabilityInventory resetInventory,
            ActivityCapabilityInventoryValidationResult resetInventoryValidation)
        {
            DiscoveryResult = discoveryResult;
            ResetInventory = resetInventory;
            ResetInventoryValidation = resetInventoryValidation;
        }

        public ActivityObjectContributorDiscoveryResult DiscoveryResult { get; }
        public ActivityCapabilityInventory ResetInventory { get; }
        public ActivityCapabilityInventoryValidationResult ResetInventoryValidation { get; }
    }

    public readonly struct ActivityResetResult
    {
        public ActivityResetResult(
            ActivityResetCompletionKind completionKind,
            string completionReason,
            int commandCount,
            int appliedCount,
            int skippedCount,
            int failedCount)
        {
            CompletionKind = completionKind;
            CompletionReason = Normalize(completionReason);
            CommandCount = commandCount < 0 ? 0 : commandCount;
            AppliedCount = appliedCount < 0 ? 0 : appliedCount;
            SkippedCount = skippedCount < 0 ? 0 : skippedCount;
            FailedCount = failedCount < 0 ? 0 : failedCount;
        }

        public ActivityResetCompletionKind CompletionKind { get; }
        public string CompletionReason { get; }
        public int CommandCount { get; }
        public int AppliedCount { get; }
        public int SkippedCount { get; }
        public int FailedCount { get; }

        public bool IsValid => CompletionKind != ActivityResetCompletionKind.Unknown;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
