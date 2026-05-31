namespace _ImmersiveGames.NewScripts.PlayerParticipation.Contracts
{
    public enum PlayerSlotKind
    {
        Unknown = 0,
        Local = 1,
        Remote = 2,
        AiReserved = 3,
    }

    public enum PlayerSlotReservationSourceKind
    {
        Unknown = 0,
        DefaultPolicy = 1,
        CharacterSelectionRoute = 2,
        RuntimeJoin = 3,
        SaveOrProfile = 4,
        ExternalSystem = 5,
        RouteParticipantSetDefinition = 6,
    }

    public enum PlayerSelectionSourceKind
    {
        Unknown = 0,
        DefaultPolicy = 1,
        CharacterSelectionRoute = 2,
        SaveOrProfile = 3,
        RuntimeJoinDefault = 4,
        RouteParticipantSetDefinition = 5,
    }

    public enum SessionParticipantRole
    {
        Unknown = 0,
        PrimaryPlayer = 1,
        SupportingPlayer = 2,
        RouteActor = 3,
        SceneAuthoredActor = 4,
    }

    public enum RouteParticipationRequirementKind
    {
        Unknown = 0,
        None = 1,
        Optional = 2,
        RequiredDefaultable = 3,
        RequiredExplicit = 4,
    }

    public enum RuntimePlayerJoinPolicyKind
    {
        Unknown = 0,
        Unsupported = 1,
        Reject = 2,
        DeferUntilNextActivityEntry = 3,
        RequireSelection = 4,
        MaterializeThroughActivityPipeline = 5,
    }

    public enum ActorMaterializationPolicyKind
    {
        Unknown = 0,
        MaterializeOnActivityEntry = 1,
        RetainRouteScoped = 2,
        ReuseExistingIfAvailable = 3,
        Unsupported = 4,
    }
}
