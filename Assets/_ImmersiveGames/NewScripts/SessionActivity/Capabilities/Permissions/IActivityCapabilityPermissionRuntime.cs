using System.Collections.Generic;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions
{
    public interface IActivityCapabilityPermissionRuntime
    {
        ActivityCapabilityPermissionSnapshot Snapshot { get; }

        void SetActiveIdentity(string pipelineId, string sessionStateId, string activityId, int entrySequence);

        void ReplaceReceivers(IReadOnlyList<IActivityCapabilityPermissionReceiver> receivers);

        ActivityCapabilityPermissionFact Publish(ActivityCapabilityPermissionCommand command);
    }
}
