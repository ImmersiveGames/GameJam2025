using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public static class SessionOperationalObservableIdFormatter
    {
        public static string BuildRouteOperationId(string routeIdentity, string activeScene, int sequence)
        {
            return $"{routeIdentity.TrimToEmpty()}|{activeScene.TrimToEmpty()}|{sequence}";
        }

        public static string BuildTransitionId(string routeIdentity, string activeScene, int sequence)
        {
            return $"{routeIdentity.TrimToEmpty()}|{activeScene.TrimToEmpty()}|{sequence}|sandbox";
        }
    }
}
