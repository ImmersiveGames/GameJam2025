namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public enum ActorAttributeUiTargetResolveResultKind
    {
        Resolved = 0,
        RejectedInvalidRequest = 1,
        RejectedParticipationContextMissing = 2,
        RejectedRegistryMissing = 3,
        RejectedPrimaryPlayerMissing = 4,
        RejectedPrimaryPlayerAmbiguous = 5,
        RejectedExplicitActorIdMissing = 6,
        RejectedExplicitActorIdAmbiguous = 7,
        RejectedExplicitActorInstanceRuntimeIdMissing = 8,
        RejectedExplicitActorInstanceRuntimeIdNotFound = 9
    }
}
