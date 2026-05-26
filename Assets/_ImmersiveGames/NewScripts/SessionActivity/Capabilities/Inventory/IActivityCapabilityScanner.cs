namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public interface IActivityCapabilityScanner
    {
        string ScannerId { get; }
        int Order { get; }
        ActivityCapabilityScanResult Scan(ActivityCapabilityScanContext context);
    }
}
