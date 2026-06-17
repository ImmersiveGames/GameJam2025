namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory.RuntimeReferences
{
    public interface IActivityCapabilityRuntimeReference
    {
        string CapabilityId { get; }
        string OwnerId { get; }
        string ComponentPath { get; }
        bool IsValid { get; }
    }
}
