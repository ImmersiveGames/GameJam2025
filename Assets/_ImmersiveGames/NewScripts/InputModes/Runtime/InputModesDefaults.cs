using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    internal static class InputModesDefaults
    {
        public const string PlayerActionMapName = "Player";
        public const string MenuActionMapName = "UI";

        public static (string player, string menu) ResolveRequiredFrom(RuntimeModeConfig config)
        {
            RuntimeModeConfig.InputModesSettings settings = config?.inputModes;
            string player = Normalize(settings?.playerActionMapName);
            string menu = Normalize(settings?.menuActionMapName);
            return (player, menu);
        }

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

