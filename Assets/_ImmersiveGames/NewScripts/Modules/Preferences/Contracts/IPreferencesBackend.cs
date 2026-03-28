namespace _ImmersiveGames.NewScripts.Modules.Preferences.Contracts
{
    public interface IPreferencesBackend
    {
        string BackendId { get; }
        bool IsAvailable { get; }

        bool TryLoad(
            string profileId,
            string slotId,
            out AudioPreferencesSnapshot snapshot,
            out string reason);

        bool TrySave(
            AudioPreferencesSnapshot snapshot,
            out string reason);
    }
}
