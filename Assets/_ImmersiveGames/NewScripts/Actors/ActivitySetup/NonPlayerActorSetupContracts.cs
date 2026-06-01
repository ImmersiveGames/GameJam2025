namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public enum NonPlayerActorScope
    {
        Unknown = 0,
        ActivityScoped = 1,
        RouteScoped = 2,
        GlobalScopedUnsupported = 3,
    }

    public enum NonPlayerActorParticipationPolicy
    {
        Unknown = 0,
        ExplicitActivityIds = 1,
        AllActivitiesInRoute = 2,
        Disabled = 3,
    }
}
