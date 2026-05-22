using _ImmersiveGames.NewScripts.SaveRuntime.Models;

namespace _ImmersiveGames.NewScripts.SessionOperational.Adapters
{
    public interface IProgressionSlotContextResolver
    {
        bool TryResolveForRouteActivitySave(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            out ProgressionSlotContext slotContext,
            out string failureReason);
    }
}
