namespace _ImmersiveGames.NewScripts.SaveRuntime.Models
{
    public enum SaveResultKind
    {
        None = 0,
        Saved = 1,
        Loaded = 2,
        Skipped = 3,
        Failed = 4,
        Deleted = 5,
    }

    public sealed class SaveResult
    {
        public SaveResult(
            SaveResultKind kind,
            string reason,
            SaveRecord record)
        {
            Kind = kind;
            Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
            Record = record;
        }

        public SaveResultKind Kind { get; }
        public string Reason { get; }
        public SaveRecord Record { get; }

        public bool IsSuccess =>
            Kind == SaveResultKind.Saved ||
            Kind == SaveResultKind.Loaded ||
            Kind == SaveResultKind.Deleted;
    }
}

