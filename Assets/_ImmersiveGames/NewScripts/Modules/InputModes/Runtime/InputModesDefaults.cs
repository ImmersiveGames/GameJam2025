using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;

namespace _ImmersiveGames.NewScripts.Modules.InputModes
{
    internal static class InputModesDefaults
    {
        public const string PlayerActionMapName = "Player";
        public const string MenuActionMapName = "UI";

        public static (string player, string menu) ResolveFrom(RuntimeModeConfig config)
        {
            RuntimeModeConfig.InputModesSettings settings = config?.inputModes;
            string player = NormalizeOrDefault(settings?.playerActionMapName, PlayerActionMapName);
            string menu = NormalizeOrDefault(settings?.menuActionMapName, MenuActionMapName);
            return (player, menu);
        }

        public static string NormalizeOrDefault(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
