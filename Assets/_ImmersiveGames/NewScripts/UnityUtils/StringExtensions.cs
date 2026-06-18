namespace _ImmersiveGames.NewScripts.UnityUtils
{
    public static class StringExtensions
    {
        public static string TrimToEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public static string TrimToOrDefault(this string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }
    }
}
