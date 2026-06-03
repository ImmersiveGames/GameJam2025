using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionActivity.Pipeline.Runtime
{
    internal sealed class ActivityEntryInventoryRuntimeState
    {
        public ActivityObjectContributorDiscoveryResult CurrentActivityObjectContributorDiscoveryResult { get; private set; }
        public ActorInventoryFeedResult CurrentActorInventoryFeedResult { get; private set; }
        public ActivitySetupInventory CurrentActivitySetupInventory { get; private set; }
        public ActivityCapabilityInventory CurrentActivityCapabilityInventoryPreview { get; private set; }
        public ActivityCapabilityInventoryValidationResult CurrentActivityCapabilityInventoryPreviewValidation { get; private set; }

        public void SetCurrentActivityObjectContributorDiscoveryResult(ActivityObjectContributorDiscoveryResult result)
        {
            CurrentActivityObjectContributorDiscoveryResult = result;
        }

        public void ClearCurrentActivityObjectContributorDiscoveryResult()
        {
            CurrentActivityObjectContributorDiscoveryResult = default;
        }

        public void SetCurrentActorInventoryFeedResult(ActorInventoryFeedResult result)
        {
            CurrentActorInventoryFeedResult = result;
        }

        public void ClearCurrentActorInventoryFeedResult()
        {
            CurrentActorInventoryFeedResult = default;
        }

        public void SetCurrentActivitySetupInventory(ActivitySetupInventory inventory)
        {
            CurrentActivitySetupInventory = inventory;
        }

        public void ClearCurrentActivitySetupInventory()
        {
            CurrentActivitySetupInventory = default;
        }

        public void SetCurrentActivityCapabilityInventoryPreview(
            ActivityCapabilityInventory inventory,
            ActivityCapabilityInventoryValidationResult validation)
        {
            CurrentActivityCapabilityInventoryPreview = inventory;
            CurrentActivityCapabilityInventoryPreviewValidation = validation;
        }

        public void ClearCurrentActivityCapabilityInventoryPreview()
        {
            CurrentActivityCapabilityInventoryPreview = default;
            CurrentActivityCapabilityInventoryPreviewValidation = default;
        }
    }
}
