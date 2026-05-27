using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public interface IActivityCapabilityPermissionRuntime
    {
        ActivityCapabilityPermissionSnapshot Snapshot { get; }

        void BeginPermissionScope(string pipelineId, string sessionStateId, string activityId, int entrySequence);

        void SetActiveIdentity(string pipelineId, string sessionStateId, string activityId, int entrySequence);

        void ReplaceReceivers(IReadOnlyList<ActivityCapabilityPermissionReceiverReference> receivers);

        ActivityCapabilityPermissionFact Publish(ActivityCapabilityPermissionCommand command);
    }
}
