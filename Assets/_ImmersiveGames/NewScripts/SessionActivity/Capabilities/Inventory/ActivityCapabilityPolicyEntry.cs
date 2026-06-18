using _ImmersiveGames.NewScripts.UnityUtils;
namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public readonly struct ActivityCapabilityPolicyEntry
    {
        public ActivityCapabilityPolicyEntry(string key, string value)
        {
            Key = key.TrimToEmpty();
            Value = value.TrimToEmpty();
        }

        public string Key { get; }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Key);

        public override string ToString()
        {
            return $"{Key}={Value}";
        }
}
}
