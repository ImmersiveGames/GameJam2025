namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public interface IPlayerActorCapabilityIdentityResolver
    {
        bool TryResolve(ActorScanTarget target, out PlayerActorCapabilityIdentity identity);
    }
}
