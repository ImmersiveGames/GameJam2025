namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public enum ActivityContentMode
    {
        None = 0,
        Profile = 1,
    }

    public enum ActivityContentRequiredness
    {
        Unknown = 0,
        Optional = 1,
        Required = 2,
    }

    public enum ActivityContentPreparationPolicy
    {
        Unknown = 0,
        LoadBeforeSetup = 1,
    }
}
