using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Attributes;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Presentation;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
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
        public IReadOnlyList<ActorAttributeSetupContribution> CurrentActivityAttributeSetupContributions { get; private set; }
        public IReadOnlyList<ActorPresentationSetupContribution> CurrentActivityPresentationSetupContributions { get; private set; }
        public IReadOnlyList<ActivityPermissionReceiverContribution> CurrentActivityPermissionReceiverContributions { get; private set; }

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

        public void SetCurrentActivityAttributeSetupContributions(IReadOnlyList<ActorAttributeSetupContribution> contributions)
        {
            CurrentActivityAttributeSetupContributions = contributions ?? Array.Empty<ActorAttributeSetupContribution>();
        }

        public void SetCurrentActivityPresentationSetupContributions(IReadOnlyList<ActorPresentationSetupContribution> contributions)
        {
            CurrentActivityPresentationSetupContributions = contributions ?? Array.Empty<ActorPresentationSetupContribution>();
        }

        public void SetCurrentActivityPermissionReceiverContributions(IReadOnlyList<ActivityPermissionReceiverContribution> contributions)
        {
            CurrentActivityPermissionReceiverContributions = contributions ?? Array.Empty<ActivityPermissionReceiverContribution>();
        }

        public void ClearCurrentActivityCapabilityInventoryPreview()
        {
            CurrentActivityCapabilityInventoryPreview = default;
            CurrentActivityCapabilityInventoryPreviewValidation = default;
            CurrentActivityAttributeSetupContributions = Array.Empty<ActorAttributeSetupContribution>();
            CurrentActivityPresentationSetupContributions = Array.Empty<ActorPresentationSetupContribution>();
            CurrentActivityPermissionReceiverContributions = Array.Empty<ActivityPermissionReceiverContribution>();
        }
    }
}
