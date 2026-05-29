namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityPolicyEntry
    {
        public ActivityCapabilityPolicyEntry(string key, string value)
        {
            Key = Normalize(key);
            Value = Normalize(value);
        }

        public string Key { get; }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Key);

        public override string ToString()
        {
            return $"{Key}={Value}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
