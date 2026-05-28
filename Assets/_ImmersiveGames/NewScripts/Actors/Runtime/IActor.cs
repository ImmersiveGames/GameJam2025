using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    public interface IActor
    {
        string ActorId { get; }
        ActorInstanceId RuntimeActorInstanceId { get; }
        ActorDefinitionRef ActorDefinitionRef { get; }
        ActorRole ActorRoleMetadata { get; }
        ActorScope ActorScopeMetadata { get; }
        ActorCapabilitySurface CapabilitySurface { get; }
        void ValidateLocalConfigurationOrThrow(string source);
    }
}
