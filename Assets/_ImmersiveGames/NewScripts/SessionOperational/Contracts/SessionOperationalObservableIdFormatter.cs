namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public static class SessionOperationalObservableIdFormatter
    {
        public static string BuildRouteOperationId(string routeIdentity, string activeScene, int sequence)
        {
            return $"{Normalize(routeIdentity)}|{Normalize(activeScene)}|{sequence}";
        }

        public static string BuildTransitionId(string routeIdentity, string activeScene, int sequence)
        {
            return $"{Normalize(routeIdentity)}|{Normalize(activeScene)}|{sequence}|sandbox";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
