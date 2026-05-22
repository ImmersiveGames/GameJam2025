namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    internal static class InputModesDefaults
    {
        public const string PlayerActionMapName = "Player";
        public const string MenuActionMapName = "UI";

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

